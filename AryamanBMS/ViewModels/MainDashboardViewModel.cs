namespace AryamanBMS.ViewModels
{
    public class MainDashboardViewModel
    {
        public DateTime Today { get; set; } = DateTime.Today;
        public string FinancialYear { get; set; } = string.Empty;

        public int PendingApprovals { get; set; }
        public int OverdueItems { get; set; }
        public int FinanceAttention { get; set; }

        public HrDashboardSummary Hr { get; set; } = new();
        public ProjectDashboardSummary Projects { get; set; } = new();
        public FinanceDashboardSummary Finance { get; set; } = new();

        public List<DashboardListItem> RecentEmployees { get; set; } = new();
        public List<DashboardListItem> OverdueTasks { get; set; } = new();
        public List<DashboardListItem> HighRisks { get; set; } = new();
        public List<DashboardListItem> OverdueInvoices { get; set; } = new();
        public List<DashboardListItem> PendingExpenses { get; set; } = new();

        public List<DashboardBucket> ProjectStatusBuckets { get; set; } = new();
        public List<DashboardBucket> ReceivableBuckets { get; set; } = new();
        public List<DashboardBucket> DepartmentEmployeeBuckets { get; set; } = new();
        public List<DashboardBucket> TaskHealthBuckets { get; set; } = new();
        public List<DashboardBucket> MonthlyInvoiceCollectionBuckets { get; set; } = new();
        public List<DashboardBucket> FinancePressureBuckets { get; set; } = new();
    }

    public class HrDashboardSummary
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int InactiveEmployees { get; set; }
        public int OnLeaveToday { get; set; }
        public int PendingLeaveApplications { get; set; }
        public int PendingCompOffRequests { get; set; }
        public int UpcomingExits { get; set; }
    }

    public class ProjectDashboardSummary
    {
        public int TotalProjects { get; set; }
        public int ActiveProjects { get; set; }
        public int OpenTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int OpenRisks { get; set; }
        public int PendingActionItems { get; set; }
    }

    public class FinanceDashboardSummary
    {
        public decimal TotalInvoiced { get; set; }
        public decimal OutstandingReceivables { get; set; }
        public decimal OverdueReceivables { get; set; }
        public decimal CollectionThisMonth { get; set; }
        public int DraftInvoices { get; set; }
        public int PendingExpenseVouchers { get; set; }
        public decimal AdvanceReceiptBalance { get; set; }
        public decimal AssetBookValue { get; set; }
        public decimal NetGstPayable { get; set; }
    }

    public class DashboardListItem
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Meta { get; set; }
    public string? Badge { get; set; }

    public string? Controller { get; set; }
    public string? Action { get; set; }
    public int? RouteId { get; set; }
}

    public class DashboardBucket
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
        public decimal Percent { get; set; }
        public string CssClass { get; set; } = string.Empty;
    }
}
