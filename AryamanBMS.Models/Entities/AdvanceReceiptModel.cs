using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    public class AdvanceReceiptModel
    {
        [Key]
        public int AdvanceReceiptId { get; set; }

        [StringLength(30)]
        public string AdvanceReceiptNo { get; set; } = string.Empty;

        [Required]
        public int ClientId { get; set; }

        public int? ProjectId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public DateTime ReceiptDate { get; set; } = DateTime.Today;

        [Required]
        [StringLength(30)]
        public string PaymentMode { get; set; } = "Bank Transfer";

        [StringLength(100)]
        public string? PaymentReference { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AvailableBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AdjustedAmount { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        [StringLength(450)]
        public string? ReceivedByUserId { get; set; }

        public bool IsCancelled { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        [ForeignKey(nameof(ClientId))]
        [ValidateNever]
        public virtual ClientModel? Client { get; set; }

        [ForeignKey(nameof(ProjectId))]
        [ValidateNever]
        public virtual ProjectModel? Project { get; set; }
    }
}