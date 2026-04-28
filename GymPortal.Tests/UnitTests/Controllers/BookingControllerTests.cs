using FluentAssertions;
using GymPortal.Application.Interfaces.Services;
using GymPortal.Domain.Common;
using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using GymPortal.Domain.Exceptions;
using GymPortal.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using System.Security.Claims;
using Xunit;

namespace GymPortal.Tests.UnitTests.Controllers
{
    public class BookingControllerTests
    {
        private readonly Mock<IBookingService> _bookingServiceMock;
        private readonly Mock<IClassService> _classServiceMock;
        private readonly Mock<UserManager<IdentityUser>> _userManagerMock;
        private readonly BookingController _controller;
        private readonly ITempDataDictionary _tempData;
        private readonly DefaultHttpContext _httpContext;

        public BookingControllerTests()
        {
            _bookingServiceMock = new Mock<IBookingService>();
            _classServiceMock = new Mock<IClassService>();
            _userManagerMock = CreateUserManagerMock();

            _controller = new BookingController(
                _bookingServiceMock.Object,
                _classServiceMock.Object,
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

        private Mock<UserManager<IdentityUser>> CreateUserManagerMock()
        {
            var store = new Mock<IUserStore<IdentityUser>>();
            var mock = new Mock<UserManager<IdentityUser>>(
                store.Object, null, null, null, null, null, null, null, null);

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
        public async Task MyBookings_Get_ReturnsViewWithBookings()
        {
            // Arrange
            var userId = "user123";
            var bookings = new List<Booking>
            {
                new Booking
                {
                    Id = 1,
                    ClassSession = new ClassSession
                    {
                        StartTime = DateTime.UtcNow.AddDays(1),
                        EndTime = DateTime.UtcNow.AddDays(1).AddHours(1)
                    }
                }
            };

            SetupUserIdentity(userId);
            _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
            _bookingServiceMock.Setup(x => x.GetUserBookingsWithClassAsync(userId)).ReturnsAsync(bookings);

            // Act
            var result = await _controller.MyBookings();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<List<Booking>>(viewResult.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task BookClassAsync_Post_ValidBooking_RedirectsWithSuccess()
        {
            // Arrange
            var userId = "user123";
            var classSessionId = 1;

            SetupUserIdentity(userId);
            _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
            _bookingServiceMock.Setup(x => x.BookClassAsync(userId, classSessionId))
                .ReturnsAsync(Result<Booking>.Success(new Booking()));

            // Act
            var result = await _controller.BookClass(classSessionId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyBookings", redirectResult.ActionName);
            Assert.Equal("Class booked successfully!", _controller.TempData["SuccessMessage"]);
        }

        [Fact]
        public async Task BookClassAsync_Post_DuplicateBooking_RedirectsWithError()
        {
            // Arrange
            var userId = "user123";
            var classSessionId = 1;

            SetupUserIdentity(userId);
            _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
            _bookingServiceMock.Setup(x => x.BookClassAsync(userId, classSessionId))
                .ThrowsAsync(new DuplicateBookingException());

            // Act
            var result = await _controller.BookClass(classSessionId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Class", redirectResult.ControllerName);
            Assert.Equal("You have already booked this class.", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task BookClassAsync_Post_ClassFull_RedirectsWithError()
        {
            // Arrange
            var userId = "user123";
            var classSessionId = 1;

            SetupUserIdentity(userId);
            _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
            _bookingServiceMock.Setup(x => x.BookClassAsync(userId, classSessionId))
                .ThrowsAsync(new ClassFullException());

            // Act
            var result = await _controller.BookClass(classSessionId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Class", redirectResult.ControllerName);
            Assert.Equal("This class is already full.", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task BookClassAsync_Post_GeneralException_RedirectsWithError()
        {
            // Arrange
            var userId = "user123";
            var classSessionId = 1;

            SetupUserIdentity(userId);
            _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
            _bookingServiceMock.Setup(x => x.BookClassAsync(userId, classSessionId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.BookClass(classSessionId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Class", redirectResult.ControllerName);
            Assert.Equal("An error occurred while booking the class.", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task CancelBooking_Post_ValidRequest_RedirectsWithSuccess()
        {
            // Arrange
            var userId = "user123";
            var bookingId = 1;
            var bookings = new List<Booking> { new Booking { Id = bookingId } };

            SetupUserIdentity(userId);
            _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
            _bookingServiceMock.Setup(x => x.GetUserBookingsAsync(userId)).ReturnsAsync(bookings);
            _bookingServiceMock.Setup(x => x.CancelBookingAsync(bookingId)).ReturnsAsync(Result.Success());

            // Act
            var result = await _controller.CancelBooking(bookingId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyBookings", redirectResult.ActionName);
            Assert.Equal("Booking cancelled successfully.", _controller.TempData["SuccessMessage"]);
        }

        [Fact]
        public async Task CancelBooking_Post_BookingNotFound_ReturnsForbid()
        {
            // Arrange
            var userId = "user123";
            var bookingId = 999;

            SetupUserIdentity(userId);
            _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
            _bookingServiceMock.Setup(x => x.GetUserBookingsAsync(userId)).ReturnsAsync(new List<Booking>());

            // Act
            var result = await _controller.CancelBooking(bookingId);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task CancelBooking_Post_CancelFails_RedirectsWithError()
        {
            // Arrange
            var userId = "user123";
            var bookingId = 1;
            var bookings = new List<Booking> { new Booking { Id = bookingId } };

            SetupUserIdentity(userId);
            _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
            _bookingServiceMock.Setup(x => x.GetUserBookingsAsync(userId)).ReturnsAsync(bookings);
            _bookingServiceMock.Setup(x => x.CancelBookingAsync(bookingId))
                .ReturnsAsync(Result.Failure("Cancel failed"));

            // Act
            var result = await _controller.CancelBooking(bookingId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyBookings", redirectResult.ActionName);
            Assert.Equal("Cancel failed", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task CheckAvailability_Get_ExistingSession_ReturnsJson()
        {
            // Arrange
            var classSessionId = 1;
            var classSession = new ClassSession { Id = classSessionId, MaxParticipants = 20 };
            var bookings = new List<Booking>
            {
                new Booking { Status = BookingStatus.Confirmed },
                new Booking { Status = BookingStatus.Confirmed }
            };

            _classServiceMock.Setup(x => x.GetSessionByIdAsync(classSessionId)).ReturnsAsync(classSession);
            _bookingServiceMock.Setup(x => x.GetBookingsBySessionIdAsync(classSessionId)).ReturnsAsync(bookings);

            // Act
            var result = await _controller.CheckAvailability(classSessionId);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic data = jsonResult.Value;

            // Use proper property access
            var available = data.GetType().GetProperty("available")?.GetValue(data, null);
            var bookedCount = data.GetType().GetProperty("bookedCount")?.GetValue(data, null);
            var maxParticipants = data.GetType().GetProperty("maxParticipants")?.GetValue(data, null);
            var availableSpots = data.GetType().GetProperty("availableSpots")?.GetValue(data, null);

            Assert.True((bool)available);
            Assert.Equal(2, (int)bookedCount);
            Assert.Equal(20, (int)maxParticipants);
            Assert.Equal(18, (int)availableSpots);
        }

        [Fact]
        public async Task CheckAvailability_Get_NonExistingSession_ReturnsNotFoundMessage()
        {
            // Arrange
            var classSessionId = 999;
            _classServiceMock.Setup(x => x.GetSessionByIdAsync(classSessionId)).ReturnsAsync((ClassSession)null);

            // Act
            var result = await _controller.CheckAvailability(classSessionId);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            dynamic data = jsonResult.Value;
            var available = data.GetType().GetProperty("available")?.GetValue(data, null);
            var message = data.GetType().GetProperty("message")?.GetValue(data, null);

            Assert.False((bool)available);
            Assert.Equal("Class not found.", message);
        }

        [Fact]
        public async Task Upcoming_Get_ReturnsFilteredBookings()
        {
            // Arrange
            var userId = "user123";
            var bookings = new List<Booking>
            {
                new Booking
                {
                    Id = 1,
                    ClassSession = new ClassSession { StartTime = DateTime.UtcNow.AddDays(1) }
                },
                new Booking
                {
                    Id = 2,
                    ClassSession = new ClassSession { StartTime = DateTime.UtcNow.AddDays(-1) }
                }
            };

            SetupUserIdentity(userId);
            _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
            _bookingServiceMock.Setup(x => x.GetUserBookingsWithClassAsync(userId)).ReturnsAsync(bookings);

            // Act
            var result = await _controller.Upcoming();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = viewResult.Model;

            // The model is an IEnumerable<Booking>, not necessarily a List
            var modelAsEnumerable = model as IEnumerable<Booking>;
            Assert.NotNull(modelAsEnumerable);
            Assert.Single(modelAsEnumerable);
        }

        [Fact]
        public async Task History_Get_ReturnsPastBookings()
        {
            // Arrange
            var userId = "user123";
            var bookings = new List<Booking>
            {
                new Booking
                {
                    Id = 1,
                    ClassSession = new ClassSession { StartTime = DateTime.UtcNow.AddDays(1) }
                },
                new Booking
                {
                    Id = 2,
                    ClassSession = new ClassSession { StartTime = DateTime.UtcNow.AddDays(-1) }
                }
            };

            SetupUserIdentity(userId);
            _userManagerMock.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
            _bookingServiceMock.Setup(x => x.GetUserBookingsWithClassAsync(userId)).ReturnsAsync(bookings);

            // Act
            var result = await _controller.History();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = viewResult.Model;

            // The model is an IEnumerable<Booking>, not necessarily a List
            var modelAsEnumerable = model as IEnumerable<Booking>;
            Assert.NotNull(modelAsEnumerable);
            Assert.Single(modelAsEnumerable);
        }
    }
}