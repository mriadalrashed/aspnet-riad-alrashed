using FluentAssertions;
using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using System;
using Xunit;

namespace GymPortal.Tests.UnitTests.Domain.Entities
{
    public class MembershipTests
    {
        [Fact]
        public void Membership_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            var membership = new Membership();
            var startDate = DateTime.UtcNow;
            var endDate = DateTime.UtcNow.AddMonths(1);
            var user = new ApplicationUser { Id = "user-123" };

            // Act
            membership.Id = 1;
            membership.UserId = "user-123";
            membership.User = user;
            membership.PlanId = 2;
            membership.PlanName = "Premium";
            membership.StartDate = startDate;
            membership.EndDate = endDate;
            membership.Status = MembershipStatus.Active;
            membership.Type = MembershipType.Monthly;

            // Assert
            membership.Id.Should().Be(1);
            membership.UserId.Should().Be("user-123");
            membership.User.Should().BeSameAs(user);
            membership.PlanId.Should().Be(2);
            membership.PlanName.Should().Be("Premium");
            membership.StartDate.Should().Be(startDate);
            membership.EndDate.Should().Be(endDate);
            membership.Status.Should().Be(MembershipStatus.Active);
            membership.Type.Should().Be(MembershipType.Monthly);
        }
    }
}