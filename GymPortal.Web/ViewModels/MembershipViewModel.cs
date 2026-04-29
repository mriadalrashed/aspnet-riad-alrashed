namespace GymPortal.Web.ViewModels
{
    public class MembershipViewModel
    {
        public List<MembershipPlanViewModel> Plans { get; set; } = new();
        public MembershipPlanViewModel? CurrentUserPlan { get; set; }
    }
    public class MembershipPlanViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Description { get; set; } = string.Empty;
        public int DurationMonths { get; set; } = 1;
        public List<string> Features { get; set; } = new();
        public bool IsPopular { get; set; }
    }
}
