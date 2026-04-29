using FluentAssertions;
using GymPortal.Application.DTOs;
using GymPortal.Application.Interfaces.Services;
using GymPortal.Domain.Common;
using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using GymPortal.Domain.Exceptions;
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
    public class ClassesControllerTests
    {
        private readonly Mock<IClassService> _classServiceMock;
        private readonly Mock<IBookingService> _bookingServiceMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly ClassesController _controller;
        private readonly ITempDataDictionary _tempData;
        private readonly DefaultHttpContext _httpContext;

        public ClassesControllerTests()
        {
            _classServiceMock = new Mock<IClassService>();
            _bookingServiceMock = new Mock<IBookingService>();
            _userManagerMock = CreateUserManagerMock();

            _controller = new ClassesController(
                _classServiceMock.Object,
                _bookingServiceMock.Object,
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
                    principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "user123");

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
        }

        [Fact]
        public async Task Index_Get_ReturnsViewWithClasses()
        {
            // Arrange
            var sessions = new List<ClassSession>
            {
                new ClassSession
                {
                    Id = 1,
                    IsActive = true,
                    StartTime = DateTime.UtcNow.AddDays(1),
                    TrainingProgram = new TrainingProgram { Id = 1, Title = "Yoga", Category = "Wellness" },
                    Bookings = new List<Booking>()
                }
            };
            var programs = new List<TrainingProgram>
            {
                new TrainingProgram { Id = 1, Title = "Yoga", Category = "Wellness" }
            };

            _classServiceMock.Setup(x => x.GetAvailableSessionsAsync(null)).ReturnsAsync(sessions);
            _classServiceMock.Setup(x => x.GetAllProgramsAsync()).ReturnsAsync(programs);
            _bookingServiceMock.Setup(x => x.GetUserBookingsAsync(It.IsAny<string>())).ReturnsAsync(new List<Booking>());

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ClassesIndexViewModel>(viewResult.Model);
            Assert.Single(model.Sessions);
        }

        [Fact]
        public async Task Index_Get_WithAuthenticatedUser_IncludesBookedSessionIds()
        {
            // Arrange
            var userId = "user123";
            var sessions = new List<ClassSession>
            {
                new ClassSession
                {
                    Id = 1,
                    IsActive = true,
                    StartTime = DateTime.UtcNow.AddDays(1),
                    TrainingProgram = new TrainingProgram { Id = 1, Title = "Yoga", Category = "Wellness" },
                    Bookings = new List<Booking>()
                }
            };
            var programs = new List<TrainingProgram>
            {
                new TrainingProgram { Id = 1, Title = "Yoga", Category = "Wellness" }
            };
            var userBookings = new List<Booking> { new Booking { ClassSessionId = 1 } };

            SetupUserIdentity(userId);
            _classServiceMock.Setup(x => x.GetAvailableSessionsAsync(null)).ReturnsAsync(sessions);
            _classServiceMock.Setup(x => x.GetAllProgramsAsync()).ReturnsAsync(programs);
            _bookingServiceMock.Setup(x => x.GetUserBookingsAsync(userId)).ReturnsAsync(userBookings);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ClassesIndexViewModel>(viewResult.Model);
            Assert.Contains(1, model.UserBookedSessionIds);
        }

        [Fact]
        public async Task Index_WithCategory_ReturnsFilteredClasses()
        {
            // Arrange
            var category = "Yoga";
            var sessions = new List<ClassSession>();
            var programs = new List<TrainingProgram>();

            _classServiceMock.Setup(x => x.GetAvailableSessionsAsync(category)).ReturnsAsync(sessions);
            _classServiceMock.Setup(x => x.GetAllProgramsAsync()).ReturnsAsync(programs);

            // Act
            var result = await _controller.Index(category);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ClassesIndexViewModel>(viewResult.Model);
            Assert.Equal(category, model.SelectedCategory);
        }

        [Fact]
        public async Task Index_WhenNoSessions_ReturnsEmptyList()
        {
            // Arrange
            var sessions = new List<ClassSession>();
            var programs = new List<TrainingProgram>();

            _classServiceMock.Setup(x => x.GetAvailableSessionsAsync(null)).ReturnsAsync(sessions);
            _classServiceMock.Setup(x => x.GetAllProgramsAsync()).ReturnsAsync(programs);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ClassesIndexViewModel>(viewResult.Model);
            Assert.Empty(model.Sessions);
        }

        [Fact]
        public async Task Book_Post_ValidBooking_RedirectsWithSuccess()
        {
            // Arrange
            var userId = "user123";
            var sessionId = 1;

            SetupUserIdentity(userId);
            _bookingServiceMock.Setup(x => x.BookClassAsync(userId, sessionId))
                .ReturnsAsync(Result<Booking>.Success(new Booking()));

            // Act
            var result = await _controller.Book(sessionId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(ClassesController.Index), redirectResult.ActionName);
            Assert.Equal("Class booked successfully!", _controller.TempData["Success"]);
        }

        [Fact]
        public async Task Book_Post_DuplicateBooking_RedirectsWithError()
        {
            // Arrange
            var userId = "user123";
            var sessionId = 1;

            SetupUserIdentity(userId);
            _bookingServiceMock.Setup(x => x.BookClassAsync(userId, sessionId))
                .ThrowsAsync(new DuplicateBookingException());

            // Act
            var result = await _controller.Book(sessionId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(ClassesController.Index), redirectResult.ActionName);
            Assert.Equal("You have already booked this class.", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Book_Post_ClassFull_RedirectsWithError()
        {
            // Arrange
            var userId = "user123";
            var sessionId = 1;

            SetupUserIdentity(userId);
            _bookingServiceMock.Setup(x => x.BookClassAsync(userId, sessionId))
                .ThrowsAsync(new ClassFullException());

            // Act
            var result = await _controller.Book(sessionId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(ClassesController.Index), redirectResult.ActionName);
            Assert.Equal("This class is already full.", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Book_Post_GeneralException_RedirectsWithGenericError()
        {
            // Arrange
            var userId = "user123";
            var sessionId = 1;

            SetupUserIdentity(userId);
            _bookingServiceMock.Setup(x => x.BookClassAsync(userId, sessionId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Book(sessionId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(ClassesController.Index), redirectResult.ActionName);
            Assert.Equal("An error occurred while booking the class.", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Book_Post_BookingServiceReturnsFailure_RedirectsWithError()
        {
            // Arrange
            var userId = "user123";
            var sessionId = 1;

            SetupUserIdentity(userId);
            _bookingServiceMock.Setup(x => x.BookClassAsync(userId, sessionId))
                .ReturnsAsync(Result<Booking>.Failure("Booking failed"));

            // Act
            var result = await _controller.Book(sessionId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(ClassesController.Index), redirectResult.ActionName);
            Assert.Equal("Booking failed", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Cancel_Post_ValidCancellation_RedirectsWithSuccess()
        {
            // Arrange
            var bookingId = 1;
            var returnUrl = "/Classes";

            _bookingServiceMock.Setup(x => x.CancelBookingAsync(bookingId))
                .ReturnsAsync(Result.Success());

            // Act
            var result = await _controller.Cancel(bookingId, returnUrl);

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal(returnUrl, redirectResult.Url);
            Assert.Equal("Booking cancelled successfully.", _controller.TempData["Success"]);
        }

        [Fact]
        public async Task Cancel_Post_CancellationFails_RedirectsWithError()
        {
            // Arrange
            var bookingId = 1;
            var returnUrl = "/Classes";

            _bookingServiceMock.Setup(x => x.CancelBookingAsync(bookingId))
                .ReturnsAsync(Result.Failure("Cancel failed"));

            // Act
            var result = await _controller.Cancel(bookingId, returnUrl);

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal(returnUrl, redirectResult.Url);
            Assert.Equal("Cancel failed", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Cancel_Post_WithNullReturnUrl_RedirectsToHome()
        {
            // Arrange
            var bookingId = 1;
            var returnUrl = null as string;

            _bookingServiceMock.Setup(x => x.CancelBookingAsync(bookingId))
                .ReturnsAsync(Result.Success());

            // Act
            var result = await _controller.Cancel(bookingId, returnUrl);

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/", redirectResult.Url);
            Assert.Equal("Booking cancelled successfully.", _controller.TempData["Success"]);
        }
    }
}