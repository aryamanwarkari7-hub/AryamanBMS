using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    public class EsicChallanModel
    {
        [Key]
        public int EsicChallanId { get; set; }

        [Required]
        public int EsicSnapshotId { get; set; }

        [ForeignKey(nameof(EsicSnapshotId))]
        [ValidateNever]
        public virtual EsicMonthlySnapshotModel? Snapshot { get; set; }

        [StringLength(50)]
        public string? ChallanNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? PaymentDate { get; set; }

        [StringLength(100)]
        public string? BankName { get; set; }

        [StringLength(50)]
        public string? PaymentMode { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal InterestAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LateFee { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = FinancialConstants.StatutoryStatus.Pending;

        [StringLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }
    }
}