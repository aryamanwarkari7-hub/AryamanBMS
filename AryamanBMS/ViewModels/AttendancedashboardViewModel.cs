namespace AryamanBMS.ViewModels
{
    public class AttendanceDashboardViewModel
    {
        public int Month { get; set; }

        public int Year { get; set; }

        public int TotalDays { get; set; }

        public List<EmployeeAttendanceViewModel> Employees
            = new();
    }
}