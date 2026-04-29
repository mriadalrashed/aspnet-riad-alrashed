using GymPortal.Application.DTOs;

namespace GymPortal.Web.ViewModels
{
    public class MyBookingsViewModel
    {
        public UserDto User { get; set; } = new();
        public List<BookingDto> Bookings { get; set; } = new();
    }
}
