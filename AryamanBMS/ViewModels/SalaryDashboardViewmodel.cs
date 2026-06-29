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

        public decimal TotalGrossSalary { get; set; }

        public decimal TotalNetSalary { get; set; }

        public decimal TotalDeductions { get; set; }

        public decimal PayrollCompletionPercentage { get; set; }

        public decimal TotalBasic { get; set; }

        public decimal TotalHRA { get; set; }

        public decimal TotalDA { get; set; }

        public decimal TotalOtherAllowances { get; set; }

        public decimal TotalPF { get; set; }

        public decimal TotalESIC { get; set; }

        public decimal TotalTDS { get; set; }

        public decimal TotalOtherDeductions { get; set; }

        public List<MonthlySalarySummaryViewModel>
            MonthlySummaries
        { get; set; } = new();

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