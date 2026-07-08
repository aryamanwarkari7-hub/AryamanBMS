using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AryamanBMS.ViewModels
{
    public class AdvanceReceiptApplyViewModel
    {
        public int AdvanceReceiptId { get; set; }

        public string AdvanceReceiptNo { get; set; } = string.Empty;

        public string ClientName { get; set; } = string.Empty;

        public decimal AvailableBalance { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal AmountToAdjust { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        public IEnumerable<SelectListItem> Invoices { get; set; } =
            Enumerable.Empty<SelectListItem>();
    }
}
