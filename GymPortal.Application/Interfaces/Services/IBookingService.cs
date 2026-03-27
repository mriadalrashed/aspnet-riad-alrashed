using GymPortal.Domain.Common;
using GymPortal.Domain.Entities;


namespace GymPortal.Application.Interfaces.Services
{
    public interface IBookingService
    {
        Task<Result<Booking>> BookClassAsync(string userId, int sessionId);
        Task<IEnumerable<Booking>> GetUserBookingsAsync(string userId);
        Task<IEnumerable<Booking>> GetUserBookingsWithClassAsync(string userId);
        Task<IEnumerable<Booking>> GetBookingsBySessionIdAsync(int sessionId);
        Task<Result> CancelBookingAsync(int bookingId);
    }
}
