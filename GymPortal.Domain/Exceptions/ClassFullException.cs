namespace GymPortal.Domain.Exceptions
{
    public class ClassFullException : DomainException
    {
        public ClassFullException() : base("This class is already full.") { }
    }
}
