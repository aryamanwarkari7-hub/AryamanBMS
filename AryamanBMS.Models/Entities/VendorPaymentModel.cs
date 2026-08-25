using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    public class VendorPaymentModel
    {
        [Key]
        public int VendorPaymentId { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentNo { get; set; } = string.Empty;

        [Required]
        public int VendorId { get; set; }

        [Required]
        public int ExpenseVoucherId { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal AmountPaid { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMode { get; set; } = "Bank Transfer";

        public int? BankAccountId { get; set; }

        [StringLength(100)]
        public string? TransactionReference { get; set; }

        [StringLength(450)]
        public string PaidByUserId { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Remarks { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [ForeignKey(nameof(VendorId))]
        [ValidateNever]
        public virtual VendorModel? Vendor { get; set; }

        [ForeignKey(nameof(ExpenseVoucherId))]
        [ValidateNever]
        public virtual ExpenseVoucherModel? ExpenseVoucher { get; set; }
    }
}
