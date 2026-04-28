using GymPortal.Application.DTOs;
using GymPortal.Domain.Common;


namespace GymPortal.Application.Interfaces.Services
{
    public interface IAdminService
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<Result> ChangeUserRoleAsync(string userId, string newRole);
        Task<Result> DeleteUserAsync(string userId);
        Task<IEnumerable<ClassSessionDto>> GetAllSessionsAsync();
        Task<Result> CreateSessionAsync(ClassSessionDto sessionDto);
        Task<Result> DeleteSessionAsync(int sessionId);
        Task<IEnumerable<TrainingProgramDto>> GetAllProgramsAsync();
    }
}
