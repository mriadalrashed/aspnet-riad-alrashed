using GymPortal.Application.DTOs;

namespace GymPortal.Web.ViewModels
{
    public class AdminClassesViewModel
    {
        public List<ClassSessionDto> Sessions { get; set; } = new();
        public List<TrainingProgramDto> Programs { get; set; } = new();
        public ClassSessionDto Form { get; set; } = new();
    }
}
