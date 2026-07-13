namespace AryamanBMS.ViewModels
{
    public class SalaryDashboardViewModel
    {
        public string ViewType { get; set; } = "Monthly";

        public int Month { get; set; }

        public int Year { get; set; }

        public int TotalEmployees { get; set; }

        public int PaidCount { get; set; }

        public int PendingCount { get; set; }

        public int FinalizedCount { get; set; }

        public int DraftCount { get; set; }

        public int VerifiedCount { get; set; }

        public int PayslipReleasedCount { get; set; }

        public int PayslipPendingCount { get; set; }

        public decimal TotalGrossSalary { get; set; }

        public decimal TotalNetSalary { get; set; }

        public decimal TotalDeductions { get; set; }

        public decimal PayrollCompletionPercentage { get; set; }

        public decimal PayslipReleasePercentage { get; set; }

        public decimal FinalizationPercentage { get; set; }

        public decimal TotalBasic { get; set; }

        public decimal TotalHRA { get; set; }

        public decimal TotalDA { get; set; }

        public decimal TotalOtherAllowances { get; set; }

        public decimal TotalPF { get; set; }

        public decimal TotalESIC { get; set; }

        public decimal TotalTDS { get; set; }

        public decimal TotalOtherDeductions { get; set; }

        public List<SalaryDashboardBucket> PaymentBuckets { get; set; } = new();

        public List<SalaryDashboardBucket> PayrollStatusBuckets { get; set; } = new();

        public List<SalaryDashboardBucket> PayComponentBuckets { get; set; } = new();

        public List<SalaryDashboardBucket> DeductionBuckets { get; set; } = new();

        public List<SalaryDashboardListItem> PendingPayments { get; set; } = new();

        public List<SalaryDashboardListItem> PendingFinalization { get; set; } = new();

        public List<SalaryDashboardListItem> PendingPayslips { get; set; } = new();

        public List<MonthlySalarySummaryViewModel> MonthlySummaries { get; set; } = new();

        public class MonthlySalarySummaryViewModel
        {
            public int Month { get; set; }

            public string MonthName { get; set; } = string.Empty;

            public int EmployeeCount { get; set; }

            public int PaidCount { get; set; }

            public int PendingCount { get; set; }

            public decimal GrossSalary { get; set; }

            public decimal NetSalary { get; set; }
        }
    }

    public class SalaryDashboardBucket
    {
        public string Label { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public int Count { get; set; }

        public decimal Percent { get; set; }

        public string CssClass { get; set; } = "bucket-info";
    }

    public class SalaryDashboardListItem
    {
        public int SalaryRecordId { get; set; }

        public int EmployeeId { get; set; }

        public string EmployeeName { get; set; } = string.Empty;

        public string EmployeeCode { get; set; } = string.Empty;

        public string? Meta { get; set; }

        public string? Badge { get; set; }

        public decimal Amount { get; set; }
    }

    public class MonthlySalarySummaryViewModel
    {
        public int Month { get; set; }

        public string MonthName { get; set; } = string.Empty;

        public int EmployeeCount { get; set; }

        public int PaidCount { get; set; }

        public int PendingCount { get; set; }

        public decimal GrossSalary { get; set; }

        public decimal NetSalary { get; set; }
    }
}