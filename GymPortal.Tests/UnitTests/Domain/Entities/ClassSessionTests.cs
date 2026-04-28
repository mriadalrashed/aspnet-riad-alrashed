using FluentAssertions;
using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using Xunit;

namespace GymPortal.Tests.UnitTests.Domain.Entities
{
    public class ClassSessionTests
    {
        [Fact]
        public void ClassSession_ShouldInitializeBookingsCollection()
        {
            // Arrange & Act
            var classSession = new ClassSession();

            // Assert
            classSession.Bookings.Should().NotBeNull();
            classSession.Bookings.Should().BeEmpty();
        }

        [Fact]
        public void ClassSession_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var classSession = new ClassSession();
            var startTime = DateTime.UtcNow.AddDays(1);
            var endTime = DateTime.UtcNow.AddDays(1).AddHours(1);
            var trainingProgram = new TrainingProgram { Id = 10, Title = "Yoga" };

            // Act
            classSession.Id = 1;
            classSession.TrainingProgramId = 10;
            classSession.TrainingProgram = trainingProgram;
            classSession.InstructorName = "John Doe";
            classSession.StartTime = startTime;
            classSession.EndTime = endTime;
            classSession.Location = "Studio A";
            classSession.MaxParticipants = 20;
            classSession.IsActive = true;

            // Assert
            classSession.Id.Should().Be(1);
            classSession.TrainingProgramId.Should().Be(10);
            classSession.TrainingProgram.Should().BeSameAs(trainingProgram);
            classSession.InstructorName.Should().Be("John Doe");
            classSession.StartTime.Should().Be(startTime);
            classSession.EndTime.Should().Be(endTime);
            classSession.Location.Should().Be("Studio A");
            classSession.MaxParticipants.Should().Be(20);
            classSession.IsActive.Should().BeTrue();
        }

        [Fact]
        public void ClassSession_ShouldAddBooking()
        {
            // Arrange
            var classSession = new ClassSession();
            var booking = new Booking();

            // Act
            classSession.Bookings.Add(booking);

            // Assert
            classSession.Bookings.Should().Contain(booking);
        }
    }
}