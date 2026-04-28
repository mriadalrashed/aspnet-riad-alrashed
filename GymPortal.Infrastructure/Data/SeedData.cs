using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace GymPortal.Infrastructure.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure database is created (especially for InMemory)
            await context.Database.EnsureCreatedAsync();

            // Seed Roles
            string[] roles = { "Admin", "Member" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed Admin User
            string adminEmail = "admin@gym.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                    EmailConfirmed = true // Auto-confirm admin email
                };

                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                    await userManager.AddToRoleAsync(admin, "Member"); // Admin is also a member
                }
                else
                {
                    // Log errors if needed
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    Console.WriteLine($"Failed to create admin user: {errors}");
                }
            }
            else
            {
                // Ensure existing admin has Admin role
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Seed Training Programs if none exist
            if (!await context.TrainingPrograms.AnyAsync())
            {
                var programs = new List<TrainingProgram>
                {
                    new TrainingProgram
                    {
                        Title = "Yoga Flow",
                        Description = "Relaxing yoga session for all levels",
                        Category = "Yoga",
                        DifficultyLevel = DifficultyLevel.Beginner,
                        ImageUrl = "/images/program1.png"
                    },
                    new TrainingProgram
                    {
                        Title = "HIIT Blast",
                        Description = "High intensity interval training for maximum calorie burn",
                        Category = "Cardio",
                        DifficultyLevel = DifficultyLevel.Intermediate,
                        ImageUrl = "/images/program-2.png"
                    },
                    new TrainingProgram
                    {
                        Title = "Strength Foundation",
                        Description = "Build muscle and strength with proper form",
                        Category = "Strength",
                        DifficultyLevel = DifficultyLevel.Beginner,
                        ImageUrl = "/images/program-3.png"
                    },
                    new TrainingProgram
                    {
                        Title = "Advanced CrossFit",
                        Description = "Intense functional fitness for experienced athletes",
                        Category = "CrossFit",
                        DifficultyLevel = DifficultyLevel.Advanced,
                        ImageUrl = "/images/program-default.png"
                    }
                };

                await context.TrainingPrograms.AddRangeAsync(programs);
                await context.SaveChangesAsync();
            }

            // Seed Class Sessions if none exist
            if (!await context.ClassSessions.AnyAsync())
            {
                var programs = await context.TrainingPrograms.ToDictionaryAsync(p => p.Title);

                var sessions = new List<ClassSession>
                {
                    new ClassSession
                    {
                        TrainingProgramId = programs.GetValueOrDefault("Yoga Flow")?.Id ?? 1,
                        InstructorName = "Anna Smith",
                        StartTime = DateTime.UtcNow.AddDays(1).AddHours(10),
                        EndTime = DateTime.UtcNow.AddDays(1).AddHours(11),
                        Location = "Studio A",
                        MaxParticipants = 20,
                        IsActive = true
                    },
                    new ClassSession
                    {
                        TrainingProgramId = programs.GetValueOrDefault("Yoga Flow")?.Id ?? 1,
                        InstructorName = "Anna Smith",
                        StartTime = DateTime.UtcNow.AddDays(3).AddHours(18),
                        EndTime = DateTime.UtcNow.AddDays(3).AddHours(19),
                        Location = "Studio A",
                        MaxParticipants = 20,
                        IsActive = true
                    },
                    new ClassSession
                    {
                        TrainingProgramId = programs.GetValueOrDefault("HIIT Blast")?.Id ?? 2,
                        InstructorName = "John Doe",
                        StartTime = DateTime.UtcNow.AddDays(2).AddHours(18),
                        EndTime = DateTime.UtcNow.AddDays(2).AddHours(19),
                        Location = "Studio B",
                        MaxParticipants = 15,
                        IsActive = true
                    },
                    new ClassSession
                    {
                        TrainingProgramId = programs.GetValueOrDefault("HIIT Blast")?.Id ?? 2,
                        InstructorName = "John Doe",
                        StartTime = DateTime.UtcNow.AddDays(4).AddHours(9),
                        EndTime = DateTime.UtcNow.AddDays(4).AddHours(10),
                        Location = "Studio B",
                        MaxParticipants = 15,
                        IsActive = true
                    },
                    new ClassSession
                    {
                        TrainingProgramId = programs.GetValueOrDefault("Strength Foundation")?.Id ?? 3,
                        InstructorName = "Mike Johnson",
                        StartTime = DateTime.UtcNow.AddDays(3).AddHours(9),
                        EndTime = DateTime.UtcNow.AddDays(3).AddHours(10),
                        Location = "Weight Room",
                        MaxParticipants = 12,
                        IsActive = true
                    },
                    new ClassSession
                    {
                        TrainingProgramId = programs.GetValueOrDefault("Advanced CrossFit")?.Id ?? 4,
                        InstructorName = "Sarah Wilson",
                        StartTime = DateTime.UtcNow.AddDays(5).AddHours(17),
                        EndTime = DateTime.UtcNow.AddDays(5).AddHours(18),
                        Location = "CrossFit Box",
                        MaxParticipants = 10,
                        IsActive = true
                    }
                };

                await context.ClassSessions.AddRangeAsync(sessions);
                await context.SaveChangesAsync();
            }

            // Optional: Seed a sample member for testing
            string memberEmail = "member@gym.com";
            if (await userManager.FindByEmailAsync(memberEmail) == null)
            {
                var member = new ApplicationUser
                {
                    UserName = memberEmail,
                    Email = memberEmail,
                    FirstName = "Test",
                    LastName = "Member",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(member, "Member@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(member, "Member");
                }
            }
        }
    }
}

