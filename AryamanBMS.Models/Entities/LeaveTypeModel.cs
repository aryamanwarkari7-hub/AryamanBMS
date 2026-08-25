using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class LeaveTypeModel
    {
        public int Id { get; set; }

        public string LeaveCode { get; set; } = string.Empty;

        public string LeaveName { get; set; } = string.Empty;

        public int DaysPerYear { get; set; }

        [Display(Name = "Carry Forward")]
        public bool IsCarryForward { get; set; }

        [Display(Name = "Maximum Carry Forward Days")]
        [Range(0, 365)]
        public int? MaximumCarryForwardDays { get; set; }



        public bool IsPaidLeave { get; set; }

        public bool IsActive { get; set; } = true;
    }
}