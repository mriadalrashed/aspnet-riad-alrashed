using FluentAssertions;
using GymPortal.Domain.Common;
using Xunit;

namespace GymPortal.Tests.UnitTests.Domain.Common
{
    public class BaseEntityTests
    {
        private class TestEntity : BaseEntity { }

        [Fact]
        public void BaseEntity_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var entity = new TestEntity();

            // Assert
            entity.Id.Should().Be(0);
            entity.CreatedAt.Should().Be(default);
            entity.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void BaseEntity_ShouldAllowSettingProperties()
        {
            // Arrange
            var entity = new TestEntity();
            var createdAt = DateTime.UtcNow;
            var updatedAt = DateTime.UtcNow.AddHours(1);

            // Act
            entity.Id = 1;
            entity.CreatedAt = createdAt;
            entity.UpdatedAt = updatedAt;

            // Assert
            entity.Id.Should().Be(1);
            entity.CreatedAt.Should().Be(createdAt);
            entity.UpdatedAt.Should().Be(updatedAt);
        }
    }
}