using FluentAssertions;
using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using Xunit;

namespace GymPortal.Tests.UnitTests.Domain.Entities
{
    public class BookingTests
    {
        [Fact]
        public void Booking_ShouldInitializeWithDefaultValues()
        {
            // Arrange & Act
            var booking = new Booking();

            // Assert
            booking.Id.Should().Be(0);
            booking.UserId.Should().BeEmpty();
            booking.User.Should().BeNull();
            booking.ClassSessionId.Should().Be(0);
            booking.ClassSession.Should().BeNull();
            booking.BookingTime.Should().Be(default);
            booking.Status.Should().Be(default(BookingStatus));
        }

        [Fact]
        public void Booking_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var booking = new Booking();
            var bookingTime = DateTime.UtcNow;

            // Act
            booking.Id = 1;
            booking.UserId = "user-123";
            booking.ClassSessionId = 5;
            booking.BookingTime = bookingTime;
            booking.Status = BookingStatus.Confirmed;

            // Assert
            booking.Id.Should().Be(1);
            booking.UserId.Should().Be("user-123");
            booking.ClassSessionId.Should().Be(5);
            booking.BookingTime.Should().Be(bookingTime);
            booking.Status.Should().Be(BookingStatus.Confirmed);
        }

        [Fact]
        public void Booking_UserAndClassSession_ShouldBeNavigable()
        {
            // Arrange
            var user = new ApplicationUser { Id = "user-123", FirstName = "John" };
            var classSession = new ClassSession { Id = 5, InstructorName = "Jane" };
            var booking = new Booking
            {
                User = user,
                ClassSession = classSession
            };

            // Act & Assert
            booking.User.Should().BeSameAs(user);
            booking.ClassSession.Should().BeSameAs(classSession);
        }
    }
}