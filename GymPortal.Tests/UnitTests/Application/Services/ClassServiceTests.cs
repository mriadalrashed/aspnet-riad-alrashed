using FluentAssertions;
using GymPortal.Application.Interfaces.Repositories;
using GymPortal.Application.Services;
using GymPortal.Domain.Common;
using GymPortal.Domain.Entities;
using Moq;
using Xunit;

namespace GymPortal.Tests.UnitTests.Services
{
    public class ClassServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IBaseRepository<ClassSession>> _sessionRepoMock;
        private readonly Mock<IBaseRepository<TrainingProgram>> _programRepoMock;
        private readonly ClassService _classService;

        public ClassServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _sessionRepoMock = new Mock<IBaseRepository<ClassSession>>();
            _programRepoMock = new Mock<IBaseRepository<TrainingProgram>>();

            _unitOfWorkMock.Setup(u => u.Repository<ClassSession>()).Returns(_sessionRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<TrainingProgram>()).Returns(_programRepoMock.Object);

            _classService = new ClassService(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task GetAvailableSessionsAsync_ShouldReturnOnlyFutureActiveSessions()
        {
            // Arrange
            var futureSession = new ClassSession
            {
                Id = 1,
                IsActive = true,
                StartTime = DateTime.UtcNow.AddDays(1)
            };
            var pastSession = new ClassSession
            {
                Id = 2,
                IsActive = true,
                StartTime = DateTime.UtcNow.AddDays(-1)
            };
            var inactiveSession = new ClassSession
            {
                Id = 3,
                IsActive = false,
                StartTime = DateTime.UtcNow.AddDays(1)
            };

            _sessionRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<ClassSession> { futureSession, pastSession, inactiveSession });

            // Act
            var result = await _classService.GetAvailableSessionsAsync();

            // Assert
            result.Should().HaveCount(1);
            result.First().Id.Should().Be(1);
        }

        [Fact]
        public async Task GetAvailableSessionsAsync_WithCategory_ShouldFilterByCategory()
        {
            // Arrange
            var category = "Yoga";
            var program1 = new TrainingProgram { Id = 1, Category = "Yoga" };
            var program2 = new TrainingProgram { Id = 2, Category = "Cardio" };

            var session1 = new ClassSession
            {
                Id = 1,
                TrainingProgramId = 1,
                IsActive = true,
                StartTime = DateTime.UtcNow.AddDays(1)
            };
            var session2 = new ClassSession
            {
                Id = 2,
                TrainingProgramId = 2,
                IsActive = true,
                StartTime = DateTime.UtcNow.AddDays(1)
            };

            _sessionRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<ClassSession> { session1, session2 });
            _programRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<TrainingProgram> { program1, program2 });

            // Act
            var result = await _classService.GetAvailableSessionsAsync(category);

            // Assert
            result.Should().HaveCount(1);
            result.First().Id.Should().Be(1);
        }

        [Fact]
        public async Task GetSessionByIdAsync_ExistingSession_ShouldReturnSession()
        {
            // Arrange
            var sessionId = 1;
            var session = new ClassSession { Id = sessionId };

            _sessionRepoMock.Setup(r => r.GetByIdAsync(sessionId))
                .ReturnsAsync(session);

            // Act
            var result = await _classService.GetSessionByIdAsync(sessionId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(sessionId);
        }

        [Fact]
        public async Task CreateSessionAsync_ValidSession_ShouldSucceed()
        {
            // Arrange
            var session = new ClassSession
            {
                InstructorName = "John Doe",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                MaxParticipants = 20,
                IsActive = true
            };

            // Act
            var result = await _classService.CreateSessionAsync(session);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeSameAs(session);
            _sessionRepoMock.Verify(r => r.AddAsync(session), Times.Once);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteSessionAsync_ExistingSession_ShouldSucceed()
        {
            // Arrange
            var sessionId = 1;
            var session = new ClassSession { Id = sessionId };

            _sessionRepoMock.Setup(r => r.GetByIdAsync(sessionId))
                .ReturnsAsync(session);

            // Act
            var result = await _classService.DeleteSessionAsync(sessionId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _sessionRepoMock.Verify(r => r.Delete(session), Times.Once);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteSessionAsync_NonExistingSession_ShouldReturnFailure()
        {
            // Arrange
            var sessionId = 999;

            _sessionRepoMock.Setup(r => r.GetByIdAsync(sessionId))
                .ReturnsAsync((ClassSession)null);

            // Act
            var result = await _classService.DeleteSessionAsync(sessionId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Session not found.");
        }

        [Fact]
        public async Task GetAllProgramsAsync_ShouldReturnAllPrograms()
        {
            // Arrange
            var programs = new List<TrainingProgram>
            {
                new TrainingProgram { Id = 1, Title = "Yoga" },
                new TrainingProgram { Id = 2, Title = "HIIT" }
            };

            _programRepoMock.Setup(r => r.GetAllAsync())
                .ReturnsAsync(programs);

            // Act
            var result = await _classService.GetAllProgramsAsync();

            // Assert
            result.Should().HaveCount(2);
        }
    }
}