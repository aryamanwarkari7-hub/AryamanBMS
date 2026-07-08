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

        [Column(TypeName = "decimal(18,2)")]
        public decimal OutputCGSTBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OutputSGSTBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OutputIGSTBalance { get; set; }

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

        [Column(TypeName = "decimal(18,2)")]
        public decimal InputCGSTBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal InputSGSTBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal InputIGSTBalance { get; set; }

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

        [Column(TypeName = "decimal(18,2)")]
        public decimal PreviousITCCarryForward { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ITCUtilizedForIGST { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ITCUtilizedForCGST { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ITCUtilizedForSGST { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CashPayableIGST { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CashPayableCGST { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CashPayableSGST { get; set; }

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

        [StringLength(450)]
        public string? VerifiedByUserId { get; set; }

        public DateTime? FiledOn { get; set; }

        [StringLength(450)]
        public string? FiledByUserId { get; set; }

        [StringLength(450)]
        public string? ReopenedByUserId { get; set; }

        public DateTime? ReopenedOn { get; set; }

        [StringLength(500)]
        public string? ReopenReason { get; set; }

        [StringLength(450)]
        public string? LockedByUserId { get; set; }

        public DateTime? LockedOn { get; set; }

        public bool IsFiledPeriodLocked { get; set; }

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
