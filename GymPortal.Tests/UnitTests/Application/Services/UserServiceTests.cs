using FluentAssertions;
using GymPortal.Application.DTOs;
using GymPortal.Application.Interfaces.Repositories;
using GymPortal.Application.Services;
using GymPortal.Domain.Common;
using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace GymPortal.Tests.UnitTests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IBaseRepository<Membership>> _membershipRepoMock;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object,
                null, null, null, null, null, null, null, null);

            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _membershipRepoMock = new Mock<IBaseRepository<Membership>>();

            _unitOfWorkMock.Setup(u => u.Repository<Membership>()).Returns(_membershipRepoMock.Object);
            _userService = new UserService(_userManagerMock.Object, _unitOfWorkMock.Object);
        }

        [Fact]
        public async Task GetUserByIdAsync_WhenUserExists_ShouldReturnUserDto()
        {
            // Arrange
            var userId = "user123";
            var user = new ApplicationUser
            {
                Id = userId,
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe"
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Member" });
            _membershipRepoMock.Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Membership, bool>>>()))
                .ReturnsAsync(new List<Membership>());

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(userId);
            result.Email.Should().Be("test@example.com");
            result.FullName.Should().Be("John Doe");
        }

        [Fact]
        public async Task GetUserByIdAsync_WhenUserNotFound_ShouldReturnNull()
        {
            // Arrange
            var userId = "nonexistent";
            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetUserByEmailAsync_WhenUserExists_ShouldReturnUserDto()
        {
            // Arrange
            var email = "test@example.com";
            var user = new ApplicationUser
            {
                Id = "user123",
                Email = email,
                FirstName = "John",
                LastName = "Doe"
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Member" });
            _membershipRepoMock.Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Membership, bool>>>()))
                .ReturnsAsync(new List<Membership>());

            // Act
            var result = await _userService.GetUserByEmailAsync(email);

            // Assert
            result.Should().NotBeNull();
            result.Email.Should().Be(email);
        }

        [Fact]
        public async Task UpdateUserProfileAsync_ValidUser_ShouldSucceed()
        {
            // Arrange
            var userId = "user123";
            var user = new ApplicationUser { Id = userId };
            var firstName = "Updated";
            var lastName = "Name";
            var phoneNumber = "1234567890";
            var dateOfBirth = new DateTime(1990, 1, 1);
            var profileImageUrl = "/images/profile.jpg";

            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.UpdateUserProfileAsync(userId, firstName, lastName, phoneNumber, dateOfBirth, profileImageUrl);

            // Assert
            result.IsSuccess.Should().BeTrue();
            user.FirstName.Should().Be(firstName);
            user.LastName.Should().Be(lastName);
            user.PhoneNumber.Should().Be(phoneNumber);
            user.DateOfBirth.Should().Be(dateOfBirth);
            user.ProfileImageUrl.Should().Be(profileImageUrl);
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
        public async Task UpdateUserProfileAsync_UpdateFails_ShouldReturnFailure()
        {
            // Arrange
            var userId = "user123";
            var user = new ApplicationUser { Id = userId };
            var errors = new[] { new IdentityError { Description = "Update failed" } };

            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Failed(errors));

            // Act
            var result = await _userService.UpdateUserProfileAsync(userId, "John", "Doe", null, null, null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Update failed");
        }

        [Fact]
        public async Task DeleteUserAsync_ValidUser_ShouldSucceed()
        {
            // Arrange
            var userId = "user123";
            var user = new ApplicationUser { Id = userId };

            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.DeleteUserAsync(userId);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteUserAsync_UserNotFound_ShouldReturnFailure()
        {
            // Arrange
            var userId = "nonexistent";
            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _userService.DeleteUserAsync(userId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("User not found.");
        }
    }
}