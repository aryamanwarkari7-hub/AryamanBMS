namespace AryamanBMS.Models
{
    public class AttendanceSummaryCalculationInput
    {
        public int Month { get; set; }

        public int Year { get; set; }

        public List<AttendanceSummaryEmployeeInput> Employees { get; set; } = [];
    }

    public class AttendanceSummaryEmployeeInput
    {
        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; } = string.Empty;

        public string EmployeeName { get; set; } = string.Empty;

        public DateTime JoiningDate { get; set; }

        public DateTime? LastWorkingDate { get; set; }

        public bool IsActive { get; set; }

        public List<AttendanceSummaryAttendanceInput> Attendances { get; set; } = [];

        public List<AttendanceSummaryLeaveInput> ApprovedLeaves { get; set; } = [];
    }

    public class AttendanceSummaryAttendanceInput
    {
        public int AttendanceRecordId { get; set; }

        public DateTime AttendanceDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public decimal AttendanceValue { get; set; }
    }

    public class AttendanceSummaryLeaveInput
    {
        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public decimal NumberOfDays { get; set; }

        public decimal PaidDays { get; set; }

        public bool IsHalfDay { get; set; }
    }
}