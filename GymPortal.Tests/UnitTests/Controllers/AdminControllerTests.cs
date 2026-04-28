using FluentAssertions;
using GymPortal.Application.DTOs;
using GymPortal.Application.Interfaces.Services;
using GymPortal.Domain.Common;
using GymPortal.Web.Controllers;
using GymPortal.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace GymPortal.Tests.UnitTests.Controllers
{
    public class AdminControllerTests
    {
        private readonly Mock<IAdminService> _adminServiceMock;
        private readonly AdminController _controller;
        private readonly ITempDataDictionary _tempData;

        public AdminControllerTests()
        {
            _adminServiceMock = new Mock<IAdminService>();
            _controller = new AdminController(_adminServiceMock.Object);

            // Setup TempData
            var httpContext = new DefaultHttpContext();
            _tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            _controller.TempData = _tempData;
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public void Index_Get_RedirectsToUsers()
        {
            // Act
            var result = _controller.Index();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AdminController.Users), redirectResult.ActionName);
        }

        [Fact]
        public async Task Users_Get_ReturnsViewWithUsers()
        {
            // Arrange
            var users = new List<UserDto>
            {
                new UserDto { Id = "1", Email = "user1@test.com", FirstName = "John", LastName = "Doe" },
                new UserDto { Id = "2", Email = "user2@test.com", FirstName = "Jane", LastName = "Smith" }
            };

            _adminServiceMock.Setup(x => x.GetAllUsersAsync()).ReturnsAsync(users);

            // Act
            var result = await _controller.Users();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<List<UserDto>>(viewResult.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task ChangeRole_Post_ValidRequest_RedirectsWithSuccess()
        {
            // Arrange
            var userId = "user123";
            var role = "Admin";

            _adminServiceMock.Setup(x => x.ChangeUserRoleAsync(userId, role))
                .ReturnsAsync(Result.Success());

            // Act
            var result = await _controller.ChangeRole(userId, role);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AdminController.Users), redirectResult.ActionName);
            Assert.Equal("Role updated.", _controller.TempData["Success"]);
        }

        [Fact]
        public async Task ChangeRole_Post_RequestFails_RedirectsWithError()
        {
            // Arrange
            var userId = "user123";
            var role = "Admin";

            _adminServiceMock.Setup(x => x.ChangeUserRoleAsync(userId, role))
                .ReturnsAsync(Result.Failure("Role change failed"));

            // Act
            var result = await _controller.ChangeRole(userId, role);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AdminController.Users), redirectResult.ActionName);
            Assert.Equal("Role change failed", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task DeleteUser_Post_ValidRequest_RedirectsWithSuccess()
        {
            // Arrange
            var userId = "user123";

            _adminServiceMock.Setup(x => x.DeleteUserAsync(userId))
                .ReturnsAsync(Result.Success());

            // Act
            var result = await _controller.DeleteUser(userId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AdminController.Users), redirectResult.ActionName);
            Assert.Equal("User deleted.", _controller.TempData["Success"]);
        }

        [Fact]
        public async Task DeleteUser_Post_RequestFails_RedirectsWithError()
        {
            // Arrange
            var userId = "user123";

            _adminServiceMock.Setup(x => x.DeleteUserAsync(userId))
                .ReturnsAsync(Result.Failure("Delete failed"));

            // Act
            var result = await _controller.DeleteUser(userId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AdminController.Users), redirectResult.ActionName);
            Assert.Equal("Delete failed", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task Classes_Get_ReturnsViewWithSessionsAndPrograms()
        {
            // Arrange
            var sessions = new List<ClassSessionDto>
            {
                new ClassSessionDto { Id = 1, ProgramTitle = "Yoga Class", InstructorName = "Jane" }
            };
            var programs = new List<TrainingProgramDto>
            {
                new TrainingProgramDto { Id = 1, Title = "Yoga" }
            };

            _adminServiceMock.Setup(x => x.GetAllSessionsAsync()).ReturnsAsync(sessions);
            _adminServiceMock.Setup(x => x.GetAllProgramsAsync()).ReturnsAsync(programs);

            // Act
            var result = await _controller.Classes();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<AdminClassesViewModel>(viewResult.Model);
            Assert.Single(model.Sessions);
            Assert.Single(model.Programs);
        }

        [Fact]
        public async Task CreateClass_Post_ValidModel_RedirectsWithSuccess()
        {
            // Arrange
            var viewModel = new AdminClassesViewModel
            {
                Form = new ClassSessionDto
                {
                    TrainingProgramId = 1,
                    InstructorName = "John Doe",
                    StartTime = DateTime.UtcNow.AddDays(1),
                    EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                    Location = "Studio A",
                    MaxParticipants = 20
                }
            };

            _adminServiceMock.Setup(x => x.CreateSessionAsync(viewModel.Form))
                .ReturnsAsync(Result.Success());

            // Act
            var result = await _controller.CreateClass(viewModel);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AdminController.Classes), redirectResult.ActionName);
            Assert.Equal("Class session created.", _controller.TempData["Success"]);
        }

        [Fact]
        public async Task CreateClass_Post_ValidModel_CreatesSession()
        {
            // Arrange
            var viewModel = new AdminClassesViewModel
            {
                Form = new ClassSessionDto
                {
                    TrainingProgramId = 1,
                    InstructorName = "John Doe",
                    StartTime = DateTime.UtcNow.AddDays(1),
                    EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                    Location = "Studio A",
                    MaxParticipants = 20
                }
            };

            _adminServiceMock.Setup(x => x.CreateSessionAsync(viewModel.Form))
                .ReturnsAsync(Result.Success());

            // Act
            var result = await _controller.CreateClass(viewModel);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            _adminServiceMock.Verify(x => x.CreateSessionAsync(It.IsAny<ClassSessionDto>()), Times.Once);
        }

        [Fact]
        public async Task CreateClass_Post_InvalidModel_ReturnsViewWithErrors()
        {
            // Arrange
            var viewModel = new AdminClassesViewModel { Form = new ClassSessionDto() };
            _controller.ModelState.AddModelError("Form.TrainingProgramId", "Program is required");

            var sessions = new List<ClassSessionDto>();
            var programs = new List<TrainingProgramDto>();
            _adminServiceMock.Setup(x => x.GetAllSessionsAsync()).ReturnsAsync(sessions);
            _adminServiceMock.Setup(x => x.GetAllProgramsAsync()).ReturnsAsync(programs);

            // Act
            var result = await _controller.CreateClass(viewModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Classes", viewResult.ViewName);
            var model = Assert.IsType<AdminClassesViewModel>(viewResult.Model);
            Assert.Equal(sessions, model.Sessions);
            Assert.Equal(programs, model.Programs);
        }

        [Fact]
        public async Task CreateClass_Post_MissingInstructorName_ReturnsViewWithError()
        {
            // Arrange
            var viewModel = new AdminClassesViewModel
            {
                Form = new ClassSessionDto
                {
                    TrainingProgramId = 1,
                    InstructorName = "",
                    StartTime = DateTime.UtcNow.AddDays(1),
                    EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                    Location = "Studio A",
                    MaxParticipants = 20
                }
            };

            var sessions = new List<ClassSessionDto>();
            var programs = new List<TrainingProgramDto>();
            _adminServiceMock.Setup(x => x.GetAllSessionsAsync()).ReturnsAsync(sessions);
            _adminServiceMock.Setup(x => x.GetAllProgramsAsync()).ReturnsAsync(programs);

            // Act
            var result = await _controller.CreateClass(viewModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Classes", viewResult.ViewName);
            Assert.Contains(_controller.ModelState, m => m.Key == "Form.InstructorName");
        }

        [Fact]
        public async Task CreateClass_Post_StartTimeAfterEndTime_ReturnsViewWithError()
        {
            // Arrange
            var viewModel = new AdminClassesViewModel
            {
                Form = new ClassSessionDto
                {
                    TrainingProgramId = 1,
                    InstructorName = "John Doe",
                    StartTime = DateTime.UtcNow.AddDays(2),
                    EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                    Location = "Studio A",
                    MaxParticipants = 20
                }
            };

            var sessions = new List<ClassSessionDto>();
            var programs = new List<TrainingProgramDto>();
            _adminServiceMock.Setup(x => x.GetAllSessionsAsync()).ReturnsAsync(sessions);
            _adminServiceMock.Setup(x => x.GetAllProgramsAsync()).ReturnsAsync(programs);

            // Act
            var result = await _controller.CreateClass(viewModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Classes", viewResult.ViewName);
            Assert.Contains(_controller.ModelState, m => m.Key == "Form.EndTime");
        }

        [Fact]
        public async Task CreateClass_Post_InvalidMaxParticipants_ReturnsViewWithError()
        {
            // Arrange
            var viewModel = new AdminClassesViewModel
            {
                Form = new ClassSessionDto
                {
                    TrainingProgramId = 1,
                    InstructorName = "John Doe",
                    StartTime = DateTime.UtcNow.AddDays(1),
                    EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                    Location = "Studio A",
                    MaxParticipants = 0
                }
            };

            var sessions = new List<ClassSessionDto>();
            var programs = new List<TrainingProgramDto>();
            _adminServiceMock.Setup(x => x.GetAllSessionsAsync()).ReturnsAsync(sessions);
            _adminServiceMock.Setup(x => x.GetAllProgramsAsync()).ReturnsAsync(programs);

            // Act
            var result = await _controller.CreateClass(viewModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Classes", viewResult.ViewName);
            Assert.Contains(_controller.ModelState, m => m.Key == "Form.MaxParticipants");
        }

        [Fact]
        public async Task DeleteClass_Post_ValidRequest_RedirectsWithSuccess()
        {
            // Arrange
            var sessionId = 1;

            _adminServiceMock.Setup(x => x.DeleteSessionAsync(sessionId))
                .ReturnsAsync(Result.Success());

            // Act
            var result = await _controller.DeleteClass(sessionId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AdminController.Classes), redirectResult.ActionName);
            Assert.Equal("Session deleted.", _controller.TempData["Success"]);
        }

        [Fact]
        public async Task DeleteClass_Post_RequestFails_RedirectsWithError()
        {
            // Arrange
            var sessionId = 1;

            _adminServiceMock.Setup(x => x.DeleteSessionAsync(sessionId))
                .ReturnsAsync(Result.Failure("Delete failed"));

            // Act
            var result = await _controller.DeleteClass(sessionId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AdminController.Classes), redirectResult.ActionName);
            Assert.Equal("Delete failed", _controller.TempData["Error"]);
        }

        [Fact]
        public async Task DeleteClass_Post_ServiceReturnsFailure_ShowsErrorMessage()
        {
            // Arrange
            var sessionId = 999;
            var errorMessage = "Session not found";

            _adminServiceMock.Setup(x => x.DeleteSessionAsync(sessionId))
                .ReturnsAsync(Result.Failure(errorMessage));

            // Act
            var result = await _controller.DeleteClass(sessionId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(errorMessage, _controller.TempData["Error"]);
        }
    }
}