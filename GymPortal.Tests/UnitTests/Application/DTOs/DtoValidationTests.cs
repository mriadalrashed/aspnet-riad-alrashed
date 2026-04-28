using FluentAssertions;
using GymPortal.Application.DTOs;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace GymPortal.Tests.UnitTests.Application.DTOs
{
    public class DtoValidationTests
    {
        [Fact]
        public void RegisterDto_ValidData_ShouldPassValidation()
        {
            // Arrange
            var dto = new RegisterDto
            {
                Email = "test@example.com",
                Password = "Password123!",
                FirstName = "John",
                LastName = "Doe",
                PhoneNumber = "1234567890"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void RegisterDto_MissingRequiredFields_ShouldFailValidation()
        {
            // Arrange
            var dto = new RegisterDto();

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            validationResults.Should().HaveCount(4); // Email, Password, FirstName, LastName are required
        }

        [Fact]
        public void LoginDto_ValidData_ShouldPassValidation()
        {
            // Arrange
            var dto = new LoginDto
            {
                Email = "test@example.com",
                Password = "password123"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void ContactFormDto_ValidData_ShouldPassValidation()
        {
            // Arrange
            var dto = new ContactFormDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Message = "This is a test message"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void ContactFormDto_MissingMessage_ShouldFailValidation()
        {
            // Arrange
            var dto = new ContactFormDto
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            validationResults.Should().Contain(v => v.MemberNames.Contains("Message"));
        }

        private List<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var ctx = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, ctx, validationResults, true);
            return validationResults;
        }
    }
}