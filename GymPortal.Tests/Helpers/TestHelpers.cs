using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using GymPortal.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymPortal.Tests.Helpers
{
    public static class TestHelpers
    {
        public static AppDbContext CreateInMemoryDbContext(string databaseName = null)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        public static async Task<ApplicationUser> CreateTestUserAsync(UserManager<ApplicationUser> userManager, string email = null, string role = "Member")
        {
            email ??= $"test{Guid.NewGuid()}@example.com";
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = email,
                Email = email,
                FirstName = "Test",
                LastName = "User",
            };
            await userManager.CreateAsync(user, "Test@123");
            await userManager.AddToRoleAsync(user, role);
            return user;
        }

        public static async Task<TrainingProgram> CreateTestTrainingProgramAsync(AppDbContext context, string title = null)
        {
            var program = new TrainingProgram
            {
                Title = title ?? $"Program_{Guid.NewGuid()}",
                Description = "Test Description",
                Category = "Test Category",
                DifficultyLevel = DifficultyLevel.Beginner,
                CreatedAt = DateTime.UtcNow
            };
            await context.TrainingPrograms.AddAsync(program);
            await context.SaveChangesAsync();
            return program;
        }

        public static async Task<ClassSession> CreateTestClassSessionAsync(AppDbContext context, int trainingProgramId)
        {
            var session = new ClassSession
            {
                TrainingProgramId = trainingProgramId,
                InstructorName = "Test Instructor",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                Location = "Studio A",
                MaxParticipants = 20,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await context.ClassSessions.AddAsync(session);
            await context.SaveChangesAsync();
            return session;
        }

        public static async Task<Booking> CreateTestBookingAsync(AppDbContext context, string userId, int classSessionId)
        {
            var booking = new Booking
            {
                UserId = userId,
                ClassSessionId = classSessionId,
                BookingTime = DateTime.UtcNow,
                Status = BookingStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };
            await context.Bookings.AddAsync(booking);
            await context.SaveChangesAsync();
            return booking;
        }
    }
}