using FluentAssertions;
using GymPortal.Application.DTOs;
using GymPortal.Web.Controllers;
using GymPortal.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace GymPortal.Tests.UnitTests.Controllers
{
    public class CustomerServiceControllerTests
    {
        private readonly CustomerServiceController _controller;
        private readonly ITempDataDictionary _tempData;
        private readonly DefaultHttpContext _httpContext;

        public CustomerServiceControllerTests()
        {
            _controller = new CustomerServiceController();

            // Setup HttpContext and TempData
            _httpContext = new DefaultHttpContext();
            _tempData = new TempDataDictionary(_httpContext, Mock.Of<ITempDataProvider>());
            _controller.TempData = _tempData;

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };
        }

        // Helper method to create a mock for ITempDataProvider
        private class MockTempDataProvider : ITempDataProvider
        {
            public IDictionary<string, object> LoadTempData(HttpContext context)
            {
                return new Dictionary<string, object>();
            }

            public void SaveTempData(HttpContext context, IDictionary<string, object> values)
            {
            }
        }

        [Fact]
        public void Index_Get_ReturnsView()
        {
            // Act
            var result = _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CustomerServiceViewModel>(viewResult.Model);
            Assert.NotNull(model.ContactForm);
        }

        [Fact]
        public void Contact_Post_ValidModel_RedirectsWithSuccess()
        {
            // Arrange
            var model = new ContactFormDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Message = "Test message"
            };

            // Act
            var result = _controller.Contact(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(CustomerServiceController.Index), redirectResult.ActionName);
            Assert.Equal("Your message has been sent. We'll get back to you soon.", _controller.TempData["Success"]);
        }

        [Fact]
        public void Contact_Post_ValidModel_ClearsModelStateAndRedirects()
        {
            // Arrange
            var model = new ContactFormDto
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane@example.com",
                PhoneNumber = "1234567890",
                Message = "Another test message"
            };

            // Act
            var result = _controller.Contact(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(CustomerServiceController.Index), redirectResult.ActionName);
            Assert.True(_controller.ModelState.IsValid);
        }

        [Fact]
        public void Contact_Post_InvalidModel_ReturnsViewWithModel()
        {
            // Arrange
            var model = new ContactFormDto();
            _controller.ModelState.AddModelError("Email", "Email is required");

            // Act
            var result = _controller.Contact(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var returnedModel = Assert.IsType<CustomerServiceViewModel>(viewResult.Model);
            Assert.Equal(model, returnedModel.ContactForm);
        }

        [Fact]
        public void Contact_Post_MissingFirstName_ReturnsViewWithError()
        {
            // Arrange
            var model = new ContactFormDto
            {
                LastName = "Doe",
                Email = "john@example.com",
                Message = "Test message"
            };
            _controller.ModelState.AddModelError("FirstName", "First name is required");

            // Act
            var result = _controller.Contact(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var returnedModel = Assert.IsType<CustomerServiceViewModel>(viewResult.Model);
            Assert.Equal(model, returnedModel.ContactForm);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public void Contact_Post_MissingLastName_ReturnsViewWithError()
        {
            // Arrange
            var model = new ContactFormDto
            {
                FirstName = "John",
                Email = "john@example.com",
                Message = "Test message"
            };
            _controller.ModelState.AddModelError("LastName", "Last name is required");

            // Act
            var result = _controller.Contact(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var returnedModel = Assert.IsType<CustomerServiceViewModel>(viewResult.Model);
            Assert.Equal(model, returnedModel.ContactForm);
        }

        [Fact]
        public void Contact_Post_MissingMessage_ReturnsViewWithError()
        {
            // Arrange
            var model = new ContactFormDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com"
            };
            _controller.ModelState.AddModelError("Message", "Message is required");

            // Act
            var result = _controller.Contact(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var returnedModel = Assert.IsType<CustomerServiceViewModel>(viewResult.Model);
            Assert.Equal(model, returnedModel.ContactForm);
        }

        [Fact]
        public void Contact_Post_InvalidEmailFormat_ReturnsViewWithError()
        {
            // Arrange
            var model = new ContactFormDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "invalid-email",
                Message = "Test message"
            };
            _controller.ModelState.AddModelError("Email", "Invalid email format");

            // Act
            var result = _controller.Contact(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var returnedModel = Assert.IsType<CustomerServiceViewModel>(viewResult.Model);
            Assert.Equal(model, returnedModel.ContactForm);
        }

        [Fact]
        public void Contact_Post_WithPhoneNumber_StillProcessesSuccessfully()
        {
            // Arrange
            var model = new ContactFormDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                PhoneNumber = "123-456-7890",
                Message = "Test message with phone"
            };

            // Act
            var result = _controller.Contact(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(CustomerServiceController.Index), redirectResult.ActionName);
            Assert.Equal("Your message has been sent. We'll get back to you soon.", _controller.TempData["Success"]);
        }

        [Fact]
        public void Contact_Post_WithAllFields_ProcessesSuccessfully()
        {
            // Arrange
            var model = new ContactFormDto
            {
                FirstName = "Johnathan",
                LastName = "Doe-Smith",
                Email = "johnathan.doe@example.com",
                PhoneNumber = "+1-555-123-4567",
                Message = "This is a detailed test message with multiple sentences. It should still work fine."
            };

            // Act
            var result = _controller.Contact(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(CustomerServiceController.Index), redirectResult.ActionName);
        }
    }
}