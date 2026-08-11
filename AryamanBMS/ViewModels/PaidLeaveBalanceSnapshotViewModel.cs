namespace AryamanBMS.ViewModels
{
    public class PaidLeaveBalanceSnapshotViewModel
    {
        public DateTime FinancialYearStart { get; set; }

        public DateTime FinancialYearEnd { get; set; }

        public decimal AnnualEntitlement { get; set; }

        public decimal MonthlyAccrual { get; set; }

        public decimal ProratedEntitlement { get; set; }

        public decimal PaidLeaveUsed { get; set; }

        public decimal PaidLeaveBalance { get; set; }

        public decimal RequestedDays { get; set; }

        public decimal PaidDaysForRequest { get; set; }

        public decimal UnpaidDaysForRequest { get; set; }

        public string FinancialYearLabel =>
            $"{FinancialYearStart:dd-MMM-yyyy} to {FinancialYearEnd:dd-MMM-yyyy}";
    }
}