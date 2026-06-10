namespace AryamanBMS.ViewModels
{
    public class AttendanceSummaryViewModel
    {
        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; }
            = string.Empty;

        public string EmployeeName { get; set; }
            = string.Empty;

        public int PresentCount { get; set; }

        public int AbsentCount { get; set; }

        public int LeaveCount { get; set; }

        public int HolidayCount { get; set; }

        public int WeekOffCount { get; set; }

        public int OnDutyCount { get; set; }

        public int TotalDays { get; set; }

        public decimal AttendancePercentage { get; set; }
    }
}