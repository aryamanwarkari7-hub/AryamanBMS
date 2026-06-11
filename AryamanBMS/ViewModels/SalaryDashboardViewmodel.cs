namespace AryamanBMS.ViewModels
{
    public class SalaryDashboardViewModel
    {
        public int TotalEmployees { get; set; }

        public int PaidCount { get; set; }

        public int PendingCount { get; set; }

        public decimal TotalGrossSalary { get; set; }

        public decimal TotalNetSalary { get; set; }
    }
}