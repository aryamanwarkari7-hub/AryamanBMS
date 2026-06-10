namespace AryamanBMS.Models
{
    public class LeaveTypeModel
    {
        public int Id { get; set; }

        public string LeaveCode { get; set; } = string.Empty;

        public string LeaveName { get; set; } = string.Empty;

        public int DaysPerYear { get; set; }

        public bool IsCarryForward { get; set; }

        public bool IsPaidLeave { get; set; }

        public bool IsActive { get; set; } = true;
    }
}