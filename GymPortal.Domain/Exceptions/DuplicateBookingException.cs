namespace GymPortal.Domain.Exceptions
{
    public class DuplicateBookingException : DomainException
    {
        public DuplicateBookingException() : base("You have already booked this class.") { }
    }
}
