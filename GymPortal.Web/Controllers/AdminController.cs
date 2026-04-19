using GymPortal.Application.DTOs;
using GymPortal.Application.Interfaces.Services;
using GymPortal.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymPortal.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        // GET: /Admin
        [HttpGet]
        public IActionResult Index()
        {
            // Redirect to Users by default, or you can create a dashboard view
            return RedirectToAction(nameof(Users));
        }

        // GET: /Admin/Users
        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var users = await _adminService.GetAllUsersAsync();
            return View(users.ToList());
        }

        // POST: /Admin/ChangeRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string id, string role)
        {
            var result = await _adminService.ChangeUserRoleAsync(id, role);
            if (result.IsSuccess)
                TempData["Success"] = "Role updated.";
            else
                TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Users));
        }

        // POST: /Admin/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var result = await _adminService.DeleteUserAsync(id);
            if (result.IsSuccess)
                TempData["Success"] = "User deleted.";
            else
                TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Users));
        }

        // GET: /Admin/Classes
        [HttpGet]
        public async Task<IActionResult> Classes()
        {
            var sessions = await _adminService.GetAllSessionsAsync();
            var programs = await _adminService.GetAllProgramsAsync();
            var viewModel = new AdminClassesViewModel
            {
                Sessions = sessions.ToList(),
                Programs = programs.ToList(),
                Form = new ClassSessionDto()
            };
            return View(viewModel);
        }

        // POST: /Admin/CreateClass
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClass(ClassSessionDto model)
        {
            if (ModelState.IsValid)
            {
                var result = await _adminService.CreateSessionAsync(model);
                if (result.IsSuccess)
                    TempData["Success"] = "Class session created.";
                else
                    TempData["Error"] = result.Error;
            }
            return RedirectToAction(nameof(Classes));
        }

        // POST: /Admin/DeleteClass/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteClass(int id)
        {
            var result = await _adminService.DeleteSessionAsync(id);
            if (result.IsSuccess)
                TempData["Success"] = "Session deleted.";
            else
                TempData["Error"] = result.Error;
            return RedirectToAction(nameof(Classes));
        }
    }
}
