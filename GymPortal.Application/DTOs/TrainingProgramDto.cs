using GymPortal.Domain.Enums;


namespace GymPortal.Application.DTOs
{
    public class TrainingProgramDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DifficultyLevel DifficultyLevel { get; set; }
        public string? ImageUrl { get; set; }
    }
}
