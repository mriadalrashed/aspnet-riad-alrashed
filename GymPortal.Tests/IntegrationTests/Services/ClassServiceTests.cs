using FluentAssertions;
using GymPortal.Application.Services;
using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using GymPortal.Infrastructure.Data;
using GymPortal.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GymPortal.Tests.IntegrationTests.Services
{
    public class ClassServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly UnitOfWork _unitOfWork;
        private readonly ClassService _classService;
        private readonly string _dbName;

        public ClassServiceTests()
        {
            _dbName = $"TestDb_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: _dbName)
                .Options;
            _context = new AppDbContext(options);
            _unitOfWork = new UnitOfWork(_context);
            _classService = new ClassService(_unitOfWork);
        }

        [Fact]
        public async Task GetAvailableSessionsAsync_ShouldReturnOnlyFutureActiveSessions()
        {
            // Arrange
            var program = new TrainingProgram
            {
                Title = "Yoga",
                Category = "Wellness",
                CreatedAt = DateTime.UtcNow
            };
            await _context.TrainingPrograms.AddAsync(program);

            var futureSession = new ClassSession
            {
                TrainingProgramId = program.Id,
                InstructorName = "Future Instructor",
                StartTime = DateTime.UtcNow.AddDays(2),
                EndTime = DateTime.UtcNow.AddDays(2).AddHours(1),
                Location = "Studio A",
                MaxParticipants = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var pastSession = new ClassSession
            {
                TrainingProgramId = program.Id,
                InstructorName = "Past Instructor",
                StartTime = DateTime.UtcNow.AddDays(-2),
                EndTime = DateTime.UtcNow.AddDays(-2).AddHours(1),
                Location = "Studio B",
                MaxParticipants = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var inactiveSession = new ClassSession
            {
                TrainingProgramId = program.Id,
                InstructorName = "Inactive Instructor",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                Location = "Studio C",
                MaxParticipants = 10,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };

            await _context.ClassSessions.AddRangeAsync(futureSession, pastSession, inactiveSession);
            await _context.SaveChangesAsync();

            // Act
            var result = await _classService.GetAvailableSessionsAsync();
            var resultList = result.ToList();

            // Assert
            resultList.Should().HaveCount(1);
            resultList[0].InstructorName.Should().Be("Future Instructor");
        }

        [Fact]
        public async Task GetAvailableSessionsAsync_WithCategory_ShouldFilterByCategory()
        {
            // Arrange
            var yogaProgram = new TrainingProgram
            {
                Title = "Yoga",
                Category = "Yoga",
                CreatedAt = DateTime.UtcNow
            };
            var cardioProgram = new TrainingProgram
            {
                Title = "HIIT",
                Category = "Cardio",
                CreatedAt = DateTime.UtcNow
            };
            await _context.TrainingPrograms.AddRangeAsync(yogaProgram, cardioProgram);

            var yogaSession = new ClassSession
            {
                TrainingProgramId = yogaProgram.Id,
                InstructorName = "Yoga Instructor",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                Location = "Studio A",
                MaxParticipants = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var cardioSession = new ClassSession
            {
                TrainingProgramId = cardioProgram.Id,
                InstructorName = "Cardio Instructor",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                Location = "Studio B",
                MaxParticipants = 15,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.ClassSessions.AddRangeAsync(yogaSession, cardioSession);
            await _context.SaveChangesAsync();

            // Act
            var result = await _classService.GetAvailableSessionsAsync("Yoga");
            var resultList = result.ToList();

            // Assert
            resultList.Should().HaveCount(1);
            resultList[0].InstructorName.Should().Be("Yoga Instructor");
        }

        [Fact]
        public async Task GetSessionByIdAsync_ExistingSession_ShouldReturnSessionWithProgram()
        {
            // Arrange
            var program = new TrainingProgram
            {
                Title = "Yoga",
                Category = "Wellness",
                CreatedAt = DateTime.UtcNow
            };
            await _context.TrainingPrograms.AddAsync(program);

            var session = new ClassSession
            {
                TrainingProgramId = program.Id,
                InstructorName = "Test Instructor",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                Location = "Studio A",
                MaxParticipants = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.ClassSessions.AddAsync(session);
            await _context.SaveChangesAsync();

            // Act
            var result = await _classService.GetSessionByIdAsync(session.Id);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(session.Id);
            result.InstructorName.Should().Be("Test Instructor");
        }

        [Fact]
        public async Task CreateSessionAsync_ValidSession_ShouldPersistToDatabase()
        {
            // Arrange
            var program = new TrainingProgram
            {
                Title = "Yoga",
                Category = "Wellness",
                CreatedAt = DateTime.UtcNow
            };
            await _context.TrainingPrograms.AddAsync(program);
            await _context.SaveChangesAsync();

            var session = new ClassSession
            {
                TrainingProgramId = program.Id,
                InstructorName = "New Instructor",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                Location = "Studio A",
                MaxParticipants = 20,
                IsActive = true
            };

            // Act
            var result = await _classService.CreateSessionAsync(session);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var savedSession = await _context.ClassSessions.FirstOrDefaultAsync(s => s.InstructorName == "New Instructor");
            savedSession.Should().NotBeNull();
            savedSession.Location.Should().Be("Studio A");
        }

        [Fact]
        public async Task DeleteSessionAsync_ExistingSession_ShouldRemoveFromDatabase()
        {
            // Arrange
            var program = new TrainingProgram
            {
                Title = "Yoga",
                Category = "Wellness",
                CreatedAt = DateTime.UtcNow
            };
            await _context.TrainingPrograms.AddAsync(program);

            var session = new ClassSession
            {
                TrainingProgramId = program.Id,
                InstructorName = "To Delete",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                Location = "Studio A",
                MaxParticipants = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.ClassSessions.AddAsync(session);
            await _context.SaveChangesAsync();

            // Act
            var result = await _classService.DeleteSessionAsync(session.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var deletedSession = await _context.ClassSessions.FindAsync(session.Id);
            deletedSession.Should().BeNull();
        }

        [Fact]
        public async Task GetAllProgramsAsync_ShouldReturnAllPrograms()
        {
            // Arrange
            var programs = new List<TrainingProgram>
            {
                new TrainingProgram { Title = "Yoga", Category = "Wellness", DifficultyLevel = DifficultyLevel.Beginner, CreatedAt = DateTime.UtcNow },
                new TrainingProgram { Title = "HIIT", Category = "Cardio", DifficultyLevel = DifficultyLevel.Intermediate, CreatedAt = DateTime.UtcNow },
                new TrainingProgram { Title = "Pilates", Category = "Wellness", DifficultyLevel = DifficultyLevel.Beginner, CreatedAt = DateTime.UtcNow }
            };
            await _context.TrainingPrograms.AddRangeAsync(programs);
            await _context.SaveChangesAsync();

            // Act
            var result = await _classService.GetAllProgramsAsync();
            var resultList = result.ToList();

            // Assert
            resultList.Should().HaveCount(3);
            resultList.Should().Contain(p => p.Title == "Yoga");
            resultList.Should().Contain(p => p.Title == "HIIT");
            resultList.Should().Contain(p => p.Title == "Pilates");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}