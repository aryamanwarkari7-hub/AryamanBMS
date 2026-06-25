using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class LeaveApplicationModel
    {
        public int Id { get; set; }

        public string ApplicationNumber { get; set; }
            = string.Empty;

        public int EmployeeId { get; set; }

        public EmployeeModel? Employee { get; set; }

        public int LeaveTypeId { get; set; }

        public LeaveTypeModel? LeaveType { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public int NumberOfDays { get; set; }

        public string Reason { get; set; } = string.Empty;

        public DateTime AppliedOn { get; set; }

        public string Status { get; set; } = "Pending";

        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedOn { get; set; }

        public string? ApprovalRemarks { get; set; }

        [StringLength(20)]
        public string? CancellationStatus { get; set; }

        [StringLength(500)]
        public string? CancellationReason { get; set; }

        public DateTime? CancellationRequestedOn { get; set; }

        [StringLength(255)]
        public string? CancellationRequestedBy { get; set; }

        public DateTime? CancellationReviewedOn { get; set; }

        [StringLength(255)]
        public string? CancellationReviewedBy { get; set; }

        [StringLength(500)]
        public string? CancellationRemarks { get; set; }
    }
}