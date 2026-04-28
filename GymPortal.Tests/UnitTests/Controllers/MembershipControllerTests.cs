using FluentAssertions;
using GymPortal.Application.Interfaces.Services;
using GymPortal.Domain.Common;
using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using GymPortal.Web.Controllers;
using GymPortal.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using System.Security.Claims;
using Xunit;

namespace GymPortal.Tests.UnitTests.Controllers
{
    public class MembershipControllerTests
    {
        private readonly Mock<IMembershipService> _membershipServiceMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly MembershipController _controller;
        private readonly ITempDataDictionary _tempData;
        private readonly DefaultHttpContext _httpContext;

        public MembershipControllerTests()
        {
            _membershipServiceMock = new Mock<IMembershipService>();
            _userManagerMock = CreateUserManagerMock();

            _controller = new MembershipController(
                _membershipServiceMock.Object,
                _userManagerMock.Object);

            // Setup HttpContext and TempData
            _httpContext = new DefaultHttpContext();
            _tempData = new TempDataDictionary(_httpContext, Mock.Of<ITempDataProvider>());
            _controller.TempData = _tempData;

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };
        }

        private Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var mock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            mock.Object.UserValidators.Add(new UserValidator<ApplicationUser>());
            mock.Object.PasswordValidators.Add(new PasswordValidator<ApplicationUser>());

            mock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns((ClaimsPrincipal principal) =>
                    principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            return mock;
        }

        private void SetupAuthenticatedUser(string userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, "test@example.com"),
                new Claim(ClaimTypes.Email, "test@example.com")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _httpContext.User = claimsPrincipal;
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };

            // Refresh TempData
            var tempData = new TempDataDictionary(_httpContext, Mock.Of<ITempDataProvider>());
            _controller.TempData = tempData;
        }

        private void SetupUnauthenticatedUser()
        {
            _httpContext.User = null;
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };
        }

        [Fact]
        public async Task Index_Get_AuthenticatedUser_ReturnsViewWithPlans()
        {
            // Arrange
            var userId = "user123";
            SetupAuthenticatedUser(userId);

            var membership = new Membership { Id = 1, PlanName = "Premium", Type = MembershipType.Monthly };
            _membershipServiceMock.Setup(x => x.GetUserMembershipAsync(userId)).ReturnsAsync(membership);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<MembershipViewModel>(viewResult.Model);
            Assert.Equal(3, model.Plans.Count);
            Assert.NotNull(model.CurrentUserPlan);
        }

        [Fact]
        public async Task Index_Get_AuthenticatedUser_NoMembership_ReturnsViewWithNullCurrentPlan()
        {
            // Arrange
            var userId = "user123";
            SetupAuthenticatedUser(userId);

            _membershipServiceMock.Setup(x => x.GetUserMembershipAsync(userId)).ReturnsAsync((Membership)null);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<MembershipViewModel>(viewResult.Model);
            Assert.Equal(3, model.Plans.Count);
            Assert.Null(model.CurrentUserPlan);
        }

        [Fact]
        public async Task Index_Get_UnauthenticatedUser_RedirectsToSignIn()
        {
            // Arrange
            SetupUnauthenticatedUser();

            // Act
            var result = await _controller.Index();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("SignIn", redirectResult.ActionName);
            Assert.Equal("Account", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Subscribe_Post_ValidPlan_RedirectsWithSuccess()
        {
            // Arrange
            var userId = "user123";
            var planId = 2; // Premium

            SetupAuthenticatedUser(userId);
            _membershipServiceMock.Setup(x => x.CreateMembershipAsync(userId, MembershipType.Monthly, "Premium"))
                .ReturnsAsync(Result<Membership>.Success(new Membership()));

            // Act
            var result = await _controller.Subscribe(planId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(MembershipController.Index), redirectResult.ActionName);
            Assert.Equal("Membership activated successfully!", _controller.TempData["Success"]);
        }

        [Fact]
        public async Task Subscribe_Post_PlanId1_Basic_RedirectsWithSuccess()
        {
            // Arrange
            var userId = "user123";
            var planId = 1; // Basic

            SetupAuthenticatedUser(userId);
            _membershipServiceMock.Setup(x => x.CreateMembershipAsync(userId, MembershipType.Monthly, "Basic"))
                .ReturnsAsync(Result<Membership>.Success(new Membership()));

            // Act
            var result = await _controller.Subscribe(planId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(MembershipController.Index), redirectResult.ActionName);
        }

        [Fact]
        public async Task Subscribe_Post_PlanId3_Elite_RedirectsWithSuccess()
        {
            // Arrange
            var userId = "user123";
            var planId = 3; // Elite - Yearly

            SetupAuthenticatedUser(userId);
            _membershipServiceMock.Setup(x => x.CreateMembershipAsync(userId, MembershipType.Yearly, "Elite"))
                .ReturnsAsync(Result<Membership>.Success(new Membership()));

            // Act
            var result = await _controller.Subscribe(planId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(MembershipController.Index), redirectResult.ActionName);
        }

        [Fact]
        public async Task Subscribe_Post_InvalidPlanId_DefaultsToMonthly()
        {
            // Arrange
            var userId = "user123";
            var planId = 99; // Invalid

            SetupAuthenticatedUser(userId);
            _membershipServiceMock.Setup(x => x.CreateMembershipAsync(userId, MembershipType.Monthly, "Custom"))
                .ReturnsAsync(Result<Membership>.Success(new Membership()));

            // Act
            var result = await _controller.Subscribe(planId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(MembershipController.Index), redirectResult.ActionName);
        }

        [Fact]
        public async Task Subscribe_Post_UnauthenticatedUser_RedirectsToSignIn()
        {
            // Arrange
            SetupUnauthenticatedUser();

            // Act
            var result = await _controller.Subscribe(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("SignIn", redirectResult.ActionName);
            Assert.Equal("Account", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Subscribe_Post_FailedCreation_RedirectsWithError()
        {
            // Arrange
            var userId = "user123";
            var planId = 2;

            SetupAuthenticatedUser(userId);
            _membershipServiceMock.Setup(x => x.CreateMembershipAsync(userId, MembershipType.Monthly, "Premium"))
                .ReturnsAsync(Result<Membership>.Failure("Failed to create membership"));

            // Act
            var result = await _controller.Subscribe(planId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(MembershipController.Index), redirectResult.ActionName);
            Assert.Equal("Failed to create membership", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Cancel_Post_ValidUser_RedirectsWithSuccess()
        {
            // Arrange
            var userId = "user123";
            SetupAuthenticatedUser(userId);
            _membershipServiceMock.Setup(x => x.CancelMembershipAsync(userId))
                .ReturnsAsync(Result.Success());

            // Act
            var result = await _controller.Cancel();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(MembershipController.Index), redirectResult.ActionName);
            Assert.Equal("Membership cancelled successfully.", _controller.TempData["Success"]);
        }

        [Fact]
        public async Task Cancel_Post_CancellationFails_RedirectsWithError()
        {
            // Arrange
            var userId = "user123";
            SetupAuthenticatedUser(userId);
            _membershipServiceMock.Setup(x => x.CancelMembershipAsync(userId))
                .ReturnsAsync(Result.Failure("No active membership found"));

            // Act
            var result = await _controller.Cancel();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(MembershipController.Index), redirectResult.ActionName);
            Assert.Equal("No active membership found", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Cancel_Post_UnauthenticatedUser_RedirectsToSignIn()
        {
            // Arrange
            SetupUnauthenticatedUser();

            // Act
            var result = await _controller.Cancel();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("SignIn", redirectResult.ActionName);
            Assert.Equal("Account", redirectResult.ControllerName);
        }
    }
}