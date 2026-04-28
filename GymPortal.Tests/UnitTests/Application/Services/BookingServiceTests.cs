using FluentAssertions;
using GymPortal.Application.Interfaces.Repositories;
using GymPortal.Application.Services;
using GymPortal.Domain.Common;
using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using GymPortal.Domain.Exceptions;
using Moq;
using Xunit;

namespace GymPortal.Tests.UnitTests.Services
{
    public class BookingServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IBaseRepository<Booking>> _bookingRepoMock;
        private readonly Mock<IBaseRepository<ClassSession>> _sessionRepoMock;
        private readonly Mock<IBaseRepository<TrainingProgram>> _programRepoMock;
        private readonly BookingService _bookingService;

        public BookingServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _bookingRepoMock = new Mock<IBaseRepository<Booking>>();
            _sessionRepoMock = new Mock<IBaseRepository<ClassSession>>();
            _programRepoMock = new Mock<IBaseRepository<TrainingProgram>>();

            _unitOfWorkMock.Setup(u => u.Repository<Booking>()).Returns(_bookingRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<ClassSession>()).Returns(_sessionRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<TrainingProgram>()).Returns(_programRepoMock.Object);

            _bookingService = new BookingService(_unitOfWorkMock.Object);
        }

        #region BookClassAsync Tests

        [Fact]
        public async Task BookClassAsync_ValidBooking_ShouldSucceed()
        {
            // Arrange
            var userId = "user-123";
            var sessionId = 1;
            var classSession = new ClassSession
            {
                Id = sessionId,
                MaxParticipants = 10,
                StartTime = DateTime.UtcNow.AddDays(1)
            };

            _sessionRepoMock.Setup(r => r.GetByIdAsync(sessionId))
                .ReturnsAsync(classSession);

            // Setup sequence: First call (duplicate check) returns empty, Second call (capacity check) returns empty
            _bookingRepoMock.SetupSequence(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                .ReturnsAsync(new List<Booking>())  // First call: No duplicate
                .ReturnsAsync(new List<Booking>()); // Second call: No existing bookings

            // Act
            var result = await _bookingService.BookClassAsync(userId, sessionId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.UserId.Should().Be(userId);
            result.Value.ClassSessionId.Should().Be(sessionId);
            result.Value.Status.Should().Be(BookingStatus.Confirmed);
            _bookingRepoMock.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task BookClassAsync_ClassSessionNotFound_ShouldReturnFailure()
        {
            // Arrange
            var userId = "user-123";
            var sessionId = 999;

            _sessionRepoMock.Setup(r => r.GetByIdAsync(sessionId))
                .ReturnsAsync((ClassSession)null);

            // Act
            var result = await _bookingService.BookClassAsync(userId, sessionId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Class session not found.");
            _bookingRepoMock.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task BookClassAsync_DuplicateBooking_ShouldThrowDuplicateBookingException()
        {
            // Arrange
            var userId = "user-123";
            var sessionId = 1;
            var classSession = new ClassSession { Id = sessionId, MaxParticipants = 10 };

            // This booking has the SAME userId and classSessionId - this is a duplicate
            var existingBooking = new Booking
            {
                Id = 1,
                UserId = userId,  // SAME user
                ClassSessionId = sessionId,  // SAME class
                Status = BookingStatus.Confirmed
            };

            _sessionRepoMock.Setup(r => r.GetByIdAsync(sessionId))
                .ReturnsAsync(classSession);

            // First call (duplicate check) returns existing booking - this triggers DuplicateBookingException
            // Second call (capacity check) would never be reached
            _bookingRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                .ReturnsAsync(new List<Booking> { existingBooking });

            // Act & Assert
            var exception = await Assert.ThrowsAsync<DuplicateBookingException>(() =>
                _bookingService.BookClassAsync(userId, sessionId));

            exception.Message.Should().Be("You have already booked this class.");
            _bookingRepoMock.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task BookClassAsync_ClassFull_ShouldThrowClassFullException()
        {
            // Arrange
            var userId = "DIFFERENT-user-456";  // Different user than existing bookings
            var sessionId = 1;
            var classSession = new ClassSession { Id = sessionId, MaxParticipants = 2 };

            // Create bookings from OTHER users (different userId)
            var existingBookingsForCapacity = new List<Booking>
            {
                new Booking { Id = 1, UserId = "other-user-1", ClassSessionId = sessionId, Status = BookingStatus.Confirmed },
                new Booking { Id = 2, UserId = "other-user-2", ClassSessionId = sessionId, Status = BookingStatus.Confirmed }
            };

            _sessionRepoMock.Setup(r => r.GetByIdAsync(sessionId))
                .ReturnsAsync(classSession);

            // First call (duplicate check for this user) returns empty (no duplicate)
            // Second call (capacity check) returns existing bookings (class full)
            _bookingRepoMock.SetupSequence(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                .ReturnsAsync(new List<Booking>())  // First call: No duplicate for this user
                .ReturnsAsync(existingBookingsForCapacity);  // Second call: Class is full

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ClassFullException>(() =>
                _bookingService.BookClassAsync(userId, sessionId));

            exception.Message.Should().Be("This class is already full.");
            _bookingRepoMock.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [Fact]
        public async Task BookClassAsync_ClassFullWithOneBooking_ShouldThrowClassFullException()
        {
            // Arrange
            var userId = "new-user-123";
            var sessionId = 1;
            var classSession = new ClassSession { Id = sessionId, MaxParticipants = 1 };

            // One booking from another user makes the class full
            var existingBookingsForCapacity = new List<Booking>
            {
                new Booking { Id = 1, UserId = "other-user", ClassSessionId = sessionId, Status = BookingStatus.Confirmed }
            };

            _sessionRepoMock.Setup(r => r.GetByIdAsync(sessionId))
                .ReturnsAsync(classSession);

            // First call (duplicate check) returns empty, Second call (capacity check) returns existing booking
            _bookingRepoMock.SetupSequence(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                .ReturnsAsync(new List<Booking>())  // First call: No duplicate
                .ReturnsAsync(existingBookingsForCapacity);  // Second call: Class full

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ClassFullException>(() =>
                _bookingService.BookClassAsync(userId, sessionId));

            exception.Message.Should().Be("This class is already full.");
        }

        [Fact]
        public async Task BookClassAsync_WhenClassNotFull_ShouldSucceed()
        {
            // Arrange
            var userId = "new-user-123";
            var sessionId = 1;
            var classSession = new ClassSession { Id = sessionId, MaxParticipants = 5 };

            // Only 2 out of 5 spots filled by other users
            var existingBookingsForCapacity = new List<Booking>
            {
                new Booking { Id = 1, UserId = "user1", ClassSessionId = sessionId, Status = BookingStatus.Confirmed },
                new Booking { Id = 2, UserId = "user2", ClassSessionId = sessionId, Status = BookingStatus.Confirmed }
            };

            _sessionRepoMock.Setup(r => r.GetByIdAsync(sessionId))
                .ReturnsAsync(classSession);

            // First call (duplicate check) returns empty, Second call (capacity check) returns existing bookings (class not full)
            _bookingRepoMock.SetupSequence(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                .ReturnsAsync(new List<Booking>())  // First call: No duplicate
                .ReturnsAsync(existingBookingsForCapacity);  // Second call: Class has 2/5 spots

            // Act
            var result = await _bookingService.BookClassAsync(userId, sessionId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _bookingRepoMock.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Once);
        }

        [Fact]
        public async Task BookClassAsync_ExactlyOneSpotLeft_ShouldSucceed()
        {
            // Arrange
            var userId = "new-user-123";
            var sessionId = 1;
            var classSession = new ClassSession { Id = sessionId, MaxParticipants = 3 };

            // 2 out of 3 spots filled - one spot left
            var existingBookingsForCapacity = new List<Booking>
            {
                new Booking { Id = 1, UserId = "user1", ClassSessionId = sessionId, Status = BookingStatus.Confirmed },
                new Booking { Id = 2, UserId = "user2", ClassSessionId = sessionId, Status = BookingStatus.Confirmed }
            };

            _sessionRepoMock.Setup(r => r.GetByIdAsync(sessionId))
                .ReturnsAsync(classSession);

            _bookingRepoMock.SetupSequence(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                .ReturnsAsync(new List<Booking>())  // First call: No duplicate
                .ReturnsAsync(existingBookingsForCapacity);  // Second call: 2/3 spots

            // Act
            var result = await _bookingService.BookClassAsync(userId, sessionId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _bookingRepoMock.Verify(r => r.AddAsync(It.IsAny<Booking>()), Times.Once);
        }

        #endregion

        #region GetUserBookingsWithClassAsync Tests

        [Fact]
        public async Task GetUserBookingsWithClassAsync_WhenBookingsExist_ShouldReturnBookingsWithClassDetails()
        {
            // Arrange
            var userId = "user-123";
            var trainingProgram = new TrainingProgram { Id = 1, Title = "Yoga", Category = "Wellness" };
            var classSession = new ClassSession
            {
                Id = 1,
                InstructorName = "Jane",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                Location = "Studio A",
                TrainingProgram = trainingProgram
            };
            var bookings = new List<Booking>
            {
                new Booking { Id = 1, UserId = userId, ClassSessionId = 1, ClassSession = classSession }
            };

            _bookingRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                .ReturnsAsync(bookings);
            _sessionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(classSession);
            _programRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(trainingProgram);

            // Act
            var result = await _bookingService.GetUserBookingsWithClassAsync(userId);
            var resultList = result.ToList();

            // Assert
            resultList.Should().HaveCount(1);
            resultList[0].ClassSession.Should().NotBeNull();
            resultList[0].ClassSession.InstructorName.Should().Be("Jane");
            resultList[0].ClassSession.TrainingProgram.Title.Should().Be("Yoga");
        }

        [Fact]
        public async Task GetUserBookingsWithClassAsync_WhenNoBookings_ShouldReturnEmptyList()
        {
            // Arrange
            var userId = "user-123";
            _bookingRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                .ReturnsAsync(new List<Booking>());

            // Act
            var result = await _bookingService.GetUserBookingsWithClassAsync(userId);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserBookingsWithClassAsync_WhenExceptionOccurs_ShouldReturnEmptyList()
        {
            // Arrange
            var userId = "user-123";
            _bookingRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _bookingService.GetUserBookingsWithClassAsync(userId);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserBookingsWithClassAsync_WhenClassSessionIsNull_ShouldFilterOut()
        {
            // Arrange
            var userId = "user-123";
            var bookings = new List<Booking>
            {
                new Booking { Id = 1, UserId = userId, ClassSessionId = 1, ClassSession = null }
            };

            _bookingRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                .ReturnsAsync(bookings);
            _sessionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((ClassSession)null);

            // Act
            var result = await _bookingService.GetUserBookingsWithClassAsync(userId);
            var resultList = result.ToList();

            // Assert
            resultList.Should().BeEmpty();
        }

        #endregion

        #region GetUserBookingsAsync Tests

        [Fact]
        public async Task GetUserBookingsAsync_WhenBookingsExist_ShouldReturnBookings()
        {
            // Arrange
            var userId = "user-123";
            var classSession = new ClassSession
            {
                Id = 1,
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(1)
            };
            var bookings = new List<Booking>
            {
                new Booking { Id = 1, UserId = userId, ClassSessionId = 1 }
            };

            _bookingRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                .ReturnsAsync(bookings);
            _sessionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(classSession);

            // Act
            var result = await _bookingService.GetUserBookingsAsync(userId);
            var resultList = result.ToList();

            // Assert
            resultList.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetUserBookingsAsync_WhenNoBookings_ShouldReturnEmptyList()
        {
            // Arrange
            var userId = "user-123";
            _bookingRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                .ReturnsAsync(new List<Booking>());

            // Act
            var result = await _bookingService.GetUserBookingsAsync(userId);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserBookingsAsync_WhenExceptionOccurs_ShouldReturnEmptyList()
        {
            // Arrange
            var userId = "user-123";
            _bookingRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _bookingService.GetUserBookingsAsync(userId);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetUserBookingsAsync_ShouldOrderByStartTime()
        {
            // Arrange
            var userId = "user-123";
            var classSession1 = new ClassSession { Id = 1, StartTime = DateTime.UtcNow.AddDays(2) };
            var classSession2 = new ClassSession { Id = 2, StartTime = DateTime.UtcNow.AddDays(1) };
            var bookings = new List<Booking>
            {
                new Booking { Id = 1, UserId = userId, ClassSessionId = 1 },
                new Booking { Id = 2, UserId = userId, ClassSessionId = 2 }
            };

            _bookingRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                .ReturnsAsync(bookings);
            _sessionRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(classSession1);
            _sessionRepoMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(classSession2);

            // Act
            var result = await _bookingService.GetUserBookingsAsync(userId);
            var resultList = result.ToList();

            // Assert
            resultList.Should().HaveCount(2);
            // Earlier date should come first (Day 1 before Day 2)
            resultList[0].ClassSessionId.Should().Be(2);
            resultList[1].ClassSessionId.Should().Be(1);
        }

        #endregion

        #region GetBookingsBySessionIdAsync Tests

        [Fact]
        public async Task GetBookingsBySessionIdAsync_WhenBookingsExist_ShouldReturnBookings()
        {
            // Arrange
            var sessionId = 1;
            var bookings = new List<Booking>
            {
                new Booking { Id = 1, ClassSessionId = sessionId, Status = BookingStatus.Confirmed },
                new Booking { Id = 2, ClassSessionId = sessionId, Status = BookingStatus.Confirmed }
            };

            _bookingRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                .ReturnsAsync(bookings);

            // Act
            var result = await _bookingService.GetBookingsBySessionIdAsync(sessionId);
            var resultList = result.ToList();

            // Assert
            resultList.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetBookingsBySessionIdAsync_WhenNoBookings_ShouldReturnEmptyList()
        {
            // Arrange
            var sessionId = 1;
            _bookingRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                .ReturnsAsync(new List<Booking>());

            // Act
            var result = await _bookingService.GetBookingsBySessionIdAsync(sessionId);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetBookingsBySessionIdAsync_WhenExceptionOccurs_ShouldReturnEmptyList()
        {
            // Arrange
            var sessionId = 1;
            _bookingRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _bookingService.GetBookingsBySessionIdAsync(sessionId);

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region CancelBookingAsync Tests

        [Fact]
        public async Task CancelBookingAsync_ValidBooking_ShouldSucceed()
        {
            // Arrange
            var bookingId = 1;
            var booking = new Booking
            {
                Id = bookingId,
                Status = BookingStatus.Confirmed
            };

            _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(booking);

            // Act
            var result = await _bookingService.CancelBookingAsync(bookingId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            booking.Status.Should().Be(BookingStatus.Cancelled);
            _bookingRepoMock.Verify(r => r.Update(booking), Times.Once);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task CancelBookingAsync_BookingNotFound_ShouldReturnFailure()
        {
            // Arrange
            var bookingId = 999;
            _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync((Booking)null);

            // Act
            var result = await _bookingService.CancelBookingAsync(bookingId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Booking not found.");
            _bookingRepoMock.Verify(r => r.Update(It.IsAny<Booking>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task CancelBookingAsync_AlreadyCancelledBooking_ShouldStillSucceed()
        {
            // Arrange
            var bookingId = 1;
            var booking = new Booking
            {
                Id = bookingId,
                Status = BookingStatus.Cancelled
            };

            _bookingRepoMock.Setup(r => r.GetByIdAsync(bookingId))
                .ReturnsAsync(booking);

            // Act
            var result = await _bookingService.CancelBookingAsync(bookingId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            booking.Status.Should().Be(BookingStatus.Cancelled);
            _bookingRepoMock.Verify(r => r.Update(booking), Times.Once);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        }

        #endregion
    }
}