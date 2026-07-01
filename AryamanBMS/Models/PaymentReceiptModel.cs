
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    public class PaymentReceiptModel
    {
        [Key]
        public int PaymentReceiptId { get; set; }

        [Required]
        [StringLength(30)]
        public string ReceiptNo { get; set; } = string.Empty;

        [Required]
        public DateTime ReceiptDate { get; set; } = DateTime.Today;

        [Required]
        public int InvoiceId { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountReceived { get; set; }

        [Required]
        [StringLength(30)]
        public string PaymentMode { get; set; } = "Cash";

        [StringLength(200)]
        public string? BankName { get; set; }

        [StringLength(100)]
        public string? TransactionNo { get; set; }

        [StringLength(100)]
        public string? ReferenceNo { get; set; }

        [StringLength(150)]
        public string? ReceivedBy { get; set; }

        [StringLength(500)]
        public string? AttachmentPath { get; set; }

        public string? Remarks { get; set; }

        public bool IsCancelled { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        //==========================
        // Navigation Properties
        //==========================

        [ForeignKey(nameof(InvoiceId))]
        public virtual InvoiceModel? Invoice { get; set; }

        [ForeignKey(nameof(ClientId))]
        public virtual ClientModel? Client { get; set; }
    }
}