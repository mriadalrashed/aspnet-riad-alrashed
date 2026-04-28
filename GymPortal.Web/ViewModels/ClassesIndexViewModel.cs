using GymPortal.Application.DTOs;

namespace GymPortal.Web.ViewModels
{
    public class ClassesIndexViewModel
    {
        public List<ClassSessionDto> Sessions { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public string SelectedCategory { get; set; } = string.Empty;
        public HashSet<int> UserBookedSessionId { get; set; } = new();
    }
}   
