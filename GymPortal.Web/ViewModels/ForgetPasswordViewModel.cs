using System.ComponentModel.DataAnnotations;

namespace GymPortal.Web.ViewModels
{
    public class ForgetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
    }
}
