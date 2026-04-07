using GymPortal.Domain.Entites;
using GymPortal.Domain.Common;
using GymPortal.Domain.Enum;
using GymPortal.Application.Interfaces.Repositories;
using GymPortal.Application.Interfaces.Services;
using GymPortal.Application.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GymPortal.Application.Services
{
    public class AdminService : IAdminService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var membershipRepo = await _unitOfWork.Repository<Membership>();
                var membership = (await membershipRepo.FindAsync(m => m.UserId == user.Id && m.Status == MembershipStatus.Active)).FirstOrDefault();
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    ProfileImageUrl = user.ProfileImageUrl,
                    DateOfBirth = user.DateOfBirth,
                    CreatedAt = user.CreatedAt,
                    Role = roles.FirstOrDefault(),
                    MembershipPlanName = membership?.MembershipPlanName,
                    MembershipEndDate = membership?.EndDate
                });
            }
            return userDtos;
        }

        public async Task<Result> ChangeUserRoleAsync(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Result.Failure("User not found");
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            var result = await _userManager.AddToRoleAsync(user, newRole);
            return result.Succeeded ? Result.Success() : Result.Failure(string.Join(",", result.Errors.Select(e => e.Description)));
        }

        public async Task<Result> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Result.Failure("User not found");
            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded ? Result.Success() : Result.Failure(string.Join(",", result.Errors.Select(e => e.Description)));
        }

        public async Task<IEnumerable<ClassSessionDto>> GetAllSessionsAsync()
        {
            var sessionRepo = _unitOfWork.Repository<ClassSession>();
            var programRepo = _unitOfWork.Repository<TrainingProgram>();
            var sessions = await sessionRepo.GetAllAsync();
            var programs = await programRepo.GetAllAsync();
            var programDict = programs.ToDictionary(p => p.Id);
            return sessions.Select(s => new ClassSessionDto
            {
                Id = s.Id,
                TrainingProgramId = s.TrainingProgramId,
                ProgramTitle = programDict.TryGetValue(s.TrainingProgramId, out var p) ? p.Title : "",
                Category = p?.category ?? ""  ,
                DifficultyLevel = p?.DifficultyLevel ?? DifficultyLevel.Beginner,
                InstructorName = s.InstructorName,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Location = s.Location,
                MaxParticipants = s.MaxParticipants,
                AvailableSpots = s.MaxParticipants - s.Bookings.Count(b => b.Status == BookingStatus.Confirmed),
                IsActive = s.IsActive
            });
        }

        public async Task<Result> CreateSessionAsync(ClassSessionDto dto)
        {
            var session = new ClassSession
            {
                TrainingProgramId = dto.TrainingProgramId,
                InstructorName = dto.InstructorName,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Location = dto.Location,
                MaxParticipants = dto.MaxParticipants,
                IsActive = true
            };
            var repo = _unitOfWork.Repository<ClassSession>();
            await repo.AddAsync(session);
            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }

        public async Task<Result> DeleteSessionAsync(int sessionId)
        {
            var repo = _unitOfWork.Repository<ClassSession>();
            var session = await repo.GetByIdAsync(sessionId);
            if (session == null)
                return Result.Failure("Session not found.");

            repo.Delete(session);
            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }

        public async Task<IEnumerable<TrainingProgramDto>> GetAllProgramsAsync()
        {
            var programs = await _unitOfWork.Repository<TrainingProgram>().GetAllAsync();
            return programs.Select(p => new TrainingProgramDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Category = p.Category,
                DifficultyLevel = p.DifficultyLevel,
                ImageUrl = p.ImageUrl
            });
        }
    }
}
