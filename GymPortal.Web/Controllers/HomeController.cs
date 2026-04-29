using GymPortal.Application.DTOs;
using GymPortal.Application.Interfaces.Services;
using GymPortal.Web.Models;
using GymPortal.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace GymPortal.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IClassService _classService;

        public HomeController(IClassService classService)
        {
            _classService = classService;
        }

        public async Task<IActionResult> Index()
        {
            var programs = await _classService.GetAllProgramsAsync();

            var viewModel = new HomeViewModel
            {
                TrainingPrograms = programs.Select(p => new TrainingProgramDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description ?? string.Empty,
                    Category = p.Category ?? string.Empty,
                    DifficultyLevel = p.DifficultyLevel,
                    ImageUrl = p.ImageUrl
                }).ToList(),
                FeaturedProducts = new List<ProductDto>()
            };

            return View(viewModel);
        }
        public IActionResult NotFoundPage()
        {
            return View("Error404");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}