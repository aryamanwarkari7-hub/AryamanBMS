using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class OffDayWorkRequestModel
    {
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public DateTime WorkDate { get; set; }

        [Required]
        [StringLength(20)]
        public string OffDayType { get; set; } = string.Empty;
        // WeeklyOff / Holiday

        [StringLength(500)]
        public string? Reason { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";
        // Pending / Approved / Rejected

        [StringLength(450)]
        public string? RequestedByUserId { get; set; }

        public DateTime RequestedOn { get; set; } = DateTime.Now;

        [StringLength(450)]
        public string? ApprovedByUserId { get; set; }

        public DateTime? ApprovedOn { get; set; }

        [StringLength(450)]
        public string? RejectedByUserId { get; set; }

        public DateTime? RejectedOn { get; set; }

        [StringLength(500)]
        public string? ApprovalRemarks { get; set; }

        public EmployeeModel? Employee { get; set; }
    }
}