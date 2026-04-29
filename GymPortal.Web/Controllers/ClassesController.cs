using GymPortal.Application.DTOs;
using GymPortal.Application.Interfaces.Services;
using GymPortal.Domain.Entities;
using GymPortal.Domain.Exceptions;
using GymPortal.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GymPortal.Web.Controllers
{
    public class ClassesController : Controller
    {
        private readonly IClassService _classService;
        private readonly IBookingService _bookingService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClassesController(
            IClassService classService,
            IBookingService bookingService,
            UserManager<ApplicationUser> userManager)
        {
            _classService = classService;
            _bookingService = bookingService;
            _userManager = userManager;
        }

        // GET: /Classes
        [HttpGet]
        public async Task<IActionResult> Index(string? category = null)
        {
            var sessions = await _classService.GetAvailableSessionsAsync(category);
            var programs = await _classService.GetAllProgramsAsync();
            var categories = programs.Select(p => p.Category).Distinct().ToList();

            var userId = _userManager.GetUserId(User);
            var userBookedSessionIds = new HashSet<int>();

            if (userId != null)
            {
                var userBookings = await _bookingService.GetUserBookingsAsync(userId);
                userBookedSessionIds = userBookings.Select(b => b.ClassSessionId).ToHashSet();
            }

            var sessionDtos = sessions.Select(s => new ClassSessionDto
            {
                Id = s.Id,
                TrainingProgramId = s.TrainingProgramId,
                ProgramTitle = s.TrainingProgram?.Title ?? "Unknown Class",
                Category = s.TrainingProgram?.Category ?? "General",
                DifficultyLevel = s.TrainingProgram?.DifficultyLevel ?? Domain.Enums.DifficultyLevel.Beginner,
                InstructorName = s.InstructorName,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Location = s.Location,
                MaxParticipants = s.MaxParticipants,
                AvailableSpots = s.MaxParticipants - s.Bookings.Count(b => b.Status == Domain.Enums.BookingStatus.Confirmed),
                IsActive = s.IsActive
            }).ToList();

            var viewModel = new ClassesIndexViewModel
            {
                Sessions = sessionDtos,
                Categories = categories,
                SelectedCategory = category,
                UserBookedSessionIds = userBookedSessionIds
            };

            return View(viewModel);
        }

        // POST: /Classes/Book
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(int sessionId)
        {
            var userId = _userManager.GetUserId(User);

            try
            {
                var result = await _bookingService.BookClassAsync(userId, sessionId);
                if (result.IsSuccess)
                {
                    TempData["Success"] = "Class booked successfully!";
                }
                else
                {
                    TempData["Error"] = result.Error;
                }
            }
            catch (DomainException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while booking the class.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Classes/Cancel
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int bookingId, string returnUrl)
        {
            var result = await _bookingService.CancelBookingAsync(bookingId);

            if (result.IsSuccess)
            {
                TempData["Success"] = "Booking cancelled successfully.";
            }
            else
            {
                TempData["Error"] = result.Error;
            }

            return Redirect(returnUrl ?? "/");
        }
    }
}
