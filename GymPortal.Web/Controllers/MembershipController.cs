using GymPortal.Application.Interfaces.Services;
using GymPortal.Domain.Entities;
using GymPortal.Domain.Enums;
using GymPortal.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GymPortal.Web.Controllers
{
    [Authorize]
    public class MembershipController : Controller
    {
        private readonly IMembershipService _membershipService;
        private readonly UserManager<ApplicationUser> _userManager;

        public MembershipController(IMembershipService membershipService, UserManager<ApplicationUser> userManager)
        {
            _membershipService = membershipService;
            _userManager = userManager;
        }

        // GET: /Membership
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToAction("SignIn", "Account");
            }

            var currentMembership = await _membershipService.GetUserMembershipAsync(userId);

            var plans = new List<MembershipPlanViewModel>
            {
                new()
                {
                    Id = 1,
                    Name = "Basic",
                    Description = "Access to gym floor and basic equipment",
                    Price = 29.99m,
                    DurationMonths = 1,
                    Features = new List<string>
                    {
                        "Gym access",
                        "Locker room",
                        "Showers",
                        "Basic equipment"
                    },
                    IsPopular = false
                },
                new()
                {
                    Id = 2,
                    Name = "Premium",
                    Description = "Gym + unlimited group classes",
                    Price = 49.99m,
                    DurationMonths = 1,
                    Features = new List<string>
                    {
                        "All Basic features",
                        "Unlimited group classes",
                        "Sauna access",
                        "Nutrition guide",
                        "Monthly fitness assessment"
                    },
                    IsPopular = true
                },
                new()
                {
                    Id = 3,
                    Name = "Elite",
                    Description = "Full access + personal training",
                    Price = 79.99m,
                    DurationMonths = 12,
                    Features = new List<string>
                    {
                        "All Premium features",
                        "2 PT sessions/month",
                        "Nutrition consultation",
                        "Recovery zone access",
                        "Guest passes (2/month)",
                        "Priority booking"
                    },
                    IsPopular = false
                }
            };

            var viewModel = new MembershipViewModel
            {
                Plans = plans,
                CurrentUserPlan = currentMembership != null ? new MembershipPlanViewModel
                {
                    Id = currentMembership.Id,
                    Name = currentMembership.PlanName ?? "Unknown",
                    Price = 0,
                    DurationMonths = currentMembership.Type == Domain.Enums.MembershipType.Yearly ? 12 : 1,
                    Features = new List<string>()
                } : null
            };

            return View(viewModel);
        }

        // POST: /Membership/Subscribe/{planId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Subscribe(int planId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToAction("SignIn", "Account");
            }

            var type = planId switch
            {
                1 => MembershipType.Monthly,
                2 => MembershipType.Monthly,
                3 => MembershipType.Yearly,
                _ => MembershipType.Monthly
            };

            var planName = planId switch
            {
                1 => "Basic",
                2 => "Premium",
                3 => "Elite",
                _ => "Custom"
            };

            var result = await _membershipService.CreateMembershipAsync(userId, type, planName);

            if (result.IsSuccess)
                TempData["Success"] = "Membership activated successfully!";
            else
                TempData["Error"] = result.Error;

            return RedirectToAction(nameof(Index));
        }

        // POST: /Membership/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToAction("SignIn", "Account");
            }

            var result = await _membershipService.CancelMembershipAsync(userId);

            if (result.IsSuccess)
                TempData["Success"] = "Membership cancelled successfully.";
            else
                TempData["Error"] = result.Error;

            return RedirectToAction(nameof(Index));
        }
    }
}
