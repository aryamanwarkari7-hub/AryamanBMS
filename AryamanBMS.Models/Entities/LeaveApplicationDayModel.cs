using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class LeaveApplicationDayModel
    {
        public int Id { get; set; }

        public int LeaveApplicationId { get; set; }

        public LeaveApplicationModel? LeaveApplication { get; set; }

        public DateTime LeaveDate { get; set; }

        public decimal DayValue { get; set; } = 1m;

        public decimal PaidDays { get; set; }

        public decimal UnpaidDays { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Active";
        // Active, CancellationRequested, Cancelled

        [StringLength(20)]
        public string? HalfDaySession { get; set; }

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
