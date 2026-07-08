using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    public class FullAndFinalSettlementModel
    {
        [Key]
        public int FullAndFinalSettlementId { get; set; }

        public int EmployeeId { get; set; }

        public DateTime LastWorkingDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalaryUpToLastWorkingDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LeaveEncashment { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BonusOrIncentive { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NoticePay { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalaryAdvanceRecovery { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LoanRecovery { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AssetRecovery { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OtherPayableAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OtherRecoverableAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal FinalNetPayable { get; set; }

        [StringLength(30)]
        public string ApprovalStatus { get; set; } = "Draft";

        [StringLength(30)]
        public string PaymentStatus { get; set; } = "Pending";

        [StringLength(450)]
        public string? ApprovedByUserId { get; set; }

        public DateTime? ApprovedOn { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        public EmployeeModel? Employee { get; set; }
    }
}
