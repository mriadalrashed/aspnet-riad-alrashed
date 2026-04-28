using System.ComponentModel.DataAnnotations;


namespace GymPortal.Application.DTOs
{
    public class ContactFormDto
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;


        [Required]
        public string LastName { get; set; } = string.Empty;


        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;


        [Phone]
        public string? PhoneNumber { get; set; }


        [Required]
        public string Message { get; set; } = string.Empty;
    }
}
