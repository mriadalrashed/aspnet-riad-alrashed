using GymPortal.Application.Interfaces.Services;
using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using GymPortal.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymPortal.Web.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IClassService _ClassService;
        private readonly UserManager<IdentityUser> _userManager;

        public BookingController(
            IBookingService bookingService,
            IClassService ClassService,
            UserManager<IdentityUser> userManager)
        {
            _bookingService = bookingService;
            _ClassService = ClassService;
            _userManager = userManager;
        }

        // GET: /Booking/MyBookings
        public async Task<IActionResult> MyBookings()
        {
            var userId = _userManager.GetUserId(User);
            var bookings = await _bookingService.GetUserBookingsWithClassAsync(userId);

            return View(bookings);
        }

        // POST: /Booking/BookClass
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookClass(int classSessionId)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var result = await _bookingService.BookClassAsync(userId, classSessionId);

                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Class booked successfully!";
                    return RedirectToAction("MyBookings");
                }

                TempData["ErrorMessage"] = result.Error;
                return RedirectToAction("Details", "Class", new { id = classSessionId });
            }
            catch (DuplicateBookingException)
            {
                TempData["ErrorMessage"] = "You have already booked this class.";
                return RedirectToAction("Details", "Class", new { id = classSessionId });
            }
            catch (ClassFullException)
            {
                TempData["ErrorMessage"] = "This class is already full.";
                return RedirectToAction("Details", "Class", new { id = classSessionId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while booking the class.";
                // Log the exception
                return RedirectToAction("Details", "Class", new { id = classSessionId });
            }
        }

        // POST: /Booking/CancelBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            try
            {
                // Verify the booking belongs to the current user
                var userId = _userManager.GetUserId(User);
                var bookings = await _bookingService.GetUserBookingsAsync(userId);
                var booking = bookings.FirstOrDefault(b => b.Id == bookingId);

                if (booking == null)
                {
                    return Forbid(); // User doesn't own this booking
                }

                var result = await _bookingService.CancelBookingAsync(bookingId);

                if (result.IsSuccess)
                {
                    TempData["SuccessMessage"] = "Booking cancelled successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = result.Error;
                }

                return RedirectToAction("MyBookings");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while cancelling the booking.";
                // Log the exception
                return RedirectToAction("MyBookings");
            }
        }

        // GET: /Booking/CheckAvailability
        [HttpGet]
        public async Task<IActionResult> CheckAvailability(int classSessionId)
        {
            var classSession = await _ClassService.GetSessionByIdAsync(classSessionId);
            if (classSession == null)
            {
                return Json(new { available = false, message = "Class not found." });
            }

            var bookings = await _bookingService.GetBookingsBySessionIdAsync(classSessionId);
            var confirmedCount = bookings.Count(b => b.Status == BookingStatus.Confirmed);
            var available = confirmedCount < classSession.MaxParticipants;

            return Json(new
            {
                available = available,
                bookedCount = confirmedCount,
                maxParticipants = classSession.MaxParticipants,
                availableSpots = classSession.MaxParticipants - confirmedCount
            });
        }

        //In TheFuture: Add separate views for upcoming and past bookings

        //// GET: /Booking/Upcoming
        //public async Task<IActionResult> Upcoming()
        //{
        //    var userId = _userManager.GetUserId(User);
        //    var bookings = await _bookingService.GetUserBookingsWithClassAsync(userId);

        //    // Filter for upcoming classes (start time > now)
        //    var upcomingBookings = bookings
        //        .Where(b => b.ClassSession != null && b.ClassSession.StartTime > DateTime.Now)
        //        .OrderBy(b => b.ClassSession.StartTime);

        //    return View(upcomingBookings);
        //}

        //// GET: /Booking/History
        //public async Task<IActionResult> History()
        //{
        //    var userId = _userManager.GetUserId(User);
        //    var bookings = await _bookingService.GetUserBookingsWithClassAsync(userId);

        //    // Filter for past classes (start time < now)
        //    var pastBookings = bookings
        //        .Where(b => b.ClassSession != null && b.ClassSession.StartTime < DateTime.Now)
        //        .OrderByDescending(b => b.ClassSession.StartTime);

        //    return View(pastBookings);
        //}
    }
}
