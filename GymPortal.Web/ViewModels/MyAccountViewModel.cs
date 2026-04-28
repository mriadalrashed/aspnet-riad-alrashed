using GymPortal.Application.DTOs;
using System.ComponentModel.DataAnnotations;

namespace GymPortal.Web.ViewModels
{
    public class MyAccountViewModel
    {
        public UserDto User { get; set; } = new();

        [Required(ErrorMessage ="First name is required.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required (ErrorMessage = "Last name is required.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Phone number is required.")]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }
        public IFormFile? ProfileImage { get; set; }
    }
}
