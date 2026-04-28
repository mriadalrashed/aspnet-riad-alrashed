using FluentAssertions;
using GymPortal.Domain.Common;
using Xunit;

namespace GymPortal.Tests.UnitTests.Domain.Entities
{
    public class ResultPatternTests
    {
        [Fact]
        public void Result_Success_ShouldHaveIsSuccessTrue()
        {
            // Act
            var result = Result<string>.Success("Test");

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.IsFailure.Should().BeFalse();
            result.Value.Should().Be("Test");
            result.Error.Should().BeNull();
        }

        [Fact]
        public void Result_Failure_ShouldHaveIsSuccessFalseAndError()
        {
            // Act
            var result = Result<string>.Failure("Error occurred");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.IsFailure.Should().BeTrue();
            result.Value.Should().BeNull();
            result.Error.Should().Be("Error occurred");
        }

        [Fact]
        public void Result_VoidSuccess_ShouldHaveIsSuccessTrue()
        {
            // Act
            var result = Result.Success();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.IsFailure.Should().BeFalse();
            result.Error.Should().BeNull();
        }

        [Fact]
        public void Result_VoidFailure_ShouldHaveIsSuccessFalseAndError()
        {
            // Act
            var result = Result.Failure("Error occurred");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be("Error occurred");
        }
    }
}