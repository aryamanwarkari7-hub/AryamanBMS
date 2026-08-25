using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class VendorModel
    {
        [Key]
        public int VendorId { get; set; }

        [Required]
        [StringLength(30)]
        public string VendorCode { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string VendorName { get; set; } = string.Empty;

        [StringLength(15)]
        public string? GSTIN { get; set; }

        [StringLength(10)]
        public string? PAN { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(2)]
        public string? StateCode { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? RegistrationType { get; set; }

        [StringLength(100)]
        public string? PaymentTerms { get; set; }

        [StringLength(1000)]
        public string? BankDetails { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        public virtual ICollection<ExpenseVoucherModel> ExpenseVouchers { get; set; }
            = new List<ExpenseVoucherModel>();
    }
}
