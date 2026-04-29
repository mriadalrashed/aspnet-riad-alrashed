using GymPortal.Application.DTOs;
using GymPortal.Application.Interfaces.Repositories;
using GymPortal.Application.Interfaces.Services;
using GymPortal.Domain.Common;
using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GymPortal.Application.Services
{
    public class AdminService : IAdminService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminService(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var membershipRepo = _unitOfWork.Repository<Membership>();
                var membership = (await membershipRepo.FindAsync(m => m.UserId == user.Id && m.Status == MembershipStatus.Active)).FirstOrDefault();
                userDtos.Add(new UserDto
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
                });
            }
            return userDtos;
        }

        public async Task<Result> ChangeUserRoleAsync(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Result.Failure("User not found.");
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            var result = await _userManager.AddToRoleAsync(user, newRole);
            return result.Succeeded ? Result.Success() : Result.Failure(string.Join(",", result.Errors.Select(e => e.Description)));
        }

        public async Task<Result> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Result.Failure("User not found.");
            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded ? Result.Success() : Result.Failure(string.Join(",", result.Errors.Select(e => e.Description)));
        }

        public async Task<IEnumerable<ClassSessionDto>> GetAllSessionsAsync()
        {
            var sessionRepo = _unitOfWork.Repository<ClassSession>();

            // Use GetQueryable() with Include to load Bookings
            var sessions = await sessionRepo.GetQueryable()
                .Include(s => s.Bookings)           // ← Eager load bookings
                .ToListAsync();

            var sessionsList = sessions.ToList();

            foreach (var session in sessionsList)
            {
                var confirmedCount = session.Bookings?.Count(b => b.Status == BookingStatus.Confirmed) ?? 0;
            }

            var programRepo = _unitOfWork.Repository<TrainingProgram>();
            var programs = await programRepo.GetAllAsync();
            var programDict = programs.ToDictionary(p => p.Id);

            var result = sessionsList.Select(s => new ClassSessionDto
            {
                Id = s.Id,
                TrainingProgramId = s.TrainingProgramId,
                ProgramTitle = programDict.TryGetValue(s.TrainingProgramId, out var p) ? p.Title : "",
                Category = p?.Category ?? "",
                DifficultyLevel = p?.DifficultyLevel ?? DifficultyLevel.Beginner,
                InstructorName = s.InstructorName,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Location = s.Location,
                MaxParticipants = s.MaxParticipants,
                AvailableSpots = s.MaxParticipants - (s.Bookings?.Count(b => b.Status == BookingStatus.Confirmed) ?? 0),
                IsActive = s.IsActive
            }).ToList();

            return result;
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
