namespace AryamanBMS.Models

{
    public class LeaveBalanceModel
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public EmployeeModel Employee { get; set; } = null!;

        public int LeaveTypeId { get; set; }

        public LeaveTypeModel LeaveType { get; set; } = null!;

        public int LeaveYear { get; set; }

        public decimal CurrentYearAllocation { get; set; }

        public decimal CarryForwardDays { get; set; }

        public decimal AllocatedDays { get; set; }

        public decimal UsedDays { get; set; }

        public decimal BalanceDays { get; set; }
    }
}