using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    public class InvoiceDetailsModel
    {
        [Key]
        public int InvoiceDetailId { get; set; }

        public int InvoiceId { get; set; }

        [Required]
        [StringLength(200)]
        public string ItemName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, 999999999)]
        public decimal Qty { get; set; }

        [StringLength(30)]
        public string Unit { get; set; } = string.Empty;

        [Range(0, 999999999999)]
        public decimal Rate { get; set; }

        [Range(0, 100)]
        public decimal GSTPercent { get; set; }

        [Range(0, 999999999999)]
        public decimal GSTAmount { get; set; }

        [Range(0, 999999999999)]
        public decimal Amount { get; set; }

        public int SortOrder { get; set; }

        [ForeignKey(nameof(InvoiceId))]
        [ValidateNever]
        public InvoiceModel Invoice { get; set; } = null!;
    }
}