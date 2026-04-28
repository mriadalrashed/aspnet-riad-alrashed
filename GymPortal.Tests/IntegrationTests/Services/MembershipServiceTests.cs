using FluentAssertions;
using GymPortal.Application.Services;
using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using GymPortal.Infrastructure.Data;
using GymPortal.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GymPortal.Tests.IntegrationTests.Services
{
    public class MembershipServiceIntegrationTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly UnitOfWork _unitOfWork;
        private readonly MembershipService _membershipService;
        private readonly string _dbName;

        public MembershipServiceIntegrationTests()
        {
            _dbName = $"TestDb_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: _dbName)
                .Options;
            _context = new AppDbContext(options);
            _unitOfWork = new UnitOfWork(_context);
            _membershipService = new MembershipService(_unitOfWork);
        }

        [Fact]
        public async Task CreateMembershipAsync_NewMembership_ShouldCreateAndPersist()
        {
            // Arrange
            var userId = "user-123";

            // Act
            var result = await _membershipService.CreateMembershipAsync(userId, MembershipType.Monthly, "Premium");

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.UserId.Should().Be(userId);
            result.Value.PlanName.Should().Be("Premium");
            result.Value.Status.Should().Be(MembershipStatus.Active);

            // Verify persistence
            var savedMembership = await _context.Memberships.FirstOrDefaultAsync(m => m.UserId == userId);
            savedMembership.Should().NotBeNull();
            savedMembership.PlanName.Should().Be("Premium");
        }

        [Fact]
        public async Task CreateMembershipAsync_ExistingActiveMembership_ShouldReturnFailure()
        {
            // Arrange
            var userId = "user-123";
            var existingMembership = new Membership
            {
                UserId = userId,
                PlanName = "Basic",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                Status = MembershipStatus.Active,
                Type = MembershipType.Monthly,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Memberships.AddAsync(existingMembership);
            await _context.SaveChangesAsync();

            // Act
            var result = await _membershipService.CreateMembershipAsync(userId, MembershipType.Monthly, "Premium");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("User already has an active membership.");
        }

        [Fact]
        public async Task GetUserMembershipAsync_ExistingMembership_ShouldReturnMembership()
        {
            // Arrange
            var userId = "user-123";
            var membership = new Membership
            {
                UserId = userId,
                PlanName = "Premium",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                Status = MembershipStatus.Active,
                Type = MembershipType.Monthly,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Memberships.AddAsync(membership);
            await _context.SaveChangesAsync();

            // Act
            var result = await _membershipService.GetUserMembershipAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(userId);
            result.PlanName.Should().Be("Premium");
        }

        [Fact]
        public async Task CancelMembershipAsync_ExistingMembership_ShouldUpdateStatus()
        {
            // Arrange
            var userId = "user-123";
            var membership = new Membership
            {
                UserId = userId,
                PlanName = "Premium",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                Status = MembershipStatus.Active,
                Type = MembershipType.Monthly,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Memberships.AddAsync(membership);
            await _context.SaveChangesAsync();

            // Act
            var result = await _membershipService.CancelMembershipAsync(userId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var cancelledMembership = await _context.Memberships.FirstOrDefaultAsync(m => m.UserId == userId);
            cancelledMembership.Status.Should().Be(MembershipStatus.Cancelled);
        }

        [Fact]
        public async Task GetUserMembershipAsync_NoMembership_ShouldReturnNull()
        {
            // Act
            var result = await _membershipService.GetUserMembershipAsync("nonexistent");

            // Assert
            result.Should().BeNull();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}