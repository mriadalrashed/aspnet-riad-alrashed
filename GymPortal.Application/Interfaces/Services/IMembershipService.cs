using GymPortal.Domain.Common;
using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;


namespace GymPortal.Application.Interfaces.Services
{
    public interface IMembershipService
    {
        Task<Result<Membership>> CreateMembershipAsync(string userId, MembershipType type, string? planName = null);
        Task<Membership?> GetUserMembershipAsync(string userId);
        Task<Result> CancelMembershipAsync(string userId);
    }
}
