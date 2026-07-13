namespace AryamanBMS.ViewModels
{
    public class AttendanceDashboardViewModel
    {
        public int Month { get; set; }

        public int Year { get; set; }

        public int TotalDays { get; set; }

        public DateTime SummaryDate { get; set; }

        public List<EmployeeAttendanceViewModel> Employees { get; set; } = new();

        public int TotalActiveEmployees { get; set; }

        public int PresentToday { get; set; }

        public int AbsentToday { get; set; }

        public int OnLeaveToday { get; set; }

        public int OnDutyToday { get; set; }

        public int NotMarkedToday { get; set; }

        public int MissingCheckoutCount { get; set; }

        public decimal AttendancePercentage { get; set; }

        public List<AttendanceDashboardBucket> StatusBuckets { get; set; } = new();

        public List<AttendanceDashboardBucket> MonthlyStatusBuckets { get; set; } = new();

        public List<AttendanceDashboardDayTrend> DayTrends { get; set; } = new();

        public List<AttendanceDashboardListItem> NotMarkedEmployees { get; set; } = new();

        public List<AttendanceDashboardListItem> OnLeaveEmployees { get; set; } = new();

        public List<AttendanceDashboardListItem> MissingCheckoutEmployees { get; set; } = new();
    }

    public class AttendanceDashboardBucket
    {
        public string Label { get; set; } = string.Empty;

        public int Count { get; set; }

        public decimal Percent { get; set; }

        public string CssClass { get; set; } = "bucket-info";
    }

    public class AttendanceDashboardDayTrend
    {
        public int Day { get; set; }

        public int PresentCount { get; set; }

        public int LeaveCount { get; set; }

        public int AbsentCount { get; set; }

        public int NotMarkedCount { get; set; }

        public decimal PresentPercent { get; set; }
    }

    public class AttendanceDashboardListItem
    {
        public int EmployeeId { get; set; }

        public string EmployeeName { get; set; } = string.Empty;

        public string EmployeeCode { get; set; } = string.Empty;

        public string? Meta { get; set; }

        public string? Badge { get; set; }
    }
}