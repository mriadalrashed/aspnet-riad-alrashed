using FluentAssertions;
using GymPortal.Application.DTOs;
using GymPortal.Application.Interfaces.Services;
using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using GymPortal.Web.Controllers;
using GymPortal.Web.Models;
using GymPortal.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GymPortal.Tests.UnitTests.Controllers
{
    public class HomeControllerTests
    {
        private readonly Mock<IClassService> _classServiceMock;
        private readonly HomeController _controller;
        private readonly DefaultHttpContext _httpContext;

        public HomeControllerTests()
        {
            _classServiceMock = new Mock<IClassService>();
            _controller = new HomeController(_classServiceMock.Object);

            // Setup HttpContext
            _httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };
        }

        [Fact]
        public async Task Index_Get_ReturnsViewWithPrograms()
        {
            // Arrange
            var programs = new List<TrainingProgram>
            {
                new TrainingProgram
                {
                    Id = 1,
                    Title = "Yoga",
                    Description = "Relaxing yoga",
                    Category = "Wellness",
                    DifficultyLevel = DifficultyLevel.Beginner,
                    ImageUrl = "/images/yoga.jpg"
                },
                new TrainingProgram
                {
                    Id = 2,
                    Title = "HIIT",
                    Description = "High intensity",
                    Category = "Cardio",
                    DifficultyLevel = DifficultyLevel.Intermediate,
                    ImageUrl = "/images/hiit.jpg"
                }
            };

            _classServiceMock.Setup(x => x.GetAllProgramsAsync()).ReturnsAsync(programs);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<HomeViewModel>(viewResult.Model);
            Assert.Equal(2, model.TrainingPrograms.Count);
            Assert.Equal("Yoga", model.TrainingPrograms[0].Title);
        }

        [Fact]
        public async Task Index_Get_WhenNoPrograms_ReturnsEmptyList()
        {
            // Arrange
            var programs = new List<TrainingProgram>();
            _classServiceMock.Setup(x => x.GetAllProgramsAsync()).ReturnsAsync(programs);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<HomeViewModel>(viewResult.Model);
            Assert.Empty(model.TrainingPrograms);
            Assert.Empty(model.FeaturedProducts);
        }

        [Fact]
        public void NotFoundPage_ReturnsError404View()
        {
            // Act
            var result = _controller.NotFoundPage();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Error404", viewResult.ViewName);
        }

        [Fact]
        public void Privacy_ReturnsView()
        {
            // Act
            var result = _controller.Privacy();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.Model);
        }

        [Fact]
        public void Error_ReturnsViewWithErrorViewModel()
        {
            // Act
            var result = _controller.Error();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
            Assert.NotNull(model.RequestId);
        }

        [Fact]
        public void Error_WhenTraceIdentifierIsNull_StillReturnsView()
        {
            // Arrange
            _httpContext.TraceIdentifier = null;
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };

            // Act
            var result = _controller.Error();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
            // The RequestId might be null, but that's acceptable
            Assert.NotNull(model);
        }
    }
}