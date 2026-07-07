using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    public class InvoiceModel
    {
        [Key]
        public int InvoiceId { get; set; }

        public string InvoiceNo { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string InvoiceType { get; set; } = "Tax Invoice";

        [Required]
        public DateTime InvoiceDate { get; set; }

        public int? ProposalId { get; set; }

        public int? PurchaseWorkOrderId { get; set; }

        [Required]
        public int ClientId { get; set; }

        public int? ProjectId { get; set; }

        public string? BillingAddress { get; set; }

        public string? GSTNo { get; set; }

        public bool IsInterState { get; set; }

        public string? ProjectName { get; set; }

        public DateTime? DueDate { get; set; }

        public string? PaymentTerms { get; set; }

        public decimal SubTotal { get; set; }

        public decimal Discount { get; set; }

        public decimal GSTAmount { get; set; }

        public decimal GrandTotal { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal BalanceAmount { get; set; }

        public string InvoiceStatus { get; set; } = "Draft";

        public string PaymentStatus { get; set; } = "Unpaid";

        [StringLength(450)]
        public string? IssuedByUserId { get; set; }

        public DateTime? IssuedOn { get; set; }

        [StringLength(450)]
        public string? CancelledByUserId { get; set; }

        public DateTime? CancelledOn { get; set; }

        [StringLength(500)]
        public string? CancellationReason { get; set; }

        public int? ReplacementInvoiceId { get; set; }

        public string? Remarks { get; set; }

        public string? AttachmentPath { get; set; }

        public bool IsDeleted { get; set; }

        public int CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        public int? ModifiedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        


        [ForeignKey(nameof(ClientId))]
        [ValidateNever]
        public virtual ClientModel? Client { get; set; }

        [ForeignKey(nameof(ProjectId))]
        [ValidateNever]
        public virtual ProjectModel? Project { get; set; }

        [ForeignKey(nameof(ProposalId))]
        [ValidateNever]
        public virtual ProposalModel? Proposal { get; set; }

        [ForeignKey(nameof(PurchaseWorkOrderId))]
        [ValidateNever]
        public virtual PurchaseOrderModel? PurchaseOrder { get; set; }

        [StringLength(50)]
        public string? SACCode { get; set; }

        


        [ValidateNever]
        public virtual ICollection<InvoiceDetailsModel> InvoiceDetails 
        { get; set; } = new List<InvoiceDetailsModel>();

        [ValidateNever]
        public virtual ICollection<InvoiceDocumentVersionModel>
        DocumentVersions{ get; set; } = new List<InvoiceDocumentVersionModel>();
    }
}