namespace AryamanBMS.Models
{
    public class AttendanceModel
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public DateTime AttendanceDate { get; set; }

        public string Status { get; set; } = "P";

        public DateTime? CheckInTime { get; set; }

        public DateTime? CheckOutTime { get; set; }

        public string? LocationType { get; set; }

        public string? Remarks { get; set; }

        public DateTime CreatedOn { get; set; }

        public EmployeeModel? Employee { get; set; }
    }
}