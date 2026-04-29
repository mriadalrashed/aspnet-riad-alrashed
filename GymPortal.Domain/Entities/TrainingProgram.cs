using GymPortal.Domain.Common;
using GymPortal.Domain.Enums;

namespace GymPortal.Domain.Entities
{
    public class TrainingProgram : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DifficultyLevel DifficultyLevel { get; set; }
        public string? ImageUrl { get; set; }
    }
}
