using GymPortal.Application.Interfaces.Repositories;
using GymPortal.Application.Interfaces.Services;
using GymPortal.Domain.Common;
using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using GymPortal.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GymPortal.Infrastructure.Services
{
    public class BookingService : IBookingService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BookingService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Booking>> BookClassAsync(string userId, int sessionId)
        {
            var sessionRepo = _unitOfWork.Repository<ClassSession>();
            var bookingRepo = _unitOfWork.Repository<Booking>();

            var session = await sessionRepo.GetByIdAsync(sessionId);
            if (session == null)
                return Result<Booking>.Failure("Class session not found.");

            // Check duplicate confirmed booking
            var existing = (await bookingRepo.FindAsync(b => b.UserId == userId && b.ClassSessionId == sessionId && b.Status == BookingStatus.Confirmed)).FirstOrDefault();
            if (existing != null)
                throw new DuplicateBookingException();

            // Check capacity
            var confirmedCount = (await bookingRepo.FindAsync(b => b.ClassSessionId == sessionId && b.Status == BookingStatus.Confirmed)).Count();
            if (confirmedCount >= session.MaxParticipants)
                throw new ClassFullException();

            var booking = new Booking
            {
                UserId = userId,
                ClassSessionId = sessionId,
                BookingTime = DateTime.UtcNow,
                Status = BookingStatus.Confirmed
            };

            await bookingRepo.AddAsync(booking);
            await _unitOfWork.CompleteAsync();

            return Result<Booking>.Success(booking);
        }

        public async Task<IEnumerable<Booking>> GetUserBookingsWithClassAsync(string userId)
        {
            try
            {
                var bookingRepo = _unitOfWork.Repository<Booking>();
                var sessionRepo = _unitOfWork.Repository<ClassSession>();
                var programRepo = _unitOfWork.Repository<TrainingProgram>(); // Add this

                // Get all confirmed bookings for the user
                var bookings = await bookingRepo.FindAsync(b =>
                    b.UserId == userId &&
                    b.Status == BookingStatus.Confirmed);

                var bookingsList = bookings.ToList();

                // Manually load ClassSession and TrainingProgram for each booking
                foreach (var booking in bookingsList)
                {
                    if (booking.ClassSession == null)
                    {
                        booking.ClassSession = await sessionRepo.GetByIdAsync(booking.ClassSessionId);
                    }

                    // Load TrainingProgram if ClassSession exists but TrainingProgram is null
                    if (booking.ClassSession != null && booking.ClassSession.TrainingProgram == null)
                    {
                        booking.ClassSession.TrainingProgram = await programRepo.GetByIdAsync(booking.ClassSession.TrainingProgramId);
                    }
                }

                // Filter out any with null ClassSession and order by start time
                return bookingsList
                    .Where(b => b.ClassSession != null)
                    .OrderBy(b => b.ClassSession.StartTime)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserBookingsWithClassAsync: {ex.Message}");
                return new List<Booking>();
            }
        }
        public async Task<IEnumerable<Booking>> GetUserBookingsAsync(string userId)
        {
            try
            {
                var bookingRepo = _unitOfWork.Repository<Booking>();
                var sessionRepo = _unitOfWork.Repository<ClassSession>();
                // Get all confirmed bookings for the user
                var bookings = await bookingRepo.FindAsync(b =>
                    b.UserId == userId &&
                    b.Status == BookingStatus.Confirmed);

                var bookingsList = bookings.ToList();

                // Manually load ClassSession for each booking
                foreach (var booking in bookingsList)
                {
                    if (booking.ClassSession == null)
                    {
                        booking.ClassSession = await sessionRepo.GetByIdAsync(booking.ClassSessionId);
                    }
                }

                // Filter out any with null ClassSession and order by start time
                return bookingsList
                    .Where(b => b.ClassSession != null)
                    .OrderBy(b => b.ClassSession.StartTime)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserBookingsAsync: {ex.Message}");
                return new List<Booking>();
            }
        }

        public async Task<IEnumerable<Booking>> GetBookingsBySessionIdAsync(int sessionId)
        {
            try
            {
                var bookingRepo = _unitOfWork.Repository<Booking>();
                var bookings = await bookingRepo.FindAsync(b =>
                    b.ClassSessionId == sessionId);

                return bookings.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetBookingsBySessionIdAsync: {ex.Message}");
                return new List<Booking>();
            }
        }

        public async Task<Result> CancelBookingAsync(int bookingId)
        {
            var bookingRepo = _unitOfWork.Repository<Booking>();
            var booking = await bookingRepo.GetByIdAsync(bookingId);
            if (booking == null)
                return Result.Failure("Booking not found.");

            booking.Status = BookingStatus.Cancelled;
            bookingRepo.Update(booking);
            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }
    }
}