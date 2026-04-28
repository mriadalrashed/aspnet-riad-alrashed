namespace GymPortal.Application.DTOs
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;


        // Computed property for convenience
        public string FullName => $"{FirstName} {LastName}".Trim();


        public string? PhoneNumber { get; set; }
        public string? ProfileImageUrl { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime CreatedAt { get; set; }


        // Additional fields that might come from related data
        public string? Role { get; set; }
        public string? MembershipPlanName { get; set; }
        public DateTime? MembershipEndDate { get; set; }
    }
}
