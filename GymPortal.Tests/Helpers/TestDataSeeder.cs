using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using GymPortal.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;

namespace GymPortal.Tests.Helpers
{
    public static class TestDataSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // Seed Training Programs
            if (!context.TrainingPrograms.Any())
            {
                var programs = new List<TrainingProgram>
                {
                    new TrainingProgram
                    {
                        Id = 1,
                        Title = "Yoga Flow",
                        Description = "Relaxing yoga session",
                        Category = "Yoga",
                        DifficultyLevel = DifficultyLevel.Beginner,
                        CreatedAt = DateTime.UtcNow
                    },
                    new TrainingProgram
                    {
                        Id = 2,
                        Title = "HIIT Blast",
                        Description = "High intensity training",
                        Category = "Cardio",
                        DifficultyLevel = DifficultyLevel.Intermediate,
                        CreatedAt = DateTime.UtcNow
                    },
                    new TrainingProgram
                    {
                        Id = 3,
                        Title = "Strength Training",
                        Description = "Build muscle",
                        Category = "Strength",
                        DifficultyLevel = DifficultyLevel.Advanced,
                        CreatedAt = DateTime.UtcNow
                    }
                };
                await context.TrainingPrograms.AddRangeAsync(programs);
            }

            // Seed Class Sessions
            if (!context.ClassSessions.Any())
            {
                var sessions = new List<ClassSession>
                {
                    new ClassSession
                    {
                        Id = 1,
                        TrainingProgramId = 1,
                        InstructorName = "Jane Doe",
                        StartTime = DateTime.UtcNow.AddHours(24),
                        EndTime = DateTime.UtcNow.AddHours(25),
                        Location = "Studio A",
                        MaxParticipants = 10,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new ClassSession
                    {
                        Id = 2,
                        TrainingProgramId = 2,
                        InstructorName = "John Smith",
                        StartTime = DateTime.UtcNow.AddHours(48),
                        EndTime = DateTime.UtcNow.AddHours(49),
                        Location = "Studio B",
                        MaxParticipants = 15,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };
                await context.ClassSessions.AddRangeAsync(sessions);
            }

            await context.SaveChangesAsync();
        }
    }
}