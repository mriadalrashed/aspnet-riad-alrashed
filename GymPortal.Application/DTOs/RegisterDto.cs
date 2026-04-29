using System.ComponentModel.DataAnnotations;


namespace GymPortal.Application.DTOs
{
    public class RegisterDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;


        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;


        [Required]
        public string FirstName { get; set; } = string.Empty;


        [Required]
        public string LastName { get; set; } = string.Empty;


        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }


        [Phone]
        public string? PhoneNumber { get; set; }
    }
}
