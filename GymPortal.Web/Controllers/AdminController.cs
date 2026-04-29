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
        public async Task<IActionResult> CreateClass(AdminClassesViewModel viewModel)
        {
            var model = viewModel.Form;   // get the actual DTO

            // Manual validation (same as before)
            if (model.TrainingProgramId <= 0)
                ModelState.AddModelError("Form.TrainingProgramId", "Please select a training program.");
            if (string.IsNullOrWhiteSpace(model.InstructorName))
                ModelState.AddModelError("Form.InstructorName", "Instructor name is required.");
            if (model.StartTime == DateTime.MinValue || model.EndTime == DateTime.MinValue)
                ModelState.AddModelError("Form.StartTime", "Start time and end time are required.");
            else if (model.StartTime >= model.EndTime)
                ModelState.AddModelError("Form.EndTime", "End time must be after start time.");
            if (model.MaxParticipants < 1)
                ModelState.AddModelError("Form.MaxParticipants", "Max participants must be at least 1.");

            if (ModelState.IsValid)
            {
                var result = await _adminService.CreateSessionAsync(model);
                if (result.IsSuccess)
                    TempData["Success"] = "Class session created.";
                else
                    TempData["Error"] = result.Error;
                return RedirectToAction(nameof(Classes));
            }

            // Reload the view with existing data
            var sessions = await _adminService.GetAllSessionsAsync();
            var programs = await _adminService.GetAllProgramsAsync();
            viewModel.Sessions = sessions.ToList();
            viewModel.Programs = programs.ToList();
            return View("Classes", viewModel);
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
