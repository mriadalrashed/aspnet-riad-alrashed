using GymPortal.Application.DTOs;
using GymPortal.Domain.Common;


namespace GymPortal.Application.Interfaces.Services
{
    public interface IUserService
    {
        Task<UserDto?> GetUserByIdAsync(string userId);
        Task<UserDto?> GetUserByEmailAsync(string email);
        Task<Result> UpdateUserProfileAsync(string userId, string firstName, string lastName, string? phoneNumber, DateTime? dateOfBirth, string? profileImageUrl);
        Task<Result> DeleteUserAsync(string userId);
    }
}
