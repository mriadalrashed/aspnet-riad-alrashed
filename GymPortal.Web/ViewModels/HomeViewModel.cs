using GymPortal.Application.DTOs;

namespace GymPortal.Web.ViewModels
{
    public class HomeViewModel
    {
        public List<TrainingProgramDto> TrainingPrograms { get; set; }= new();
        public List<ProductDto> FeaturedProducts { get; set; } = new();
    }
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
    }
}
