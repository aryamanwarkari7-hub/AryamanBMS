
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    /// <summary>
    /// Stores GST payment challan details.
    /// </summary>
    public class GstChallanModel
    {
        [Key]
        public int ChallanId { get; set; }

        [Required]
        public int SnapshotId { get; set; }

        [ForeignKey(nameof(SnapshotId))]
        [ValidateNever]
        public virtual GstMonthlySnapshotModel? Snapshot { get; set; }

        [Required]
        [StringLength(50)]
        public string ChallanNumber { get; set; } = string.Empty;

        [StringLength(50)]
        public string? CPIN { get; set; }

        [StringLength(50)]
        public string? CIN { get; set; }

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

        [Column(TypeName = "decimal(18,2)")]
        public decimal Penalty { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [StringLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }
    }
}

