using Microsoft.AspNetCore.Mvc;

namespace Group4Flight.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Manage()
        {
            return Content("Manage Users - Route: /Admin/Home/Manage");
        }

        public IActionResult Rights()
        {
            return Content("Rights & Obligations - Route: /Admin/Home/Rights");
        }
    }
}
