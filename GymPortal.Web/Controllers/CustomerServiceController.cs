using GymPortal.Application.DTOs;
using GymPortal.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace GymPortal.Web.Controllers
{
    public class CustomerServiceController : Controller
    {
        // GET: /CustomerService
        [HttpGet]
        public IActionResult Index()
        {
            return View(new CustomerServiceViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Contact(ContactFormDto model)
        {
            if (ModelState.IsValid)
            {
                // Handle contact form submission (send email, save to DB, etc.)
                TempData["Success"] = "Your message has been sent. We'll get back to you soon.";
                return RedirectToAction(nameof(Index));
            }
            // If invalid, redisplay with errors
            var viewModel = new CustomerServiceViewModel { ContactForm = model };
            return View("Index", viewModel);
        }
    }
}

