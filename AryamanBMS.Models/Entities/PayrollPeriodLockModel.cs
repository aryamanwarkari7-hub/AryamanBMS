using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class PayrollPeriodLockModel
    {
        [Key]
        public int PayrollPeriodLockId { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        public bool IsLocked { get; set; }

        [StringLength(450)]
        public string? LockedByUserId { get; set; }

        public DateTime? LockedOn { get; set; }

        [StringLength(450)]
        public string? ReopenedByUserId { get; set; }

        public DateTime? ReopenedOn { get; set; }

        [StringLength(500)]
        public string? ReopenReason { get; set; }
    }
}
