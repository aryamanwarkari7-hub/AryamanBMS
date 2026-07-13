namespace AryamanBMS.ViewModels
{
    public class EmployeeMyDashboardViewModel
    {
        public int EmployeeId { get; set; }

        public string EmployeeName { get; set; } = string.Empty;

        public string EmployeeCode { get; set; } = string.Empty;

        public string? DepartmentName { get; set; }

        public string? DesignationName { get; set; }

        public EmployeeTodayAttendanceCard Attendance { get; set; } = new();

        public EmployeeLeaveCard Leave { get; set; } = new();

        public EmployeeSalaryCard Salary { get; set; } = new();

        public List<EmployeeDashboardTaskItem> AssignedTasks { get; set; } = new();

        public List<EmployeeDashboardMeetingItem> UpcomingMeetings { get; set; } = new();

        public List<EmployeeDashboardProjectItem> MyProjects { get; set; } = new();
    }

    public class EmployeeTodayAttendanceCard
    {
        public string Status { get; set; } = "Not Marked";

        public DateTime? CheckInTime { get; set; }

        public DateTime? CheckOutTime { get; set; }

        public double WorkingHours { get; set; }

        public bool IsMarked { get; set; }
    }

    public class EmployeeLeaveCard
    {
        public int PendingApplications { get; set; }

        public int ApprovedThisMonth { get; set; }

        public decimal AvailableBalance { get; set; }

        public int PendingCompOff { get; set; }

        public decimal AvailableCompOff { get; set; }
    }

    public class EmployeeSalaryCard
    {
        public int? Month { get; set; }

        public int? Year { get; set; }

        public decimal NetSalary { get; set; }

        public string PaymentStatus { get; set; } = "-";

        public bool IsPayslipReleased { get; set; }

        public int? SalaryRecordId { get; set; }
    }

    public class EmployeeDashboardTaskItem
    {
        public int TaskId { get; set; }

        public int ProjectId { get; set; }

        public string TaskTitle { get; set; } = string.Empty;

        public string? ProjectName { get; set; }

        public string Status { get; set; } = string.Empty;

        public string Priority { get; set; } = string.Empty;

        public DateTime? DueDate { get; set; }

        public int ProgressPercent { get; set; }

        public bool IsOverdue { get; set; }
    }

    public class EmployeeDashboardMeetingItem
    {
        public int MeetingId { get; set; }

        public int ProjectId { get; set; }

        public string MeetingTitle { get; set; } = string.Empty;

        public string? ProjectName { get; set; }

        public DateTime MeetingDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public string MeetingStatus { get; set; } = string.Empty;
    }

    public class EmployeeDashboardProjectItem
    {
        public int ProjectId { get; set; }

        public string ProjectCode { get; set; } = string.Empty;

        public string ProjectName { get; set; } = string.Empty;

        public string? RoleInProject { get; set; }

        public string Status { get; set; } = string.Empty;

        public int OpenTaskCount { get; set; }
    }
}