using GymPortal.Domain.Common;
using GymPortal.Domain.Enums;


namespace GymPortal.Domain.Entities
{
    public class ClassSession : BaseEntity
    {
        public int TrainingProgramId { get; set; }
        public TrainingProgram TrainingProgram { get; set; } = null!;

        public string InstructorName { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public string Location { get; set; } = string.Empty;
        public int MaxParticipants { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
