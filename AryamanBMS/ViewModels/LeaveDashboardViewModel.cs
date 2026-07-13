namespace AryamanBMS.ViewModels
{
    public class LeaveDashboardViewModel
    {
        public DateTime Today { get; set; } = DateTime.Today;
        public string FinancialYear { get; set; } = string.Empty;

        public LeaveDashboardSummary Summary { get; set; } = new();
        public CompOffDashboardSummary CompOff { get; set; } = new();

        public List<LeaveDashboardBucket> StatusBuckets { get; set; } = new();
        public List<LeaveDashboardBucket> LeaveTypeBuckets { get; set; } = new();

        public List<LeaveDashboardListItem> PendingApplications { get; set; } = new();
        public List<LeaveDashboardListItem> OnLeaveToday { get; set; } = new();
        public List<LeaveDashboardListItem> UpcomingLeaves { get; set; } = new();
        public List<LeaveDashboardListItem> CancellationRequests { get; set; } = new();
        public List<LeaveDashboardListItem> ExpiringCompOffCredits { get; set; } = new();
    }

    public class LeaveDashboardSummary
    {
        public int TotalThisMonth { get; set; }
        public int PendingApplications { get; set; }
        public int ApprovedThisMonth { get; set; }
        public int RejectedThisMonth { get; set; }
        public int CancelledThisMonth { get; set; }
        public int CancellationRequests { get; set; }
        public int OnLeaveToday { get; set; }
        public int UpcomingLeaves { get; set; }
    }

    public class CompOffDashboardSummary
    {
        public int PendingRequests { get; set; }
        public int ApprovedAvailableCredits { get; set; }
        public int ExpiringSoon { get; set; }
        public decimal UsedThisMonth { get; set; }
    }

    public class LeaveDashboardBucket
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Days { get; set; }
        public decimal Percent { get; set; }
        public string CssClass { get; set; } = "bucket-info";
    }

    public class LeaveDashboardListItem
    {
        public int LeaveApplicationId { get; set; }
        public int? EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? Meta { get; set; }
        public string? Badge { get; set; }
    }
}