using FluentAssertions;
using GymPortal.Domain.Exceptions;
using Xunit;

namespace GymPortal.Tests.UnitTests.Domain.Exceptions
{
    public class DomainExceptionTests
    {
        [Fact]
        public void DuplicateBookingException_ShouldHaveCorrectMessage()
        {
            // Arrange & Act
            var exception = new DuplicateBookingException();

            // Assert
            exception.Message.Should().Be("You have already booked this class.");
        }

        [Fact]
        public void ClassFullException_ShouldHaveCorrectMessage()
        {
            // Arrange & Act
            var exception = new ClassFullException();

            // Assert
            exception.Message.Should().Be("This class is already full.");
        }

        [Fact]
        public void DomainException_ShouldBeAbstract()
        {
            // Assert
            typeof(DomainException).IsAbstract.Should().BeTrue();
        }
    }
}