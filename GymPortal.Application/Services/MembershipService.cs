using GymPortal.Application.Interfaces.Repositories;
using GymPortal.Application.Interfaces.Services;
using GymPortal.Domain.Common;
using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;

namespace GymPortal.Application.Services
{
    public class MembershipService : IMembershipService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MembershipService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Membership>> CreateMembershipAsync(string userId, MembershipType type, string? planName = null)
        {
            var membershipRepo = _unitOfWork.Repository<Membership>();
            var existing = (await membershipRepo.FindAsync(m => m.UserId == userId && m.Status == MembershipStatus.Active)).FirstOrDefault();
            if (existing != null)
                return Result<Membership>.Failure("User already has an active membership.");

            var membership = new Membership
            {
                UserId = userId,
                PlanName = planName ?? (type == MembershipType.Monthly ? "Monthly" : "Yearly"),
                StartDate = DateTime.UtcNow,
                EndDate = type == MembershipType.Monthly ? DateTime.UtcNow.AddMonths(1) : DateTime.UtcNow.AddYears(1),
                Status = MembershipStatus.Active,
                Type = type
            };

            await membershipRepo.AddAsync(membership);
            await _unitOfWork.CompleteAsync();

            return Result<Membership>.Success(membership);
        }

        public async Task<Membership?> GetUserMembershipAsync(string userId)
        {
            var membershipRepo = _unitOfWork.Repository<Membership>();
            return (await membershipRepo.FindAsync(m => m.UserId == userId && m.Status == MembershipStatus.Active)).FirstOrDefault();
        }

        public async Task<Result> CancelMembershipAsync(string userId)
        {
            var membershipRepo = _unitOfWork.Repository<Membership>();
            var membership = (await membershipRepo.FindAsync(m => m.UserId == userId && m.Status == MembershipStatus.Active)).FirstOrDefault();
            if (membership == null)
                return Result.Failure("No active membership found.");

            membership.Status = MembershipStatus.Cancelled;
            membershipRepo.Update(membership);
            await _unitOfWork.CompleteAsync();
            return Result.Success();
        }
    }
}