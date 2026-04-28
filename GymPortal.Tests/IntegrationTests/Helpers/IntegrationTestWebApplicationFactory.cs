using GymPortal.Domain.Entities;
using GymPortal.Infrastructure.Data;
using GymPortal.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GymPortal.Tests.IntegrationTests.Helpers
{
    public class IntegrationTestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName;

        public IntegrationTestWebApplicationFactory()
        {
            _databaseName = $"TestDb_{Guid.NewGuid()}";
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // 1. Remove ALL existing DbContext registrations
                var dbContextDescriptors = services.Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(AppDbContext) ||
                    d.ImplementationType == typeof(AppDbContext)).ToList();

                foreach (var descriptor in dbContextDescriptors)
                {
                    services.Remove(descriptor);
                }

                // 2. Remove ALL Identity-related services
                var identityServiceTypes = new[]
                {
                    "UserManager", "SignInManager", "RoleManager", "IUserStore", "IRoleStore",
                    "IUserClaimsPrincipalFactory", "IdentityErrorDescriber", "IdentityOptions",
                    "IPasswordHasher", "ILookupNormalizer", "IRoleValidator", "IUserValidator"
                };

                var identityServices = services.Where(s =>
                    identityServiceTypes.Any(t => s.ServiceType.Name.Contains(t)) ||
                    s.ServiceType.FullName?.Contains("Identity") == true ||
                    s.ImplementationType?.FullName?.Contains("Identity") == true).ToList();

                foreach (var service in identityServices)
                {
                    services.Remove(service);
                }

                // 3. Remove ALL Authentication services
                var authServices = services.Where(s =>
                    s.ServiceType.Name.Contains("Authentication") ||
                    s.ServiceType.Name.Contains("Authorization") ||
                    s.ServiceType.Name.Contains("Cookie")).ToList();

                foreach (var service in authServices)
                {
                    services.Remove(service);
                }

                // 4. Add InMemory database
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });

                // 5. Add Identity with fresh configuration
                services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequiredLength = 6;
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

                // 6. Add Authentication with explicit scheme names
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
                    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
                    options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
                })
                .AddCookie(IdentityConstants.ApplicationScheme, options =>
                {
                    options.Cookie.Name = ".AspNet.Application.Test";
                    options.LoginPath = "/Account/SignIn";
                    options.LogoutPath = "/Account/SignOut";
                });

                // 7. Configure cookie options
                services.ConfigureApplicationCookie(options =>
                {
                    options.Cookie.Name = ".AspNet.Application.Test";
                    options.LoginPath = "/Account/SignIn";
                    options.LogoutPath = "/Account/SignOut";
                });

                // 8. Add authorization
                services.AddAuthorization();
            });

            builder.UseEnvironment("Development");
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = builder.Build();
            host.Start();

            // Seed database
            using var scope = host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            context.Database.EnsureCreated();

            // Seed roles
            if (!roleManager.RoleExistsAsync("Admin").GetAwaiter().GetResult())
                roleManager.CreateAsync(new IdentityRole("Admin")).GetAwaiter().GetResult();
            if (!roleManager.RoleExistsAsync("Member").GetAwaiter().GetResult())
                roleManager.CreateAsync(new IdentityRole("Member")).GetAwaiter().GetResult();

            return host;
        }
    }
}