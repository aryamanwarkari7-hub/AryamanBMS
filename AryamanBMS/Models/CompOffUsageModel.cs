using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    [Table("tablecompoffusage")]
    public class CompOffUsageModel
    {
        public int Id { get; set; }

        public int CompOffCreditId { get; set; }

        public int LeaveApplicationId { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal UsedDays { get; set; }

        public DateTime UsedOn { get; set; } = DateTime.Now;

        public bool IsReversed { get; set; }

        public DateTime? ReversedOn { get; set; }

        public string? ReversedBy { get; set; }

        public CompOffCreditModel? CompOffCredit { get; set; }

        public LeaveApplicationModel? LeaveApplication { get; set; }
    }
}