using FluentAssertions;
using GymPortal.Application.DTOs;
using GymPortal.Application.Interfaces.Services;
using GymPortal.Domain.Common;
using GymPortal.Domain.Entities;
using GymPortal.Web.Controllers;
using GymPortal.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using Xunit;

namespace GymPortal.Tests.UnitTests.Controllers
{
    public class AccountControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IMembershipService> _membershipServiceMock;
        private readonly Mock<IBookingService> _bookingServiceMock;
        private readonly AccountController _controller;
        private readonly Mock<IUrlHelper> _urlHelperMock;
        private readonly DefaultHttpContext _httpContext;

        public AccountControllerTests()
        {
            _userManagerMock = CreateUserManagerMock();
            _signInManagerMock = CreateSignInManagerMock();
            _userServiceMock = new Mock<IUserService>();
            _membershipServiceMock = new Mock<IMembershipService>();
            _bookingServiceMock = new Mock<IBookingService>();

            _controller = new AccountController(
                _userManagerMock.Object,
                _signInManagerMock.Object,
                _userServiceMock.Object,
                _membershipServiceMock.Object,
                _bookingServiceMock.Object);

            // Setup HttpContext and TempData
            _httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(_httpContext, Mock.Of<ITempDataProvider>());
            _controller.TempData = tempData;
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };

            // Setup UrlHelper - always return true for IsLocalUrl
            _urlHelperMock = new Mock<IUrlHelper>();
            _urlHelperMock.Setup(x => x.IsLocalUrl(It.IsAny<string>())).Returns(true);
            _urlHelperMock.Setup(x => x.Action(It.IsAny<UrlActionContext>()))
                .Returns("/test-url");
            _controller.Url = _urlHelperMock.Object;
        }

        private Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var mock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            mock.Object.UserValidators.Add(new UserValidator<ApplicationUser>());
            mock.Object.PasswordValidators.Add(new PasswordValidator<ApplicationUser>());

            mock.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => new ApplicationUser
                {
                    Id = id,
                    UserName = "test@example.com",
                    Email = "test@example.com",
                    FirstName = "John",
                    LastName = "Doe"
                });

            mock.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((string email) => new ApplicationUser
                {
                    Email = email,
                    Id = "user123",
                    UserName = email,
                    FirstName = "John",
                    LastName = "Doe"
                });

            mock.Setup(x => x.DeleteAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);
            mock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);
            mock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            mock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            mock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "Member" });

            mock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns((ClaimsPrincipal principal) =>
                    principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            return mock;
        }

        private Mock<SignInManager<ApplicationUser>> CreateSignInManagerMock()
        {
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            var options = new Mock<Microsoft.Extensions.Options.IOptions<IdentityOptions>>();
            var logger = new Mock<Microsoft.Extensions.Logging.ILogger<SignInManager<ApplicationUser>>>();
            var schemes = new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
            var confirmation = new Mock<IUserConfirmation<ApplicationUser>>();

            claimsFactory.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync((ApplicationUser user) => new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.UserName ?? user.Email),
                    new Claim(ClaimTypes.Email, user.Email ?? "")
                })));

            var mock = new Mock<SignInManager<ApplicationUser>>(
                _userManagerMock.Object,
                contextAccessor.Object,
                claimsFactory.Object,
                options.Object,
                logger.Object,
                schemes.Object,
                confirmation.Object);

            mock.Setup(x => x.SignOutAsync()).Returns(Task.CompletedTask);
            mock.Setup(x => x.SignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            mock.Setup(x => x.GetExternalLoginInfoAsync())
                .ReturnsAsync((ExternalLoginInfo)null);
            mock.Setup(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            return mock;
        }

        private void SetupUserIdentity(string userId)
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
            _controller.Url = _urlHelperMock.Object;
        }

        #region SignUp Tests

        [Fact]
        public void SignUp_Get_ReturnsViewResultWithSignUpViewModel()
        {
            // Act
            var result = _controller.SignUp();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<SignUpViewModel>(viewResult.Model);
        }

        [Fact]
        public void SignUpStep1_ValidEmail_ReturnsViewWithStep2AndTempData()
        {
            // Arrange
            var email = "test@example.com";

            // Act
            var result = _controller.SignUpStep1(email);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True((bool)_controller.ViewData["Step2"]);
            Assert.Equal(email, _controller.TempData["SignUpEmail"]);
            Assert.Equal(email, ((SignUpViewModel)viewResult.Model).Email);
        }

        [Fact]
        public void SignUpStep1_InvalidEmail_ReturnsViewWithModelError()
        {
            // Act
            var result = _controller.SignUpStep1("");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState, m => m.Key == "Email");
        }

        [Fact]
        public async Task SignUp_Post_ValidModel_ReturnsRedirectToHome()
        {
            // Arrange
            var model = new SignUpViewModel
            {
                Email = "test@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                FirstName = "John",
                LastName = "Doe",
                PhoneNumber = "1234567890",
                AgreeToTerms = true
            };

            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), model.Password))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Member"))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.SignUp(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        [Fact]
        public async Task SignUp_Post_InvalidModelState_ReturnsViewWithModel()
        {
            // Arrange
            var model = new SignUpViewModel();
            _controller.ModelState.AddModelError("Email", "Required");

            // Act
            var result = await _controller.SignUp(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
        }

        [Fact]
        public async Task SignUp_Post_CreateUserFails_ReturnsViewWithErrors()
        {
            // Arrange
            var model = new SignUpViewModel
            {
                Email = "test@example.com",
                Password = "Password123!",
                FirstName = "John",
                LastName = "Doe"
            };

            var identityErrors = new[] { new IdentityError { Description = "Email already exists" } };
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), model.Password))
                .ReturnsAsync(IdentityResult.Failed(identityErrors));

            // Act
            var result = await _controller.SignUp(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
        }

        #endregion

        #region SignIn Tests

        [Fact]
        public void SignIn_Get_ReturnsViewWithReturnUrl()
        {
            // Arrange
            var returnUrl = "/dashboard";

            // Act
            var result = _controller.SignIn(returnUrl);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<SignInViewModel>(viewResult.Model);
            Assert.Equal(returnUrl, model.ReturnUrl);
        }

        [Fact]
        public async Task SignIn_Post_ValidCredentials_RedirectsToReturnUrl()
        {
            // Arrange
            var returnUrl = "/dashboard";
            var model = new SignInViewModel
            {
                Email = "test@example.com",
                Password = "Password123!",
                RememberMe = true
            };

            _signInManagerMock.Setup(x => x.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            _urlHelperMock.Setup(x => x.IsLocalUrl(returnUrl)).Returns(true);

            // Act
            var result = await _controller.SignIn(model, returnUrl);

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal(returnUrl, redirectResult.Url);
        }

        [Fact]
        public async Task SignIn_Post_ValidCredentialsWithoutReturnUrl_RedirectsToHome()
        {
            // Arrange
            var model = new SignInViewModel
            {
                Email = "test@example.com",
                Password = "Password123!",
                RememberMe = false
            };

            _signInManagerMock.Setup(x => x.PasswordSignInAsync(model.Email, model.Password, false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            // Act
            var result = await _controller.SignIn(model, null);

            // Assert - When returnUrl is null, RedirectToLocal returns RedirectToActionResult
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        [Fact]
        public async Task SignIn_Post_InvalidCredentials_ReturnsViewWithError()
        {
            // Arrange
            var model = new SignInViewModel
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            };

            _signInManagerMock.Setup(x => x.PasswordSignInAsync(model.Email, model.Password, false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var result = await _controller.SignIn(model, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
        }

        [Fact]
        public async Task SignIn_Post_LockedOutUser_ReturnsLockoutView()
        {
            // Arrange
            var model = new SignInViewModel
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            _signInManagerMock.Setup(x => x.PasswordSignInAsync(model.Email, model.Password, false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

            // Act
            var result = await _controller.SignIn(model, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Lockout", viewResult.ViewName);
        }

        #endregion

        #region SignOut Tests

        [Fact]
        public async Task SignOut_Post_RedirectsToHome()
        {
            // Act
            var result = await _controller.SignOut();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
            _signInManagerMock.Verify(x => x.SignOutAsync(), Times.Once);
        }

        #endregion

        #region MyAccount Tests

        [Fact]
        public async Task MyAccount_Get_AuthenticatedUser_ReturnsViewWithUserData()
        {
            // Arrange
            var userId = "user123";
            var userDto = new UserDto
            {
                Id = userId,
                FirstName = "John",
                LastName = "Doe",
                PhoneNumber = "1234567890",
                DateOfBirth = new DateTime(1990, 1, 1)
            };

            SetupUserIdentity(userId);
            _userServiceMock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync(userDto);

            // Act
            var result = await _controller.MyAccount();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<MyAccountViewModel>(viewResult.Model);
            Assert.Equal(userId, model.User.Id);
        }

        [Fact]
        public async Task MyAccount_Get_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            var userId = "user123";
            SetupUserIdentity(userId);
            _userServiceMock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync((UserDto)null);

            // Act
            var result = await _controller.MyAccount();

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task MyAccount_Post_ValidModelWithoutProfileImage_UpdatesProfileAndRedirects()
        {
            // Arrange
            var userId = "user123";
            var model = new MyAccountViewModel
            {
                FirstName = "John",
                LastName = "Updated",
                PhoneNumber = "9876543210",
                DateOfBirth = new DateTime(1990, 1, 1)
            };

            SetupUserIdentity(userId);
            _userServiceMock.Setup(x => x.UpdateUserProfileAsync(userId, model.FirstName, model.LastName,
                model.PhoneNumber, model.DateOfBirth, null))
                .ReturnsAsync(Result.Success());

            // Act
            var result = await _controller.MyAccount(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AccountController.MyAccount), redirectResult.ActionName);
        }

        [Fact]
        public async Task MyAccount_Post_UpdateFails_ReturnsViewWithError()
        {
            // Arrange
            var userId = "user123";
            var model = new MyAccountViewModel
            {
                FirstName = "John",
                LastName = "Doe"
            };

            SetupUserIdentity(userId);
            _userServiceMock.Setup(x => x.UpdateUserProfileAsync(userId, model.FirstName, model.LastName,
                model.PhoneNumber, model.DateOfBirth, null))
                .ReturnsAsync(Result.Failure("Update failed"));

            // Act
            var result = await _controller.MyAccount(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
        }

        #endregion

        #region MyMembership Tests

        [Fact]
        public async Task MyMembership_Get_AuthenticatedUser_ReturnsViewWithUser()
        {
            // Arrange
            var userId = "user123";
            var userDto = new UserDto { Id = userId, FirstName = "John", LastName = "Doe" };

            SetupUserIdentity(userId);
            _userServiceMock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync(userDto);

            // Act
            var result = await _controller.MyMembership();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<UserDto>(viewResult.Model);
            Assert.Equal(userId, model.Id);
        }

        #endregion

        #region DeleteAccount Tests

        [Fact]
        public async Task DeleteAccount_Post_ValidUser_DeletesAccountAndRedirectsToHome()
        {
            // Arrange
            var userId = "user123";
            SetupUserIdentity(userId);
            _userServiceMock.Setup(x => x.DeleteUserAsync(userId)).ReturnsAsync(Result.Success());
            _signInManagerMock.Setup(x => x.SignOutAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteAccount();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        [Fact]
        public async Task DeleteAccount_Post_DeleteFails_RedirectsToMyAccountWithError()
        {
            // Arrange
            var userId = "user123";
            SetupUserIdentity(userId);
            _userServiceMock.Setup(x => x.DeleteUserAsync(userId)).ReturnsAsync(Result.Failure("Delete failed"));

            // Act
            var result = await _controller.DeleteAccount();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AccountController.MyAccount), redirectResult.ActionName);
        }

        #endregion

        #region MyBookings Tests

        [Fact]
        public async Task MyBookings_Get_AuthenticatedUser_ReturnsViewWithBookings()
        {
            // Arrange
            var userId = "user123";
            var bookings = new List<Booking>();
            var userDto = new UserDto { Id = userId, FirstName = "John", LastName = "Doe" };

            SetupUserIdentity(userId);
            _bookingServiceMock.Setup(x => x.GetUserBookingsWithClassAsync(userId)).ReturnsAsync(bookings);
            _userServiceMock.Setup(x => x.GetUserByIdAsync(userId)).ReturnsAsync(userDto);

            // Act
            var result = await _controller.MyBookings();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<MyBookingsViewModel>(viewResult.Model);
            Assert.Equal(userId, model.User.Id);
        }

        [Fact]
        public async Task MyBookings_Get_UnauthenticatedUser_RedirectsToSignIn()
        {
            // Arrange - Ensure no user identity is set
            _httpContext.User = null;
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };

            // Act
            var result = await _controller.MyBookings();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("SignIn", redirectResult.ActionName);
        }

        #endregion

        #region ForgotPassword Tests

        [Fact]
        public void ForgotPassword_Get_ReturnsView()
        {
            // Act
            var result = _controller.ForgotPassword();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<ForgotPasswordViewModel>(viewResult.Model);
        }

        [Fact]
        public async Task ForgotPassword_Post_ValidEmail_RedirectsToConfirmation()
        {
            // Arrange
            var model = new ForgotPasswordViewModel { Email = "test@example.com" };
            var user = new ApplicationUser
            {
                Id = "user123",
                Email = model.Email,
                UserName = model.Email
            };

            _userManagerMock.Setup(x => x.FindByEmailAsync(model.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
            _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");

            // Act
            var result = await _controller.ForgotPassword(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AccountController.ForgotPasswordConfirmation), redirectResult.ActionName);
        }

        [Fact]
        public async Task ForgotPassword_Post_UserNotFound_RedirectsToConfirmationWithoutRevealing()
        {
            // Arrange
            var model = new ForgotPasswordViewModel { Email = "nonexistent@example.com" };
            _userManagerMock.Setup(x => x.FindByEmailAsync(model.Email)).ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.ForgotPassword(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AccountController.ForgotPasswordConfirmation), redirectResult.ActionName);
        }

        [Fact]
        public void ForgotPasswordConfirmation_Get_ReturnsView()
        {
            // Act
            var result = _controller.ForgotPasswordConfirmation();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        #endregion

        #region ResetPassword Tests

        [Fact]
        public void ResetPassword_Get_ValidParameters_ReturnsView()
        {
            // Arrange
            var userId = "user123";
            var code = "reset-code";

            // Act
            var result = _controller.ResetPassword(userId, code);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ResetPasswordViewModel>(viewResult.Model);
            Assert.Equal(userId, model.UserId);
            Assert.Equal(code, model.Code);
        }

        [Fact]
        public void ResetPassword_Get_NullUserId_ReturnsBadRequest()
        {
            // Act
            var result = _controller.ResetPassword(null, "code");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void ResetPassword_Get_NullCode_ReturnsBadRequest()
        {
            // Act
            var result = _controller.ResetPassword("userId", null);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ResetPassword_Post_ValidModel_ResetsPasswordAndRedirects()
        {
            // Arrange
            var model = new ResetPasswordViewModel
            {
                UserId = "user123",
                Code = "reset-code",
                Password = "NewPassword123!",
                ConfirmPassword = "NewPassword123!"
            };

            var user = new ApplicationUser { Id = model.UserId };
            _userManagerMock.Setup(x => x.FindByIdAsync(model.UserId)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.ResetPasswordAsync(user, model.Code, model.Password))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.ResetPassword(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AccountController.ResetPasswordConfirmation), redirectResult.ActionName);
        }

        [Fact]
        public async Task ResetPassword_Post_UserNotFound_RedirectsToConfirmationWithoutRevealing()
        {
            // Arrange
            var model = new ResetPasswordViewModel
            {
                UserId = "nonexistent",
                Code = "code",
                Password = "NewPassword123!"
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(model.UserId)).ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.ResetPassword(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AccountController.ResetPasswordConfirmation), redirectResult.ActionName);
        }

        [Fact]
        public async Task ResetPassword_Post_ResetFails_ReturnsViewWithErrors()
        {
            // Arrange
            var model = new ResetPasswordViewModel
            {
                UserId = "user123",
                Code = "invalid-code",
                Password = "NewPassword123!"
            };

            var user = new ApplicationUser { Id = model.UserId };
            var identityErrors = new[] { new IdentityError { Description = "Invalid token" } };

            _userManagerMock.Setup(x => x.FindByIdAsync(model.UserId)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.ResetPasswordAsync(user, model.Code, model.Password))
                .ReturnsAsync(IdentityResult.Failed(identityErrors));

            // Act
            var result = await _controller.ResetPassword(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
        }

        [Fact]
        public void ResetPasswordConfirmation_Get_ReturnsView()
        {
            // Act
            var result = _controller.ResetPasswordConfirmation();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        #endregion

        #region ExternalLogin Tests

        [Fact]
        public void ExternalLogin_Post_ReturnsChallengeResult()
        {
            // Arrange
            var provider = "Google";
            var returnUrl = "/";

            // Act
            var result = _controller.ExternalLogin(provider, returnUrl);

            // Assert
            var challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.NotNull(challengeResult);
        }

        [Fact]
        public async Task ExternalLoginCallback_RemoteError_ReturnsSignInViewWithError()
        {
            // Act
            var result = await _controller.ExternalLoginCallback(null, "Error from provider");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(nameof(AccountController.SignIn), viewResult.ViewName);
        }

        [Fact]
        public async Task ExternalLoginCallback_NoExternalInfo_RedirectsToSignIn()
        {
            // Arrange
            _signInManagerMock.Setup(x => x.GetExternalLoginInfoAsync()).ReturnsAsync((ExternalLoginInfo)null);

            // Act
            var result = await _controller.ExternalLoginCallback();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AccountController.SignIn), redirectResult.ActionName);
        }

        [Fact]
        public async Task ExternalLoginCallback_ExistingUser_RedirectsToReturnUrl()
        {
            // Arrange
            var returnUrl = "/dashboard";

            var claimsIdentity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "123"),
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim(ClaimTypes.Name, "Test User")
            }, "Google");

            var principal = new ClaimsPrincipal(claimsIdentity);
            var loginInfo = new ExternalLoginInfo(principal, "Google", "123", "Google");

            _signInManagerMock.Setup(x => x.GetExternalLoginInfoAsync()).ReturnsAsync(loginInfo);
            _signInManagerMock.Setup(x => x.ExternalLoginSignInAsync(It.IsAny<string>(), It.IsAny<string>(), false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            _urlHelperMock.Setup(x => x.IsLocalUrl(returnUrl)).Returns(true);

            // Act
            var result = await _controller.ExternalLoginCallback(returnUrl);

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal(returnUrl, redirectResult.Url);
        }

        #endregion
    }
}