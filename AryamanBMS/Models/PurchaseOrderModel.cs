using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    public class PurchaseOrderModel
    {
        [Key]
        public int PurchaseOrderId { get; set; }

        [StringLength(30)]
        public string OrderNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Order type is required.")]
        [StringLength(5)]
        public string OrderType { get; set; } = "PO";

        [Required(ErrorMessage = "Client is required.")]
        public int ClientId { get; set; }

        public int? ProposalId { get; set; }

        [Required(ErrorMessage = "Order title is required.")]
        [StringLength(300)]
        public string OrderTitle { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        public DateTime? DeliveryDueDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Amount cannot be negative.")]
        public decimal? OrderAmount { get; set; }

        [StringLength(10)]
        public string Currency { get; set; } = "INR";

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Open";

        [StringLength(200)]
        public string? VendorReference { get; set; }

        public string? Remarks { get; set; }

        [ValidateNever] public string FileName { get; set; } = string.Empty;
        [ValidateNever] public string StoredFileName { get; set; } = string.Empty;
        [ValidateNever] public string? FileExtension { get; set; }
        [ValidateNever] public string? ContentType { get; set; }
        [ValidateNever] public long FileSize { get; set; }
        [ValidateNever] public string FilePath { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public DateTime? UpdatedOn { get; set; }

        [ForeignKey(nameof(ClientId))]
        [ValidateNever]
        public virtual ClientModel? Client { get; set; }

        [ForeignKey(nameof(ProposalId))]
        [ValidateNever]
        public virtual ProposalModel? Proposal { get; set; }
    }
}