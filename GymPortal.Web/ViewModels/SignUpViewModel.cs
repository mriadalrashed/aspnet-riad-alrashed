using System.ComponentModel.DataAnnotations;

namespace GymPortal.Web.ViewModels
{
    public class SignUpViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;
        [Phone]
        public string? PhoneNumber { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree.")]
        public bool AgreeToTerms { get; set; }
    }
}
