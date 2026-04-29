using GymPortal.Domain.Enums;


namespace GymPortal.Application.DTOs
{
    public class MembershipDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? PlanName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public MembershipStatus Status { get; set; }
        public MembershipType Type { get; set; }
    }
}
