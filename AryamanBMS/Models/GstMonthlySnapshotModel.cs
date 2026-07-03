using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    /// <summary>
    /// Stores the monthly GST snapshot generated from
    /// Sales Invoices and Expense Vouchers.
    /// One record per Month + Year.
    /// Once Filed, this record should not be recalculated.
    /// </summary>
    public class GstMonthlySnapshotModel
    {
        [Key]
        public int SnapshotId { get; set; }

        [Required]
        [Range(1, 12)]
        public int Month { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        [StringLength(10)]
        public string FinancialYear { get; set; } = string.Empty;

        // -----------------------------
        // Sales Summary
        // -----------------------------

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalesTaxableAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalesCGST { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalesSGST { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalesIGST { get; set; }

        // -----------------------------
        // Purchase Summary
        // -----------------------------

        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchaseTaxableAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchaseCGST { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchaseSGST { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchaseIGST { get; set; }

        // -----------------------------
        // GST Totals
        // -----------------------------

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalOutputGST { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalInputGST { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetGSTPayable { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal InputCreditCarryForward { get; set; }

        // -----------------------------
        // Statistics
        // -----------------------------

        public int InvoiceCount { get; set; }

        public int ExpenseVoucherCount { get; set; }

        // -----------------------------
        // Workflow
        // -----------------------------

        /// <summary>
        /// Draft
        /// Calculated
        /// Verified
        /// Filed
        /// Locked
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Draft";

        public DateTime GeneratedOn { get; set; } = DateTime.Now;

        public DateTime? VerifiedOn { get; set; }

        public DateTime? FiledOn { get; set; }

        public string? Remarks { get; set; }

        // -----------------------------
        // Navigation
        // -----------------------------

        [ValidateNever]
        public virtual ICollection<GstReturnModel> Returns { get; set; }
            = new List<GstReturnModel>();

        [ValidateNever]
        public virtual ICollection<GstChallanModel> Challans { get; set; }
            = new List<GstChallanModel>();

        [ValidateNever]
        public virtual ICollection<GstDocumentModel> Documents { get; set; }
            = new List<GstDocumentModel>();

        [ValidateNever]
        public virtual ICollection<GstItcRecordModel> ItcRecords { get; set; }
            = new List<GstItcRecordModel>();
    }
}
