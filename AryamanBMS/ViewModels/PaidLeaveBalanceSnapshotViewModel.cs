namespace AryamanBMS.ViewModels
{
    public class PaidLeaveBalanceSnapshotViewModel
    {
        public DateTime FinancialYearStart { get; set; }

        public DateTime FinancialYearEnd { get; set; }

        public decimal AnnualEntitlement { get; set; }

        public decimal MonthlyAccrual { get; set; }

        public decimal ProratedEntitlement { get; set; }

        public decimal CarryForwardDays { get; set; }

        public decimal PaidLeaveUsed { get; set; }

        public decimal PendingPaidLeaveReserved { get; set; }

        public decimal PaidLeaveBalance { get; set; }

        public List<PaidLeaveMonthlyCreditViewModel> MonthlyCredits { get; set; } = new();

        public decimal RequestedDays { get; set; }

        public decimal PaidDaysForRequest { get; set; }

        public decimal UnpaidDaysForRequest { get; set; }

        public BirthdayLeaveBalanceViewModel BirthdayLeave { get; set; } = new();

        public string FinancialYearLabel =>
            $"{FinancialYearStart:dd-MMM-yyyy} to {FinancialYearEnd:dd-MMM-yyyy}";
    }

    public class PaidLeaveMonthlyCreditViewModel
    {
        public DateTime MonthStart { get; set; }

        public string MonthLabel => MonthStart.ToString("MMM yyyy");

        public decimal Credit { get; set; }

        public string Remarks { get; set; } = string.Empty;
    }
}
