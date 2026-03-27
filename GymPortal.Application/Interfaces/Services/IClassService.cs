using GymPortal.Domain.Common;
using GymPortal.Domain.Entities;


namespace GymPortal.Application.Interfaces.Services
{
    public interface IClassService
    {
        Task<IEnumerable<ClassSession>> GetAvailableSessionsAsync(string? category = null);
        Task<ClassSession?> GetSessionByIdAsync(int id);
        Task<Result<ClassSession>> CreateSessionAsync(ClassSession session);
        Task<Result> DeleteSessionAsync(int id);
        Task<IEnumerable<TrainingProgram>> GetAllProgramsAsync();
    }
}
