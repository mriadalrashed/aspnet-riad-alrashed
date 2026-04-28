using GymPortal.Application.Interfaces.Repositories;
using GymPortal.Application.Interfaces.Services;
using GymPortal.Infrastructure.Data;
using GymPortal.Infrastructure.Repositories;
using GymPortal.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymPortal.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
           var useInMemory = configuration.GetValue<bool>("UseInMemoryDatabase");
            if (useInMemory)
            {
                services.AddDbContext<GymPortalDbContext>(options =>
                    options.UseInMemoryDatabase("GymPortalDb"));
            }
            else
            {
                services.AddDbContext<GymPortalDbContext>(options =>
                    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            }
            
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IBaseRepostory<>), typeof(BaseRepostory<>));
            services.AddScoped<IMembershipService, MembershipService>();
            services.AddScoped<IClassService, ClassService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAdminService, AdminService>();

            return services;
        }
    }
}
