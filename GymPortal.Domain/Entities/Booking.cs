using GymPortal.Domain.Common;
using GymPortal.Domain.Enums;


namespace GymPortal.Domain.Entities
{
    public class Booking : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public int ClassSessionId { get; set; }
        public ClassSession ClassSession { get; set; } = null!;

        public DateTime BookingTime { get; set; } 
        public BookingStatus Status { get; set; } 

    }
}
