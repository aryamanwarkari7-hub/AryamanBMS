using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    public class ExpenseVoucherModel
    {
        [Key]
        public int ExpenseVoucherId { get; set; }

        [Required]
        [StringLength(50)]
        public string VoucherNumber { get; set; } = string.Empty;

        [Required]
        public DateTime VoucherDate { get; set; }

        [Required]
        [StringLength(20)]
        public string FinancialYear { get; set; } = string.Empty;

        [Required]
        public int ExpenseCategoryId { get; set; }

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal GSTRate { get; set; } = 0m;

        [Column(TypeName = "decimal(12,2)")]
        public decimal CGSTAmount { get; set; } = 0m;

        [Column(TypeName = "decimal(12,2)")]
        public decimal SGSTAmount { get; set; } = 0m;

        [Column(TypeName = "decimal(12,2)")]
        public decimal IGSTAmount { get; set; } = 0m;

        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalGSTAmount { get; set; } = 0m;

        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalAmount { get; set; } = 0m;

        [StringLength(50)]
        public string? VendorName { get; set; }

        [StringLength(20)]
        public string? VendorGSTIN { get; set; }

        [StringLength(20)]
        public string? InvoiceNumber { get; set; }

        [StringLength(20)]
        public string? Status { get; set; } = FinancialConstants.ExpenseVoucherStatus.Draft;

        public bool ITCEligible { get; set; } = true;

        [StringLength(500)]
        public string? Remarks { get; set; }

        public int CreatedByUserId { get; set; }

        public int? ApprovedByUserId { get; set; }

        public DateTime? ApprovedOn { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        [ForeignKey(nameof(ExpenseCategoryId))]
        public virtual ExpenseCategoryModel? Category { get; set; }
    }
}