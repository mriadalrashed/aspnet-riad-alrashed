using Microsoft.AspNetCore.Identity;
using GymPortal.Domain.Common;

namespace GymPortal.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? ProfileImageUrl { get; set; }

        // Navigation Properties
        public Membership? Membership { get; set; }
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }

}
