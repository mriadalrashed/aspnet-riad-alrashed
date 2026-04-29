using GymPortal.Application.DTOs;
using GymPortal.Application.Interfaces.Repositories;
using GymPortal.Application.Interfaces.Services;
using GymPortal.Domain.Common;
using GymPortal.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace GymPortal.Application.Services
{
    public class ClassService : IClassService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ClassService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<ClassSession>> GetAvailableSessionsAsync(string? category = null)
        {
            var sessionRepo = _unitOfWork.Repository<ClassSession>();
            var query = await sessionRepo.GetAllAsync();
            var sessions = query.Where(s => s.IsActive && s.StartTime > DateTime.UtcNow).ToList();
            if (!string.IsNullOrEmpty(category))
            {
                var programRepo = _unitOfWork.Repository<TrainingProgram>();
                var program = await programRepo.GetAllAsync();
                var programIds = program.Where(p => p.Category == category).Select(p => p.Id).ToList();

                sessions = sessions.Where(s => programIds.Contains(s.TrainingProgramId)).ToList();
            }
            return sessions;
        }

        public async Task<ClassSession> GetSessionByIdAsync(int Id)
        {
            return await _unitOfWork.Repository<ClassSession>().GetByIdAsync(Id);
        }

        public async Task<Result<ClassSession>> CreateSessionAsync(ClassSession session)
        {
            var repo = _unitOfWork.Repository<ClassSession>();
            await repo.AddAsync(session);
            await _unitOfWork.CompleteAsync();
            return Result<ClassSession>.Success(session);
        }

        public async Task<Result> DeleteSessionAsync(int id)
        {
            var repo = _unitOfWork.Repository<ClassSession>();
            var session = await repo.GetByIdAsync(id);
            if (session == null)
                return Result.Failure("Session not found.");
            repo.Delete(session);
            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }

        public async Task<IEnumerable<TrainingProgram>> GetAllProgramsAsync()
        {
           return await _unitOfWork.Repository<TrainingProgram>().GetAllAsync();
        }
    }
}
