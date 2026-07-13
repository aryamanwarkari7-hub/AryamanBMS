namespace AryamanBMS.ViewModels
{
    public class EmployeeDashboardViewModel
    {
        public DateTime Today { get; set; } = DateTime.Today;
        public string FinancialYear { get; set; } = string.Empty;

        public EmployeeHeadcountSummary Headcount { get; set; } = new();
        public EmployeeComplianceSummary Compliance { get; set; } = new();

        public List<EmployeeDashboardBucket> DepartmentBuckets { get; set; } = new();
        public List<EmployeeDashboardBucket> EmploymentTypeBuckets { get; set; } = new();
        public List<EmployeeDashboardBucket> GenderBuckets { get; set; } = new();

        public List<EmployeeDashboardListItem> RecentEmployees { get; set; } = new();
        public List<EmployeeDashboardListItem> UpcomingExits { get; set; } = new();
        public List<EmployeeDashboardListItem> MissingComplianceEmployees { get; set; } = new();
        public List<EmployeeDashboardListItem> EmployeesWithoutDocuments { get; set; } = new();
    }

    public class EmployeeHeadcountSummary
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int InactiveEmployees { get; set; }
        public int JoinedThisMonth { get; set; }
        public int JoinedThisFinancialYear { get; set; }
        public int UpcomingExits { get; set; }
    }

    public class EmployeeComplianceSummary
    {
        public int MissingPan { get; set; }
        public int MissingAadhaar { get; set; }
        public int MissingUan { get; set; }
        public int MissingEsic { get; set; }
        public int MissingOfficialEmail { get; set; }
        public int MissingMobileNumber { get; set; }
        public int EmployeesWithoutDocuments { get; set; }
    }

    public class EmployeeDashboardBucket
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percent { get; set; }
        public string CssClass { get; set; } = "bucket-info";
    }

    public class EmployeeDashboardListItem
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? Meta { get; set; }
        public string? Badge { get; set; }
    }
}