namespace AryamanBMS.Models
{
    public class AttendanceSummaryResult
    {
        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; } = string.Empty;

        public string EmployeeName { get; set; } = string.Empty;

        public int Month { get; set; }

        public int Year { get; set; }

        public decimal PresentCount { get; set; }

        public decimal AbsentCount { get; set; }

        public decimal LeaveCount { get; set; }

        public decimal PaidLeaveCount { get; set; }

        public decimal UnpaidLeaveCount { get; set; }

        public decimal HolidayCount { get; set; }

        public decimal WeekOffCount { get; set; }

        public decimal OnDutyCount { get; set; }

        public decimal TotalDays { get; set; }

        public decimal PayDays { get; set; }

        public decimal MarkedAbsentCount { get; set; }

        public decimal MissingDays { get; set; }

        public decimal AttendancePercentage { get; set; }
    }
}