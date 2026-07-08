using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    public class CreditNoteModel
    {
        [Key]
        public int CreditNoteId { get; set; }

        [StringLength(30)]
        public string CreditNoteNo { get; set; } = string.Empty;

        [Required]
        public int OriginalInvoiceId { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxableValueReduction { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CGSTAdjustment { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SGSTAdjustment { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal IGSTAdjustment { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCredit { get; set; }

        [StringLength(450)]
        public string? ApprovedByUserId { get; set; }

        public DateTime? ApprovedOn { get; set; }

        [StringLength(20)]
        public string? GSTPeriod { get; set; }

        public bool IsCancelled { get; set; }

        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        [ForeignKey(nameof(OriginalInvoiceId))]
        [ValidateNever]
        public virtual InvoiceModel? OriginalInvoice { get; set; }
    }
}