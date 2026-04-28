using FluentAssertions;
using GymPortal.Domain.Common;
using Xunit;

namespace GymPortal.Tests.UnitTests.Domain.Common
{
    public class ResultTests
    {
        [Fact]
        public void Success_Generic_ShouldCreateSuccessfulResult()
        {
            // Arrange & Act
            var result = Result<string>.Success("test");

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.IsFailure.Should().BeFalse();
            result.Value.Should().Be("test");
            result.Error.Should().BeNull();
        }

        [Fact]
        public void Failure_Generic_ShouldCreateFailedResult()
        {
            // Arrange & Act
            var result = Result<string>.Failure("Error occurred");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.IsFailure.Should().BeTrue();
            result.Value.Should().BeNull();
            result.Error.Should().Be("Error occurred");
        }

        [Fact]
        public void Success_NonGeneric_ShouldCreateSuccessfulResult()
        {
            // Arrange & Act
            var result = Result.Success();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.IsFailure.Should().BeFalse();
            result.Error.Should().BeNull();
        }

        [Fact]
        public void Failure_NonGeneric_ShouldCreateFailedResult()
        {
            // Arrange & Act
            var result = Result.Failure("Error occurred");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be("Error occurred");
        }
    }
}