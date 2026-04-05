using GymPortal.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace GymPortal.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<Membership> Memberships { get; set; }
        public DbSet<ClassSession> ClassSessions { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<TrainingProgram> TrainingPrograms { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Membership>()
                    .HasIndex(m => m.UserId)
                    .IsUnique()
                    .HasFilter("Status = 0");

            builder.Entity<Membership>()
                   .HasOne(m => m.UserId)
                   .WithOne(m => m.Membership)
                   .HasForeignKey(m => m.UserId)
                   .OnDelete(DeleteBehavior.Cascade);    

            builder.Entity<Booking>()
                    .HasIndex(b => new { b.UserId, b.ClassSessionId })
                    .IsUnique()
                    .HasFilter("Status = 0");

            builder.Entity<Booking>()
                    .HasOne(b => b.User)
                    .WithMany(u => u.Bookings)
                    .HasForeignKey(b => b.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Booking>()
                    .HasOne(b => b.ClassSession)
                    .WithMany(c => c.Booking)
                    .HasForeignKey(b => b.ClassSessionId)
                    .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ClassSession>()
                    .HasOne(c => c.TrainingProgram)
                    .WithMany(t => t.ClassSessions)
                    .HasForeignKey(c => c.TrainingProgramId)
                    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
