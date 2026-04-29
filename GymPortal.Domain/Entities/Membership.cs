using GymPortal.Domain.Common;
using GymPortal.Domain.Enums;

namespace GymPortal.Domain.Entities
{
    public class Membership :BaseEntity
    {
        public string UserId { get; set; } =string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public int? PlanId { get; set; }
        public string? PlanName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public MembershipStatus Status { get; set; }
        public MembershipType Type { get; set; }

    }
}
