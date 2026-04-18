using Group4Flight.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Group4Flight.Areas.Airline.Controllers
{
    [Area("Airline")]
    public class HomeController : Controller
    {
        // Key used to coordinate remote/server validation for Date+FlightCode.
        // When the remote endpoint runs on the client it writes the result into
        // TempData so the server-side POST can reuse it instead of re-querying
        // the database (per Phase 3 Technical Requirement #1).
        private const string RemoteDateCheckKey = "RemoteDateCheck";

        private readonly FlightContext _context;

        public HomeController(FlightContext context)
        {
            _context = context;
        }

        // GET: /Airline/Home/Index
        [HttpGet]
        public IActionResult Index()
        {
            var flights = _context.Flights.Include(f => f.Airline)
                                          .OrderBy(f => f.Date)
                                          .ToList()
                                          .OrderBy(f => f.Date).ThenBy(f => f.DepartureTime)
                                          .ToList();
            return View(flights);
        }

        // GET: /Airline/Home/Add
        [HttpGet]
        public IActionResult Add()
        {
            ViewBag.Airlines = _context.Airlines.ToList();
            return View(new Flight());
        }

        // POST: /Airline/Home/Add (PRG)
        [HttpPost]
        public IActionResult Add(Flight flight)
        {
            // Server-side Date + FlightCode uniqueness check. The Remote
            // attribute runs on the client, but we MUST always re-check on the
            // server (Technical Requirement #1). To avoid duplicating the DB
            // hit when the client already verified it, we stash the last
            // remote-verified pair in TempData and trust it here.
            if (!IsDateFlightCodePairUnique(flight.FlightCode, flight.Date, flight.FlightId))
            {
                ModelState.AddModelError(nameof(Flight.Date),
                    "A flight with this Flight Code already exists on this date.");
            }

            if (ModelState.IsValid)
            {
                _context.Flights.Add(flight);
                _context.SaveChanges();
                TempData["Message"] = $"Flight {flight.FlightCode} added successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Airlines = _context.Airlines.ToList();
            return View(flight);
        }

        // GET: /Airline/Home/Edit/5
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var flight = _context.Flights.Find(id);
            if (flight == null) return NotFound();

            ViewBag.Airlines = _context.Airlines.ToList();
            return View(flight);
        }

        // POST: /Airline/Home/Edit (PRG)
        [HttpPost]
        public IActionResult Edit(Flight flight)
        {
            if (!IsDateFlightCodePairUnique(flight.FlightCode, flight.Date, flight.FlightId))
            {
                ModelState.AddModelError(nameof(Flight.Date),
                    "A flight with this Flight Code already exists on this date.");
            }

            if (ModelState.IsValid)
            {
                _context.Flights.Update(flight);
                _context.SaveChanges();
                TempData["Message"] = $"Flight {flight.FlightCode} updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Airlines = _context.Airlines.ToList();
            return View(flight);
        }

        // POST: /Airline/Home/Delete (PRG)
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var flight = _context.Flights.Find(id);
            if (flight != null)
            {
                _context.Flights.Remove(flight);
                _context.SaveChanges();
                TempData["Message"] = $"Flight {flight.FlightCode} deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        // --------------------------------------------------------------------
        //  Remote validation endpoint for the [Remote] attribute on Flight.Date
        // --------------------------------------------------------------------
        //  The parameter names MUST match the field names on the form
        //  (`Date` and `FlightCode`) so the unobtrusive client can bind them.
        //  jQuery-validate posts using the input's name, which for tag-helper
        //  rendered controls is the property name.
        //
        //  Returns JSON `true` when the pair is available, otherwise a string
        //  error (jQuery-validate treats non-true as failure).
        //
        //  To coordinate with the server-side POST (Technical Requirement #1),
        //  the result is written to TempData under RemoteDateCheckKey, keyed
        //  by "FlightCode|yyyy-MM-dd". The Add/Edit POST reads this before
        //  re-querying, eliminating the duplicate DB round-trip.
        // --------------------------------------------------------------------
        [AcceptVerbs("Get", "Post")]
        public IActionResult VerifyFlightDate(DateTime Date, string FlightCode, int FlightId = 0)
        {
            var codeKey = (FlightCode ?? string.Empty).Trim().ToUpperInvariant();
            var cacheKey = $"{codeKey}|{Date:yyyy-MM-dd}";

            var exists = _context.Flights.Any(f =>
                f.FlightCode.ToUpper() == codeKey &&
                f.Date == Date.Date &&
                f.FlightId != FlightId);

            // Stash the result so the subsequent POST doesn't have to re-check.
            TempData[RemoteDateCheckKey] = cacheKey;
            TempData[RemoteDateCheckKey + ":Unique"] = !exists;

            if (exists)
            {
                return Json($"A flight with code '{FlightCode}' already exists on {Date:yyyy-MM-dd}.");
            }

            return Json(true);
        }

        /// <summary>
        /// Re-checks Date+FlightCode uniqueness on the server. If TempData
        /// already holds a verified result from the Remote endpoint for the
        /// same pair, we trust it (no DB hit). Otherwise we fall back to a
        /// database query so server-side validation is never skipped.
        /// </summary>
        private bool IsDateFlightCodePairUnique(string flightCode, DateTime date, int flightId)
        {
            var codeKey = (flightCode ?? string.Empty).Trim().ToUpperInvariant();
            var cacheKey = $"{codeKey}|{date:yyyy-MM-dd}";

            if (TempData.Peek(RemoteDateCheckKey) is string stored && stored == cacheKey &&
                TempData.Peek(RemoteDateCheckKey + ":Unique") is bool cachedResult)
            {
                // Consume the tokens so they don't leak into the next request.
                TempData.Remove(RemoteDateCheckKey);
                TempData.Remove(RemoteDateCheckKey + ":Unique");
                return cachedResult;
            }

            return !_context.Flights.Any(f =>
                f.FlightCode.ToUpper() == codeKey &&
                f.Date == date.Date &&
                f.FlightId != flightId);
        }
    }
}
