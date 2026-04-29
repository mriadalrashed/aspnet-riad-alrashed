using Microsoft.AspNetCore.Mvc;

namespace GymPortal.Web.Controllers
{
    public class StoreController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Product(int id)
        {
            ViewData["ProductId"] = id;
            return View();
        }

        [HttpGet]
        public IActionResult Category(string category)
        {
            ViewData["Category"] = category;
            return View();
        }
    }
}
