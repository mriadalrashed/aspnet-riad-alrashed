using FluentAssertions;
using GymPortal.Application.Interfaces.Repositories;
using GymPortal.Application.Services;
using GymPortal.Domain.Common;
using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using Moq;
using Xunit;

namespace GymPortal.Tests.UnitTests.Services
{
    public class MembershipServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IBaseRepository<Membership>> _membershipRepoMock;
        private readonly MembershipService _membershipService;

        public MembershipServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _membershipRepoMock = new Mock<IBaseRepository<Membership>>();

            _unitOfWorkMock.Setup(u => u.Repository<Membership>()).Returns(_membershipRepoMock.Object);

            _membershipService = new MembershipService(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task CreateMembershipAsync_NewMembership_ShouldSucceed()
        {
            // Arrange
            var userId = "user-123";
            var type = MembershipType.Monthly;
            var planName = "Premium";

            _membershipRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Membership, bool>>>()))
                .ReturnsAsync(new List<Membership>());

            // Act
            var result = await _membershipService.CreateMembershipAsync(userId, type, planName);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.UserId.Should().Be(userId);
            result.Value.PlanName.Should().Be(planName);
            result.Value.Type.Should().Be(type);
            result.Value.Status.Should().Be(MembershipStatus.Active);
            _membershipRepoMock.Verify(r => r.AddAsync(It.IsAny<Membership>()), Times.Once);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateMembershipAsync_ExistingActiveMembership_ShouldReturnFailure()
        {
            // Arrange
            var userId = "user-123";
            var type = MembershipType.Monthly;
            var existingMembership = new Membership
            {
                UserId = userId,
                Status = MembershipStatus.Active
            };

            _membershipRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Membership, bool>>>()))
                .ReturnsAsync(new List<Membership> { existingMembership });

            // Act
            var result = await _membershipService.CreateMembershipAsync(userId, type);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("User already has an active membership.");
            _membershipRepoMock.Verify(r => r.AddAsync(It.IsAny<Membership>()), Times.Never);
        }

        [Fact]
        public async Task CreateMembershipAsync_WithoutPlanName_ShouldUseDefaultPlanName()
        {
            // Arrange
            var userId = "user-123";
            var type = MembershipType.Monthly;

            _membershipRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Membership, bool>>>()))
                .ReturnsAsync(new List<Membership>());

            // Act
            var result = await _membershipService.CreateMembershipAsync(userId, type);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.PlanName.Should().Be("Monthly");
        }

        [Fact]
        public async Task CreateMembershipAsync_Yearly_ShouldSetCorrectEndDate()
        {
            // Arrange
            var userId = "user-123";
            var type = MembershipType.Yearly;
            var startDate = DateTime.UtcNow;

            _membershipRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Membership, bool>>>()))
                .ReturnsAsync(new List<Membership>());

            // Act
            var result = await _membershipService.CreateMembershipAsync(userId, type);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.EndDate.Should().BeAfter(startDate.AddMonths(11));
            result.Value.EndDate.Should().BeBefore(startDate.AddMonths(13));
        }

        [Fact]
        public async Task GetUserMembershipAsync_ExistingMembership_ShouldReturnMembership()
        {
            // Arrange
            var userId = "user-123";
            var membership = new Membership
            {
                UserId = userId,
                Status = MembershipStatus.Active
            };

            _membershipRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Membership, bool>>>()))
                .ReturnsAsync(new List<Membership> { membership });

            // Act
            var result = await _membershipService.GetUserMembershipAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(userId);
        }

        [Fact]
        public async Task GetUserMembershipAsync_NoMembership_ShouldReturnNull()
        {
            // Arrange
            var userId = "user-123";

            _membershipRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Membership, bool>>>()))
                .ReturnsAsync(new List<Membership>());

            // Act
            var result = await _membershipService.GetUserMembershipAsync(userId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CancelMembershipAsync_ExistingMembership_ShouldSucceed()
        {
            // Arrange
            var userId = "user-123";
            var membership = new Membership
            {
                UserId = userId,
                Status = MembershipStatus.Active
            };

            _membershipRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Membership, bool>>>()))
                .ReturnsAsync(new List<Membership> { membership });

            // Act
            var result = await _membershipService.CancelMembershipAsync(userId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            membership.Status.Should().Be(MembershipStatus.Cancelled);
            _membershipRepoMock.Verify(r => r.Update(membership), Times.Once);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task CancelMembershipAsync_NoActiveMembership_ShouldReturnFailure()
        {
            // Arrange
            var userId = "user-123";

            _membershipRepoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Membership, bool>>>()))
                .ReturnsAsync(new List<Membership>());

            // Act
            var result = await _membershipService.CancelMembershipAsync(userId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("No active membership found.");
        }
    }
}