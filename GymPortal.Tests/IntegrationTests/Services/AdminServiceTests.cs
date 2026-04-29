using FluentAssertions;
using GymPortal.Application.DTOs;
using GymPortal.Application.Services;
using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using GymPortal.Infrastructure.Data;
using GymPortal.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace GymPortal.Tests.IntegrationTests.Services
{
    public class AdminServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly UnitOfWork _unitOfWork;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly AdminService _adminService;
        private readonly string _dbName;

        public AdminServiceTests()
        {
            _dbName = $"TestDb_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: _dbName)
                .Options;
            _context = new AppDbContext(options);
            _unitOfWork = new UnitOfWork(_context);

            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object,
                null, null, null, null, null, null, null, null);

            _adminService = new AdminService(_userManagerMock.Object, _unitOfWork);
        }


        [Fact]
        public async Task GetAllSessionsAsync_ShouldReturnSessionsWithProgramDetails()
        {
            // Arrange
            var program = new TrainingProgram
            {
                Title = "Yoga",
                Category = "Wellness",
                DifficultyLevel = DifficultyLevel.Beginner,
                CreatedAt = DateTime.UtcNow
            };
            await _context.TrainingPrograms.AddAsync(program);

            var session = new ClassSession
            {
                TrainingProgramId = program.Id,
                InstructorName = "Jane Instructor",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                Location = "Studio A",
                MaxParticipants = 20,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.ClassSessions.AddAsync(session);
            await _context.SaveChangesAsync();

            // Act
            var result = await _adminService.GetAllSessionsAsync();
            var resultList = result.ToList();

            // Assert
            resultList.Should().HaveCount(1);
            resultList[0].ProgramTitle.Should().Be("Yoga");
            resultList[0].InstructorName.Should().Be("Jane Instructor");
            resultList[0].AvailableSpots.Should().Be(20);
        }

        [Fact]
        public async Task CreateSessionAsync_ValidDto_ShouldCreateSession()
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

            var dto = new ClassSessionDto
            {
                TrainingProgramId = program.Id,
                InstructorName = "New Instructor",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                Location = "Studio B",
                MaxParticipants = 25
            };

            // Act
            var result = await _adminService.CreateSessionAsync(dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var savedSession = await _context.ClassSessions.FirstOrDefaultAsync(s => s.InstructorName == "New Instructor");
            savedSession.Should().NotBeNull();
            savedSession.Location.Should().Be("Studio B");
            savedSession.MaxParticipants.Should().Be(25);
        }

        [Fact]
        public async Task DeleteSessionAsync_ExistingSession_ShouldDeleteSession()
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
                MaxParticipants = 20,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.ClassSessions.AddAsync(session);
            await _context.SaveChangesAsync();

            // Act
            var result = await _adminService.DeleteSessionAsync(session.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var deletedSession = await _context.ClassSessions.FindAsync(session.Id);
            deletedSession.Should().BeNull();
        }

        [Fact]
        public async Task DeleteSessionAsync_NonExistentSession_ShouldReturnFailure()
        {
            // Act
            var result = await _adminService.DeleteSessionAsync(99999);

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
                new TrainingProgram { Title = "Yoga", Category = "Wellness", DifficultyLevel = DifficultyLevel.Beginner, CreatedAt = DateTime.UtcNow },
                new TrainingProgram { Title = "HIIT", Category = "Cardio", DifficultyLevel = DifficultyLevel.Intermediate, CreatedAt = DateTime.UtcNow }
            };
            await _context.TrainingPrograms.AddRangeAsync(programs);
            await _context.SaveChangesAsync();

            // Act
            var result = await _adminService.GetAllProgramsAsync();
            var resultList = result.ToList();

            // Assert
            resultList.Should().HaveCount(2);
            resultList.Should().Contain(p => p.Title == "Yoga");
            resultList.Should().Contain(p => p.Title == "HIIT");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}