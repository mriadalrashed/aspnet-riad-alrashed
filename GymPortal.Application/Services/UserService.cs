using GymPortal.Application.DTOs;
using GymPortal.Application.Interfaces.Repositories;
using GymPortal.Application.Interfaces.Services;
using GymPortal.Domain.Common;
using GymPortal.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace GymPortal.Application.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;

        public UserService(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<UserDto?> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            return await MapToDto(user);
        }

        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return null;

            return await MapToDto(user);
        }

        public async Task<Result> UpdateUserProfileAsync(string userId, string firstName, string lastName, string? phoneNumber, DateTime? dateOfBirth, string? profileImageUrl)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Result.Failure("User not found.");

            user.FirstName = firstName;
            user.LastName = lastName;
            user.PhoneNumber = phoneNumber;
            user.DateOfBirth = dateOfBirth;
            if (!string.IsNullOrEmpty(profileImageUrl))
                user.ProfileImageUrl = profileImageUrl;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded ? Result.Success() : Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        public async Task<Result> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Result.Failure("User not found.");

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded ? Result.Success() : Result.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        private async Task<UserDto> MapToDto(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var membershipRepo = _unitOfWork.Repository<Membership>();
            var membership = (await membershipRepo.FindAsync(m => m.UserId == user.Id && m.Status == Domain.Enums.MembershipStatus.Active)).FirstOrDefault();

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                ProfileImageUrl = user.ProfileImageUrl,
                DateOfBirth = user.DateOfBirth,
                CreatedAt = DateTime.UtcNow, 
                Role = roles.FirstOrDefault(),
                MembershipPlanName = membership?.PlanName,
                MembershipEndDate = membership?.EndDate
            };
        }
    }
}