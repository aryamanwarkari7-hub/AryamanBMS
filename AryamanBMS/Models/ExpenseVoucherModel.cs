using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

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

        public int? VendorId { get; set; }

        public int? ProjectId { get; set; }

        public int? DepartmentId { get; set; }

        public int? CostCentreId { get; set; }

        [StringLength(30)]
        public string ExpenseClassification { get; set; } = "General";

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal TaxableAmount { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal GSTRate { get; set; } = 0m;

        public bool IsInterState { get; set; }

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

        public DateTime? VendorInvoiceDate { get; set; }

        [StringLength(20)]
        public string? Status { get; set; } = FinancialConstants.ExpenseVoucherStatus.Draft;

        [StringLength(20)]
        public string ApprovalStatus { get; set; } = FinancialConstants.ExpenseVoucherStatus.Draft;

        [StringLength(30)]
        public string PaymentStatus { get; set; } = FinancialConstants.PaymentStatus.Unpaid;

        [Column(TypeName = "decimal(12,2)")]
        public decimal PaidAmount { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal BalanceAmount { get; set; }

        public bool ITCEligible { get; set; } = true;

        [StringLength(50)]
        public string ITCStatus { get; set; } = "Pending Verification";

        [StringLength(50)]
        public string Gstr2BMatchStatus { get; set; } = "Pending";

        public DateTime? Gstr2BMatchedOn { get; set; }

        [StringLength(450)]
        public string? Gstr2BMatchedByUserId { get; set; }

        [StringLength(500)]
        public string? Gstr2BMismatchReason { get; set; }

        public int? ITCClaimMonth { get; set; }

        public int? ITCClaimYear { get; set; }

        [StringLength(2)]
        public string? CompanyStateCode { get; set; }

        [StringLength(2)]
        public string? VendorStateCode { get; set; }

        [StringLength(2)]
        public string? PlaceOfSupplyStateCode { get; set; }

        public bool IsGstStateOverride { get; set; }

        [StringLength(500)]
        public string? GstStateOverrideReason { get; set; }

        public bool IsEmployeeReimbursement { get; set; }

        public int? ReimbursementEmployeeId { get; set; }

        [StringLength(30)]
        public string ReimbursementStatus { get; set; } = "Not Applicable";

        [StringLength(500)]
        public string? BusinessPurpose { get; set; }

        [StringLength(150)]
        public string? BeneficiaryName { get; set; }

        [StringLength(100)]
        public string? SupportingReference { get; set; }

        [StringLength(50)]
        public string? GLAccountCode { get; set; }

        [StringLength(50)]
        public string? PayableGLAccountCode { get; set; }

        [StringLength(50)]
        public string? InputGSTGLAccountCode { get; set; }

        [StringLength(50)]
        public string? AccountingPeriod { get; set; }

        [StringLength(100)]
        public string? PostingReference { get; set; }

        public int? JournalEntryId { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        [StringLength(450)]
        public string CreatedByUserId { get; set; } = string.Empty;

        [StringLength(450)]
        public string? ApprovedByUserId { get; set; }

        public DateTime? ApprovedOn { get; set; }

        [StringLength(450)]
        public string? SubmittedByUserId { get; set; }

        public DateTime? SubmittedOn { get; set; }

        [StringLength(450)]
        public string? PostedByUserId { get; set; }

        public DateTime? PostedOn { get; set; }

        [StringLength(450)]
        public string? ReopenedByUserId { get; set; }

        public DateTime? ReopenedOn { get; set; }

        [StringLength(500)]
        public string? ReopenReason { get; set; }

        public bool IsReversed { get; set; }

        [StringLength(450)]
        public string? ReversedByUserId { get; set; }

        public DateTime? ReversedOn { get; set; }

        [StringLength(500)]
        public string? ReversalReason { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        [ForeignKey(nameof(ExpenseCategoryId))]
        public virtual ExpenseCategoryModel? Category { get; set; }

        [ForeignKey(nameof(VendorId))]
        [ValidateNever]
        public virtual VendorModel? Vendor { get; set; }

        [ForeignKey(nameof(ProjectId))]
        [ValidateNever]
        public virtual ProjectModel? Project { get; set; }

        [ForeignKey(nameof(DepartmentId))]
        [ValidateNever]
        public virtual DepartmentModel? Department { get; set; }

        // Reject
        [StringLength(500)]
        public string? RejectionReason { get; set; }

        [StringLength(450)]
        public string? RejectedByUserId { get; set; }

        public DateTime? RejectedOn { get; set; }

        [ValidateNever]
        public virtual ICollection<ExpenseVoucherDocumentModel> Documents { get; set; }
            = new List<ExpenseVoucherDocumentModel>();

        [ValidateNever]
        public virtual ICollection<VendorPaymentModel> VendorPayments { get; set; }
            = new List<VendorPaymentModel>();
    }
}
