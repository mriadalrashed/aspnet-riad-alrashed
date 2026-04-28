using GymPortal.Domain.Entities;
using GymPortal.Infrastructure.Data;
using GymPortal.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GymPortal.Tests.IntegrationTests.Base
{
    public abstract class BaseRepositoryTests : IClassFixture<WebApplicationFactory<Program>>
    {
        protected readonly WebApplicationFactory<Program> Factory;
        protected HttpClient Client;

        protected BaseRepositoryTests(WebApplicationFactory<Program> factory)
        {
            Factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d =>
                        d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

                    services.AddIdentity<ApplicationUser, IdentityRole>()
                        .AddEntityFrameworkStores<AppDbContext>()
                        .AddDefaultTokenProviders();
                });
            });

            Client = Factory.CreateClient();
        }

        protected async Task<ApplicationUser> CreateTestUserAsync(string email = null, string role = "Member")
        {
            using var scope = Factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = email ?? $"test{Guid.NewGuid()}@example.com",
                Email = email ?? $"test{Guid.NewGuid()}@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            await userManager.CreateAsync(user, "Test@123");
            await userManager.AddToRoleAsync(user, role);
            return user;
        }
    }
}