using GymPortal.Domain.Enums;


namespace GymPortal.Application.DTOs
{
    public class BookingDto
    {
        public int Id { get; set; }
        public int ClassSessionId { get; set; }
        public string ProgramTitle { get; set; } = string.Empty;
        public string InstructorName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime BookingTime { get; set; }
    }
}
