using GymPortal.Domain.Enums;


namespace GymPortal.Application.DTOs
{
    public class ClassSessionDto
    {
        public int Id { get; set; }
        public int TrainingProgramId { get; set; }
        public string ProgramTitle { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DifficultyLevel DifficultyLevel { get; set; }
        public string InstructorName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Location { get; set; } = string.Empty;
        public int MaxParticipants { get; set; }
        public int AvailableSpots { get; set; }


        // Computed property – no need to store in the database
        public bool IsFull => AvailableSpots <= 0;
        
        public bool IsActive { get; set; }
    }
}
