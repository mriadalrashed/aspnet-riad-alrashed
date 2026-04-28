using FluentAssertions;
using GymPortal.Application.Services;
using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using GymPortal.Domain.Exceptions;
using GymPortal.Infrastructure.Data;
using GymPortal.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GymPortal.Tests.IntegrationTests.Services
{
    public class BookingServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly UnitOfWork _unitOfWork;
        private readonly BookingService _bookingService;
        private readonly string _dbName;

        public BookingServiceTests()
        {
            _dbName = $"TestDb_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: _dbName)
                .Options;
            _context = new AppDbContext(options);
            _unitOfWork = new UnitOfWork(_context);
            _bookingService = new BookingService(_unitOfWork);
        }

        [Fact]
        public async Task BookClassAsync_ValidBooking_ShouldCreateBooking()
        {
            // Arrange
            var userId = "user-123";
            var trainingProgram = new TrainingProgram
            {
                Title = "Yoga",
                Category = "Wellness",
                DifficultyLevel = DifficultyLevel.Beginner,
                CreatedAt = DateTime.UtcNow
            };
            await _context.TrainingPrograms.AddAsync(trainingProgram);

            var classSession = new ClassSession
            {
                TrainingProgramId = trainingProgram.Id,
                InstructorName = "Jane Doe",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                Location = "Studio A",
                MaxParticipants = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.ClassSessions.AddAsync(classSession);
            await _context.SaveChangesAsync();

            // Act
            var result = await _bookingService.BookClassAsync(userId, classSession.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.UserId.Should().Be(userId);
            result.Value.ClassSessionId.Should().Be(classSession.Id);
            result.Value.Status.Should().Be(BookingStatus.Confirmed);

            var savedBooking = await _context.Bookings.FirstOrDefaultAsync(b => b.UserId == userId);
            savedBooking.Should().NotBeNull();
            savedBooking.BookingTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task BookClassAsync_ClassNotFound_ShouldReturnFailure()
        {
            // Arrange
            var userId = "user-123";
            var nonExistentSessionId = 99999;

            // Act
            var result = await _bookingService.BookClassAsync(userId, nonExistentSessionId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Class session not found.");
        }

        [Fact]
        public async Task BookClassAsync_DuplicateBooking_ShouldThrowDuplicateBookingException()
        {
            // Arrange
            var userId = "user-123";
            var trainingProgram = new TrainingProgram
            {
                Title = "Yoga",
                Category = "Wellness",
                CreatedAt = DateTime.UtcNow
            };
            await _context.TrainingPrograms.AddAsync(trainingProgram);

            var classSession = new ClassSession
            {
                TrainingProgramId = trainingProgram.Id,
                InstructorName = "Jane Doe",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                Location = "Studio A",
                MaxParticipants = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.ClassSessions.AddAsync(classSession);

            // Create existing booking
            var existingBooking = new Booking
            {
                UserId = userId,
                ClassSessionId = classSession.Id,
                BookingTime = DateTime.UtcNow,
                Status = BookingStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Bookings.AddAsync(existingBooking);
            await _context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<DuplicateBookingException>(() =>
                _bookingService.BookClassAsync(userId, classSession.Id));
        }

        [Fact]
        public async Task BookClassAsync_ClassFull_ShouldThrowClassFullException()
        {
            // Arrange
            var userId = "user-456"; // Different user
            var trainingProgram = new TrainingProgram
            {
                Title = "Yoga",
                Category = "Wellness",
                CreatedAt = DateTime.UtcNow
            };
            await _context.TrainingPrograms.AddAsync(trainingProgram);

            var classSession = new ClassSession
            {
                TrainingProgramId = trainingProgram.Id,
                InstructorName = "Jane Doe",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                Location = "Studio A",
                MaxParticipants = 1, // Capacity = 1
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.ClassSessions.AddAsync(classSession);

            // Create existing booking from another user to fill the class
            var otherUserId = "other-user-789";
            var existingBooking = new Booking
            {
                UserId = otherUserId,
                ClassSessionId = classSession.Id,
                BookingTime = DateTime.UtcNow,
                Status = BookingStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Bookings.AddAsync(existingBooking);
            await _context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<ClassFullException>(() =>
                _bookingService.BookClassAsync(userId, classSession.Id));
        }

        [Fact]
        public async Task GetUserBookingsWithClassAsync_WhenBookingsExist_ShouldReturnBookingsWithDetails()
        {
            // Arrange
            var userId = "user-123";
            var trainingProgram = new TrainingProgram
            {
                Title = "Yoga",
                Category = "Wellness",
                DifficultyLevel = DifficultyLevel.Beginner,
                CreatedAt = DateTime.UtcNow
            };
            await _context.TrainingPrograms.AddAsync(trainingProgram);

            var classSession = new ClassSession
            {
                TrainingProgramId = trainingProgram.Id,
                InstructorName = "Jane Doe",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                Location = "Studio A",
                MaxParticipants = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.ClassSessions.AddAsync(classSession);

            var booking = new Booking
            {
                UserId = userId,
                ClassSessionId = classSession.Id,
                BookingTime = DateTime.UtcNow,
                Status = BookingStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync();

            // Act
            var result = await _bookingService.GetUserBookingsWithClassAsync(userId);
            var resultList = result.ToList();

            // Assert
            resultList.Should().HaveCount(1);
            resultList[0].ClassSession.Should().NotBeNull();
            resultList[0].ClassSession.InstructorName.Should().Be("Jane Doe");
            resultList[0].ClassSession.TrainingProgram.Title.Should().Be("Yoga");
        }

        [Fact]
        public async Task GetUserBookingsWithClassAsync_WhenNoBookings_ShouldReturnEmptyList()
        {
            // Arrange
            var userId = "user-123";

            // Act
            var result = await _bookingService.GetUserBookingsWithClassAsync(userId);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserBookingsAsync_WhenBookingsExist_ShouldReturnBookings()
        {
            // Arrange
            var userId = "user-123";
            var trainingProgram = new TrainingProgram
            {
                Title = "Yoga",
                Category = "Wellness",
                CreatedAt = DateTime.UtcNow
            };
            await _context.TrainingPrograms.AddAsync(trainingProgram);

            var classSession = new ClassSession
            {
                TrainingProgramId = trainingProgram.Id,
                InstructorName = "Jane Doe",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                Location = "Studio A",
                MaxParticipants = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.ClassSessions.AddAsync(classSession);

            var booking = new Booking
            {
                UserId = userId,
                ClassSessionId = classSession.Id,
                BookingTime = DateTime.UtcNow,
                Status = BookingStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync();

            // Act
            var result = await _bookingService.GetUserBookingsAsync(userId);
            var resultList = result.ToList();

            // Assert
            resultList.Should().HaveCount(1);
            resultList[0].Id.Should().Be(booking.Id);
        }

        [Fact]
        public async Task GetBookingsBySessionIdAsync_WhenBookingsExist_ShouldReturnBookings()
        {
            // Arrange
            var trainingProgram = new TrainingProgram
            {
                Title = "Yoga",
                Category = "Wellness",
                CreatedAt = DateTime.UtcNow
            };
            await _context.TrainingPrograms.AddAsync(trainingProgram);

            var classSession = new ClassSession
            {
                TrainingProgramId = trainingProgram.Id,
                InstructorName = "Jane Doe",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                Location = "Studio A",
                MaxParticipants = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.ClassSessions.AddAsync(classSession);

            var booking1 = new Booking
            {
                UserId = "user-1",
                ClassSessionId = classSession.Id,
                BookingTime = DateTime.UtcNow,
                Status = BookingStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };
            var booking2 = new Booking
            {
                UserId = "user-2",
                ClassSessionId = classSession.Id,
                BookingTime = DateTime.UtcNow,
                Status = BookingStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Bookings.AddRangeAsync(booking1, booking2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _bookingService.GetBookingsBySessionIdAsync(classSession.Id);
            var resultList = result.ToList();

            // Assert
            resultList.Should().HaveCount(2);
        }

        [Fact]
        public async Task CancelBookingAsync_ValidBooking_ShouldCancelBooking()
        {
            // Arrange
            var userId = "user-123";
            var trainingProgram = new TrainingProgram
            {
                Title = "Yoga",
                Category = "Wellness",
                CreatedAt = DateTime.UtcNow
            };
            await _context.TrainingPrograms.AddAsync(trainingProgram);

            var classSession = new ClassSession
            {
                TrainingProgramId = trainingProgram.Id,
                InstructorName = "Jane Doe",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                Location = "Studio A",
                MaxParticipants = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.ClassSessions.AddAsync(classSession);

            var booking = new Booking
            {
                UserId = userId,
                ClassSessionId = classSession.Id,
                BookingTime = DateTime.UtcNow,
                Status = BookingStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync();

            // Act
            var result = await _bookingService.CancelBookingAsync(booking.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var cancelledBooking = await _context.Bookings.FindAsync(booking.Id);
            cancelledBooking.Status.Should().Be(BookingStatus.Cancelled);
        }

        [Fact]
        public async Task CancelBookingAsync_BookingNotFound_ShouldReturnFailure()
        {
            // Arrange
            var nonExistentBookingId = 99999;

            // Act
            var result = await _bookingService.CancelBookingAsync(nonExistentBookingId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Booking not found.");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}