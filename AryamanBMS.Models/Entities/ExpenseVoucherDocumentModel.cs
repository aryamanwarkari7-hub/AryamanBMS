using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    public class ExpenseVoucherDocumentModel
    {
        [Key]
        public int ExpenseVoucherDocumentId { get; set; }

        [Required]
        public int ExpenseVoucherId { get; set; }

        [Required]
        [StringLength(50)]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string StoredFilePath { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Remarks { get; set; }

        [Required]
        [StringLength(450)]
        public string UploadedByUserId { get; set; } = string.Empty;

        public DateTime UploadedOn { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(ExpenseVoucherId))]
        [ValidateNever]
        public virtual ExpenseVoucherModel? ExpenseVoucher { get; set; }
    }
}
