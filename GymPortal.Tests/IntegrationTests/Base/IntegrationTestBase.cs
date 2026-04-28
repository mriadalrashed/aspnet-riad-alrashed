using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using GymPortal.Infrastructure.Data;
using GymPortal.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymPortal.Tests.IntegrationTests.Base
{
    public abstract class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
    {
        protected readonly WebApplicationFactory<Program> Factory;
        protected HttpClient Client;
        protected readonly string TestDbName;

        protected IntegrationTestBase(WebApplicationFactory<Program> factory)
        {
            TestDbName = $"TestDb_{Guid.NewGuid()}";

            Factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration
                    var dbContextDescriptor = services.SingleOrDefault(d =>
                        d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);

                    // Add InMemory database - let the original app configure Identity
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase(TestDbName));
                });
            });

            Client = Factory.CreateClient();
        }

        protected async Task<ApplicationUser> CreateTestUserAsync(string email = null, string role = "Member")
        {
            using var scope = Factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            email ??= $"test{Guid.NewGuid()}@example.com";

            // Ensure role exists
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = email,
                Email = email,
                FirstName = "Test",
                LastName = "User",
            };

            var createResult = await userManager.CreateAsync(user, "Test@123");
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new Exception($"Failed to create user: {errors}");
            }

            await userManager.AddToRoleAsync(user, role);

            return user;
        }

        protected async Task<ApplicationUser> CreateAndLoginUserAsync(string email = null, string role = "Member")
        {
            // First create the user
            var user = await CreateTestUserAsync(email, role);

            // Login via HTTP request to properly set cookies
            var getResponse = await Client.GetAsync("/Account/SignIn");
            var token = await GetAntiForgeryTokenAsync(getResponse);

            var formData = new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Email"] = user.Email,
                ["Password"] = "Test@123",
                ["RememberMe"] = "false"
            };

            var loginResponse = await Client.PostAsync("/Account/SignIn", new FormUrlEncodedContent(formData));

            if (!loginResponse.IsSuccessStatusCode && loginResponse.StatusCode != System.Net.HttpStatusCode.Redirect)
            {
                throw new Exception($"Failed to login user: {loginResponse.StatusCode}");
            }

            return user;
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

        protected async Task<Membership> CreateTestMembershipAsync(string userId, string planName = "Premium")
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var membership = new Membership
            {
                UserId = userId,
                PlanName = planName,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                Status = MembershipStatus.Active,
                Type = MembershipType.Monthly,
                CreatedAt = DateTime.UtcNow
            };

            await context.Memberships.AddAsync(membership);
            await context.SaveChangesAsync();

            return membership;
        }

        protected async Task<string> GetAntiForgeryTokenAsync(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            var tokenStart = content.IndexOf("__RequestVerificationToken");
            if (tokenStart < 0) return string.Empty;

            var valueStart = content.IndexOf("value=\"", tokenStart) + 7;
            var valueEnd = content.IndexOf("\"", valueStart);
            if (valueStart < 0 || valueEnd < 0) return string.Empty;

            return content.Substring(valueStart, valueEnd - valueStart);
        }

        protected async Task<string> GetAntiForgeryTokenFromPageAsync(string url)
        {
            var response = await Client.GetAsync(url);
            return await GetAntiForgeryTokenAsync(response);
        }
    }
}