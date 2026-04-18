using Group4Flight.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Group4Flight.Controllers
{
    public class HomeController : Controller
    {
        private readonly FlightContext _context;

        public HomeController(FlightContext context)
        {
            _context = context;
        }

        // GET: /Home/Index — load filter from session, query DB, display results
        [HttpGet]
        public IActionResult Index()
        {
            var session = new FlightSession(HttpContext.Session);
            var savedFilter = session.GetFilter() ?? new FlightViewModel();

            var flights = ApplyFilter(_context.Flights.Include(f => f.Airline), savedFilter)
                          .ToList()
                          .OrderBy(f => f.Date).ThenBy(f => f.DepartureTime).ToList();

            var vm = savedFilter;
            vm.Flights = flights;
            vm.Airlines = _context.Airlines.ToList();
            vm.FromOptions = _context.Flights.Select(f => f.From).Distinct().OrderBy(x => x).ToList();
            vm.ToOptions = _context.Flights.Select(f => f.To).Distinct().OrderBy(x => x).ToList();

            ViewBag.SelectionCount = session.GetSelections().Count;
            return View(vm);
        }

        // POST: /Home/Index — save filter to session, redirect (PRG)
        [HttpPost]
        public IActionResult Index(FlightViewModel filter)
        {
            var session = new FlightSession(HttpContext.Session);
            session.SetFilter(filter);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Home/Detail/5
        [HttpGet]
        public IActionResult Detail(int id)
        {
            var flight = _context.Flights.Include(f => f.Airline)
                                         .FirstOrDefault(f => f.FlightId == id);
            if (flight == null) return NotFound();

            ViewBag.SelectionCount = new FlightSession(HttpContext.Session).GetSelections().Count;
            return View(flight);
        }

        // POST: /Home/Select — add flight to session selections (PRG)
        [HttpPost]
        public IActionResult Select(int id)
        {
            var flight = _context.Flights.Find(id);
            if (flight != null)
            {
                var session = new FlightSession(HttpContext.Session);
                session.AddSelection(id);
                TempData["Message"] = $"Flight {flight.FlightCode} added to your selections!";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /Home/Selections
        [HttpGet]
        public IActionResult Selections()
        {
            var session = new FlightSession(HttpContext.Session);
            var cookie = new FlightCookie(Request);

            var selectedIds = session.GetSelections();
            var reservedIds = cookie.ReservedFlightIds;

            var selectedFlights = _context.Flights.Include(f => f.Airline)
                                                   .Where(f => selectedIds.Contains(f.FlightId))
                                                   .ToList();

            var reservedFlights = _context.Flights.Include(f => f.Airline)
                                                   .Where(f => reservedIds.Contains(f.FlightId))
                                                   .ToList();

            ViewBag.SelectedFlights = selectedFlights;
            ViewBag.ReservedFlights = reservedFlights;
            ViewBag.SelectionCount = selectedIds.Count;
            return View();
        }

        // POST: /Home/Reserve — move flight from session to persistent cookie (PRG)
        [HttpPost]
        public IActionResult Reserve(int id)
        {
            var flight = _context.Flights.Find(id);
            if (flight != null)
            {
                var session = new FlightSession(HttpContext.Session);
                var cookie = new FlightCookie(Request);

                session.RemoveSelection(id);
                cookie.AddReservation(id, Response);
                TempData["Message"] = $"Flight {flight.FlightCode} has been reserved for 2 weeks!";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Home/ClearSelections — clear all session selections (PRG)
        [HttpPost]
        public IActionResult ClearSelections()
        {
            var session = new FlightSession(HttpContext.Session);
            session.ClearSelections();
            TempData["Message"] = "All selections cleared.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Home/Back — clear filter from session, redirect to Index (PRG)
        [HttpPost]
        public IActionResult Back()
        {
            var session = new FlightSession(HttpContext.Session);
            session.ClearFilter();
            return RedirectToAction(nameof(Index));
        }

        private static IQueryable<Flight> ApplyFilter(IQueryable<Flight> query, FlightViewModel filter)
        {
            if (!string.IsNullOrWhiteSpace(filter.From))
                query = query.Where(f => f.From.Contains(filter.From));

            if (!string.IsNullOrWhiteSpace(filter.To))
                query = query.Where(f => f.To.Contains(filter.To));

            if (!string.IsNullOrWhiteSpace(filter.CabinType))
                query = query.Where(f => f.CabinType == filter.CabinType);

            if (DateTime.TryParse(filter.StartDate, out var start))
                query = query.Where(f => f.Date >= start);

            if (DateTime.TryParse(filter.EndDate, out var end))
                query = query.Where(f => f.Date <= end);

            return query.OrderBy(f => f.Date);
        }
    }
}
