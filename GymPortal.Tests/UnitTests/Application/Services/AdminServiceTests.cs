using FluentAssertions;
using GymPortal.Application.DTOs;
using GymPortal.Application.Interfaces.Repositories;
using GymPortal.Application.Services;
using GymPortal.Domain.Common;
using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace GymPortal.Tests.UnitTests.Services
{
    public class AdminServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IBaseRepository<Membership>> _membershipRepoMock;
        private readonly Mock<IBaseRepository<ClassSession>> _sessionRepoMock;
        private readonly Mock<IBaseRepository<TrainingProgram>> _programRepoMock;
        private readonly AdminService _adminService;

        public AdminServiceTests()
        {
            _userManagerMock = CreateUserManagerMock();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _membershipRepoMock = new Mock<IBaseRepository<Membership>>();
            _sessionRepoMock = new Mock<IBaseRepository<ClassSession>>();
            _programRepoMock = new Mock<IBaseRepository<TrainingProgram>>();

            _unitOfWorkMock.Setup(u => u.Repository<Membership>()).Returns(_membershipRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<ClassSession>()).Returns(_sessionRepoMock.Object);
            _unitOfWorkMock.Setup(u => u.Repository<TrainingProgram>()).Returns(_programRepoMock.Object);

            _adminService = new AdminService(_userManagerMock.Object, _unitOfWorkMock.Object);
        }

        private Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var mock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            mock.Object.UserValidators.Add(new UserValidator<ApplicationUser>());
            mock.Object.PasswordValidators.Add(new PasswordValidator<ApplicationUser>());

            return mock;
        }

        // Helper to create an async queryable mock
        private static Mock<DbSet<T>> CreateDbSetMock<T>(List<T> items) where T : class
        {
            var queryable = items.AsQueryable();
            var dbSetMock = new Mock<DbSet<T>>();

            dbSetMock.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(items.GetEnumerator()));

            dbSetMock.As<IQueryable<T>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<T>(queryable.Provider));

            dbSetMock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

            return dbSetMock;
        }

        #region GetAllUsersAsync Tests

        [Fact]
        public async Task GetAllUsersAsync_WhenUsersExist_ShouldReturnListOfUserDtos()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "1", Email = "user1@test.com", FirstName = "John", LastName = "Doe" },
                new ApplicationUser { Id = "2", Email = "user2@test.com", FirstName = "Jane", LastName = "Smith" }
            };

            var userDbSetMock = CreateDbSetMock(users);

            _userManagerMock.Setup(x => x.Users).Returns(userDbSetMock.Object);
            _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "Member" });
            _membershipRepoMock.Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Membership, bool>>>()))
                .ReturnsAsync(new List<Membership>());

            // Act
            var result = await _adminService.GetAllUsersAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(u => u.Email == "user1@test.com");
            result.Should().Contain(u => u.Email == "user2@test.com");
        }

        [Fact]
        public async Task GetAllUsersAsync_WhenNoUsers_ShouldReturnEmptyList()
        {
            // Arrange
            var users = new List<ApplicationUser>();
            var userDbSetMock = CreateDbSetMock(users);

            _userManagerMock.Setup(x => x.Users).Returns(userDbSetMock.Object);

            // Act
            var result = await _adminService.GetAllUsersAsync();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllUsersAsync_WhenUserHasMembership_ShouldIncludeMembershipInfo()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                new ApplicationUser { Id = "1", Email = "user1@test.com", FirstName = "John", LastName = "Doe" }
            };

            var userDbSetMock = CreateDbSetMock(users);

            var membership = new Membership
            {
                UserId = "1",
                PlanName = "Premium",
                EndDate = DateTime.UtcNow.AddMonths(1),
                Status = MembershipStatus.Active
            };

            _userManagerMock.Setup(x => x.Users).Returns(userDbSetMock.Object);
            _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { "Member" });
            _membershipRepoMock.Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Membership, bool>>>()))
                .ReturnsAsync(new List<Membership> { membership });

            // Act
            var result = await _adminService.GetAllUsersAsync();

            // Assert
            result.Should().HaveCount(1);
            var userDto = result.First();
            userDto.MembershipPlanName.Should().Be("Premium");
            userDto.MembershipEndDate.Should().Be(membership.EndDate);
        }

        #endregion

        #region ChangeUserRoleAsync Tests

        [Fact]
        public async Task ChangeUserRoleAsync_WhenUserExists_ShouldSucceed()
        {
            // Arrange
            var userId = "user123";
            var newRole = "Admin";
            var user = new ApplicationUser { Id = userId };

            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Member" });
            _userManagerMock.Setup(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.AddToRoleAsync(user, newRole))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _adminService.ChangeUserRoleAsync(userId, newRole);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _userManagerMock.Verify(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()), Times.Once);
            _userManagerMock.Verify(x => x.AddToRoleAsync(user, newRole), Times.Once);
        }

        [Fact]
        public async Task ChangeUserRoleAsync_WhenUserNotFound_ShouldReturnFailure()
        {
            // Arrange
            var userId = "nonexistent";
            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _adminService.ChangeUserRoleAsync(userId, "Admin");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("User not found.");
        }

        [Fact]
        public async Task ChangeUserRoleAsync_WhenAddRoleFails_ShouldReturnFailure()
        {
            // Arrange
            var userId = "user123";
            var user = new ApplicationUser { Id = userId };
            var errors = new[] { new IdentityError { Description = "Role already assigned" } };

            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string>());
            _userManagerMock.Setup(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.AddToRoleAsync(user, "Admin"))
                .ReturnsAsync(IdentityResult.Failed(errors));

            // Act
            var result = await _adminService.ChangeUserRoleAsync(userId, "Admin");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Role already assigned");
        }

        #endregion

        #region DeleteUserAsync Tests

        [Fact]
        public async Task DeleteUserAsync_WhenUserExists_ShouldSucceed()
        {
            // Arrange
            var userId = "user123";
            var user = new ApplicationUser { Id = userId };

            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _adminService.DeleteUserAsync(userId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _userManagerMock.Verify(x => x.DeleteAsync(user), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAsync_WhenUserNotFound_ShouldReturnFailure()
        {
            // Arrange
            var userId = "nonexistent";
            _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _adminService.DeleteUserAsync(userId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("User not found.");
        }

        #endregion

        #region GetAllSessionsAsync Tests

        [Fact]
        public async Task GetAllSessionsAsync_WhenSessionsExist_ShouldReturnListOfSessionDtos()
        {
            // Arrange
            var programs = new List<TrainingProgram>
            {
                new TrainingProgram { Id = 1, Title = "Yoga", Category = "Wellness" },
                new TrainingProgram { Id = 2, Title = "HIIT", Category = "Cardio" }
            };
            var sessions = new List<ClassSession>
            {
                new ClassSession
                {
                    Id = 1,
                    TrainingProgramId = 1,
                    InstructorName = "Jane",
                    MaxParticipants = 20,
                    Bookings = new List<Booking>(),
                    IsActive = true,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow.AddHours(1),
                    Location = "Studio A"
                },
                new ClassSession
                {
                    Id = 2,
                    TrainingProgramId = 2,
                    InstructorName = "John",
                    MaxParticipants = 15,
                    Bookings = new List<Booking>(),
                    IsActive = true,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow.AddHours(1),
                    Location = "Studio B"
                }
            };

            _programRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(programs);
            _sessionRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(sessions);

            // Act
            var result = await _adminService.GetAllSessionsAsync();

            // Assert
            result.Should().HaveCount(2);
            var firstSession = result.First();
            firstSession.ProgramTitle.Should().Be("Yoga");
            firstSession.InstructorName.Should().Be("Jane");
        }

        [Fact]
        public async Task GetAllSessionsAsync_WhenNoSessions_ShouldReturnEmptyList()
        {
            // Arrange
            _programRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<TrainingProgram>());
            _sessionRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<ClassSession>());

            // Act
            var result = await _adminService.GetAllSessionsAsync();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region CreateSessionAsync Tests

        [Fact]
        public async Task CreateSessionAsync_WithValidDto_ShouldSucceed()
        {
            // Arrange
            var dto = new ClassSessionDto
            {
                TrainingProgramId = 1,
                InstructorName = "John Doe",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
                Location = "Studio A",
                MaxParticipants = 20
            };

            // Act
            var result = await _adminService.CreateSessionAsync(dto);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _sessionRepoMock.Verify(x => x.AddAsync(It.IsAny<ClassSession>()), Times.Once);
            _unitOfWorkMock.Verify(x => x.CompleteAsync(), Times.Once);
        }

        #endregion

        #region DeleteSessionAsync Tests

        [Fact]
        public async Task DeleteSessionAsync_WhenSessionExists_ShouldSucceed()
        {
            // Arrange
            var sessionId = 1;
            var session = new ClassSession { Id = sessionId };

            _sessionRepoMock.Setup(x => x.GetByIdAsync(sessionId)).ReturnsAsync(session);

            // Act
            var result = await _adminService.DeleteSessionAsync(sessionId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _sessionRepoMock.Verify(x => x.Delete(session), Times.Once);
            _unitOfWorkMock.Verify(x => x.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteSessionAsync_WhenSessionNotFound_ShouldReturnFailure()
        {
            // Arrange
            var sessionId = 999;
            _sessionRepoMock.Setup(x => x.GetByIdAsync(sessionId)).ReturnsAsync((ClassSession)null);

            // Act
            var result = await _adminService.DeleteSessionAsync(sessionId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Session not found.");
        }

        #endregion

        #region GetAllProgramsAsync Tests

        [Fact]
        public async Task GetAllProgramsAsync_WhenProgramsExist_ShouldReturnListOfProgramDtos()
        {
            // Arrange
            var programs = new List<TrainingProgram>
            {
                new TrainingProgram { Id = 1, Title = "Yoga", Category = "Wellness", DifficultyLevel = DifficultyLevel.Beginner },
                new TrainingProgram { Id = 2, Title = "HIIT", Category = "Cardio", DifficultyLevel = DifficultyLevel.Intermediate }
            };

            _programRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(programs);

            // Act
            var result = await _adminService.GetAllProgramsAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(p => p.Title == "Yoga");
            result.Should().Contain(p => p.Title == "HIIT");
        }

        [Fact]
        public async Task GetAllProgramsAsync_WhenNoPrograms_ShouldReturnEmptyList()
        {
            // Arrange
            _programRepoMock.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<TrainingProgram>());

            // Act
            var result = await _adminService.GetAllProgramsAsync();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion
    }

    #region Test Helpers for Async Queryable

    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        internal TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var expectedResultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                .GetMethod(nameof(IQueryProvider.Execute), 1, new[] { typeof(Expression) })
                .MakeGenericMethod(expectedResultType)
                .Invoke(_inner, new[] { expression });

            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
                ?.MakeGenericMethod(expectedResultType)
                .Invoke(null, new[] { executionResult });
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        { }

        public TestAsyncEnumerable(Expression expression)
            : base(expression)
        { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public T Current => _inner.Current;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return ValueTask.FromResult(_inner.MoveNext());
        }
    }

    #endregion
}