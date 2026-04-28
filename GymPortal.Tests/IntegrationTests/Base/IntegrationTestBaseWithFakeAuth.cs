using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using GymPortal.Infrastructure.Data;
using GymPortal.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymPortal.Tests.IntegrationTests.Base
{
    public abstract class IntegrationTestBaseWithFakeAuth : IClassFixture<WebApplicationFactory<Program>>
    {
        protected readonly WebApplicationFactory<Program> Factory;
        protected HttpClient Client;
        protected readonly string TestDbName;

        protected IntegrationTestBaseWithFakeAuth(WebApplicationFactory<Program> factory)
        {
            TestDbName = $"TestDb_{Guid.NewGuid()}";

            Factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext
                    var dbContextDescriptor = services.SingleOrDefault(d =>
                        d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);

                    // Add InMemory database
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase(TestDbName));

                    // Remove existing authentication
                    var authenticationDescriptor = services.SingleOrDefault(d =>
                        d.ServiceType == typeof(AuthenticationOptions));
                    if (authenticationDescriptor != null) services.Remove(authenticationDescriptor);

                    // Add fake authentication
                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = "FakeAuthentication";
                        options.DefaultChallengeScheme = "FakeAuthentication";
                    })
                    .AddScheme<AuthenticationSchemeOptions, FakeAuthenticationHandler>("FakeAuthentication", null);
                });
            });

            Client = Factory.CreateClient();
        }

        protected void SetUserRole(string role)
        {
            // This is handled by the FakeAuthenticationHandler
            // The handler will add the role claim automatically
        }

        protected async Task<TrainingProgram> CreateTestTrainingProgramAsync(string title = null)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var program = new TrainingProgram
            {
                Title = title ?? $"Test Program {Guid.NewGuid()}",
                Description = "Test Description",
                Category = "Test Category",
                DifficultyLevel = DifficultyLevel.Beginner,
                CreatedAt = DateTime.UtcNow
            };

            await context.TrainingPrograms.AddAsync(program);
            await context.SaveChangesAsync();

            return program;
        }

        protected async Task<ClassSession> CreateTestClassSessionAsync(int trainingProgramId, DateTime? startTime = null)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var session = new ClassSession
            {
                TrainingProgramId = trainingProgramId,
                InstructorName = "Test Instructor",
                StartTime = startTime ?? DateTime.UtcNow.AddDays(1),
                EndTime = (startTime ?? DateTime.UtcNow.AddDays(1)).AddHours(1),
                Location = "Studio A",
                MaxParticipants = 20,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await context.ClassSessions.AddAsync(session);
            await context.SaveChangesAsync();

            return session;
        }

        protected async Task<Booking> CreateTestBookingAsync(string userId, int classSessionId)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

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