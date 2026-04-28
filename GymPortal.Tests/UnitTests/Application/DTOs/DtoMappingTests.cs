using FluentAssertions;
using GymPortal.Application.DTOs;
using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using Xunit;

namespace GymPortal.Tests.UnitTests.Application.DTOs
{
    public class DtoMappingTests
    {
        [Fact]
        public void UserDto_ShouldMapFromApplicationUser()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "user-123",
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                PhoneNumber = "1234567890",
                DateOfBirth = new DateTime(1990, 1, 1)
            };

            // Act
            var dto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth
            };

            // Assert
            dto.FullName.Should().Be("John Doe");
            dto.Id.Should().Be(user.Id);
            dto.Email.Should().Be(user.Email);
        }

        [Fact]
        public void BookingDto_ShouldContainAllBookingInfo()
        {
            // Arrange
            var booking = new Booking
            {
                Id = 1,
                UserId = "user-123",
                ClassSessionId = 5,
                BookingTime = DateTime.UtcNow,
                Status = BookingStatus.Confirmed
            };

            // Act
            var dto = new BookingDto
            {
                Id = booking.Id,
                ClassSessionId = booking.ClassSessionId,
                BookingTime = booking.BookingTime,
                Status = booking.Status,
                ProgramTitle = "Yoga Class",
                InstructorName = "Jane Instructor",
                Location = "Studio A",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(1)
            };

            // Assert
            dto.Id.Should().Be(booking.Id);
            dto.ClassSessionId.Should().Be(booking.ClassSessionId);
            dto.Status.Should().Be(booking.Status);
            dto.ProgramTitle.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void ClassSessionDto_AvailableSpots_ShouldCalculateCorrectly()
        {
            // Arrange
            var dto = new ClassSessionDto
            {
                MaxParticipants = 20,
                AvailableSpots = 15
            };

            // Assert
            dto.IsFull.Should().BeFalse();
        }

        [Fact]
        public void ClassSessionDto_WhenFull_IsFullShouldBeTrue()
        {
            // Arrange
            var dto = new ClassSessionDto
            {
                MaxParticipants = 20,
                AvailableSpots = 0
            };

            // Assert
            dto.IsFull.Should().BeTrue();
        }

        [Fact]
        public void TrainingProgramDto_ShouldMapFromTrainingProgram()
        {
            // Arrange
            var program = new TrainingProgram
            {
                Id = 1,
                Title = "Yoga Flow",
                Description = "Relaxing yoga",
                Category = "Wellness",
                DifficultyLevel = DifficultyLevel.Beginner,
                ImageUrl = "/images/yoga.jpg"
            };

            // Act
            var dto = new TrainingProgramDto
            {
                Id = program.Id,
                Title = program.Title,
                Description = program.Description,
                Category = program.Category,
                DifficultyLevel = program.DifficultyLevel,
                ImageUrl = program.ImageUrl
            };

            // Assert
            dto.Id.Should().Be(program.Id);
            dto.Title.Should().Be(program.Title);
            dto.Description.Should().Be(program.Description);
            dto.DifficultyLevel.Should().Be(program.DifficultyLevel);
        }
    }
}