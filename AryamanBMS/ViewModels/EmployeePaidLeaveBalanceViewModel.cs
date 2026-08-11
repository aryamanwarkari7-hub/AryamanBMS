namespace AryamanBMS.ViewModels
{
    public class EmployeePaidLeaveBalanceViewModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime JoiningDate { get; set; }

        public DateTime FinancialYearStart { get; set; }
        public DateTime FinancialYearEnd { get; set; }

        public decimal AnnualEntitlement { get; set; }
        public decimal MonthlyAccrual { get; set; }
        public decimal ProratedEntitlement { get; set; }
        public decimal PaidUsed { get; set; }
        public decimal PaidBalance { get; set; }

        public BirthdayLeaveBalanceViewModel BirthdayLeave { get; set; } = new();

        public string FinancialYearLabel =>
            $"{FinancialYearStart:dd-MMM-yyyy} to {FinancialYearEnd:dd-MMM-yyyy}";
    }
}