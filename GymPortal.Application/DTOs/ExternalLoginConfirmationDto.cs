using System.ComponentModel.DataAnnotations;


namespace GymPortal.Application.DTOs
{
    public class ExternalLoginConfirmationDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;


        public string Name { get; set; } = string.Empty;
    }
}
