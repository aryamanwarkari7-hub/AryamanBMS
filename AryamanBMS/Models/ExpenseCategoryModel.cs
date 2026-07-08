using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class ExpenseCategoryModel
    {
        [Key]
        public int ExpenseCategoryId { get; set; }

        [Required]
        [StringLength(50)]
        public string CategoryCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Range(0, 100, ErrorMessage = "GST Rate must be between 0 and 100")]
        public decimal DefaultGSTRate { get; set; } = 0m;

        public bool ITCEligible { get; set; } = true;

        [StringLength(50)]
        public string? GLAccountCode { get; set; }

        [StringLength(50)]
        public string ExpenseType { get; set; } = "General";

        [StringLength(50)]
        public string? PayableGLAccountCode { get; set; }

        [StringLength(50)]
        public string? InputGSTGLAccountCode { get; set; }

        public bool IsCapitalExpense { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        // Navigation
        public virtual ICollection<ExpenseVoucherModel> ExpenseVouchers { get; set; } = new List<ExpenseVoucherModel>();
    }
}
