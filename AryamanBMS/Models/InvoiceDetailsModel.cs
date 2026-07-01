using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class InvoiceDetailsModel
    {
        [Key]
        public int InvoiceDetailId { get; set; }

        public int InvoiceId { get; set; }

        public string ItemName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public decimal Qty { get; set; }

        public string Unit { get; set; } = string.Empty;

        public decimal Rate { get; set; }

        public decimal GSTPercent { get; set; }

        public decimal GSTAmount { get; set; }

        public decimal Amount { get; set; }

        public int SortOrder { get; set; }

        public InvoiceModel Invoice { get; set; } = null!;
    }
}