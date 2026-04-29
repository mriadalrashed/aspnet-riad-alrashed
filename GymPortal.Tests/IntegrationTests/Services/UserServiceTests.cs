using FluentAssertions;
using GymPortal.Application.Services;
using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using GymPortal.Infrastructure.Data;
using GymPortal.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace GymPortal.Tests.IntegrationTests.Services
{
    public class UserServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly UnitOfWork _unitOfWork;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly UserService _userService;
        private readonly string _dbName;

        public UserServiceTests()
        {
            _dbName = $"TestDb_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: _dbName)
                .Options;
            _context = new AppDbContext(options);
            _unitOfWork = new UnitOfWork(_context);

            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object,
                null, null, null, null, null, null, null, null);

            _userService = new UserService(_userManagerMock.Object, _unitOfWork);
        }

        [Fact]
        public async Task GetUserByIdAsync_UserExists_ShouldReturnUserWithMembership()
        {
            // Arrange
            var userId = "user-123";
            var user = new ApplicationUser
            {
                Id = userId,
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                PhoneNumber = "1234567890"
            };

            var membership = new Membership
            {
                UserId = userId,
                PlanName = "Premium",
                Status = MembershipStatus.Active,
                EndDate = DateTime.UtcNow.AddMonths(1),
                StartDate = DateTime.UtcNow,
                Type = MembershipType.Monthly,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Memberships.AddAsync(membership);
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Member" });

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(userId);
            result.Email.Should().Be("test@example.com");
            result.FullName.Should().Be("John Doe");
            result.MembershipPlanName.Should().Be("Premium");
        }

        [Fact]
        public async Task GetUserByIdAsync_UserNotFound_ShouldReturnNull()
        {
            // Act
            var result = await _userService.GetUserByIdAsync("nonexistent");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetUserByEmailAsync_UserExists_ShouldReturnUser()
        {
            // Arrange
            var email = "test@example.com";
            var user = new ApplicationUser
            {
                Id = "user-123",
                Email = email,
                FirstName = "John",
                LastName = "Doe"
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Member" });

            // Act
            var result = await _userService.GetUserByEmailAsync(email);

            // Assert
            result.Should().NotBeNull();
            result.Email.Should().Be(email);
        }

        [Fact]
        public async Task UpdateUserProfileAsync_ValidUser_ShouldUpdateAllFields()
        {
            // Arrange
            var userId = "user-123";
            var user = new ApplicationUser
            {
                Id = userId,
                FirstName = "Old",
                LastName = "Name",
                PhoneNumber = "1111111111"
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.UpdateUserProfileAsync(
                userId,
                "Updated",
                "LastName",
                "9876543210",
                new DateTime(1990, 1, 1),
                "/images/profile.jpg");

            // Assert
            result.IsSuccess.Should().BeTrue();
            user.FirstName.Should().Be("Updated");
            user.LastName.Should().Be("LastName");
            user.PhoneNumber.Should().Be("9876543210");
            user.DateOfBirth.Should().Be(new DateTime(1990, 1, 1));
            user.ProfileImageUrl.Should().Be("/images/profile.jpg");
        }

        [Fact]
        public async Task UpdateUserProfileAsync_UserNotFound_ShouldReturnFailure()
        {
            // Arrange
            var userId = "nonexistent";
            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _userService.UpdateUserProfileAsync(userId, "John", "Doe", null, null, null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("User not found.");
        }

        [Fact]
        public async Task DeleteUserAsync_ValidUser_ShouldDeleteUser()
        {
            // Arrange
            var userId = "user-123";
            var user = new ApplicationUser { Id = userId };
            var membership = new Membership
            {
                UserId = userId,
                PlanName = "Premium",
                Status = MembershipStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            await _context.Memberships.AddAsync(membership);
            await _context.SaveChangesAsync();

            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.DeleteUserAsync(userId);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}