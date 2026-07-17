using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class CompanyProfileModel
    {
        [Key]
        public int CompanyProfileId { get; set; }

        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(15)]
        public string? GSTIN { get; set; }

        [StringLength(20)]
        public string? PAN { get; set; }

        [StringLength(21)]
        [RegularExpression(
          @"^[LlUu][0-9]{5}[A-Za-z]{2}[0-9]{4}[A-Za-z]{3}[0-9]{6}$",
          ErrorMessage = "Enter a valid 21-character CIN.")]
        public string? CIN { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? VendorRegistrationNumber { get; set; }

        [StringLength(150)]
        public string? BankName { get; set; }

        [StringLength(100)]
        public string? AccountName { get; set; }

        [StringLength(50)]
        public string? AccountNumber { get; set; }

        [StringLength(20)]
        public string? IFSCCode { get; set; }

        [StringLength(150)]
        public string? BankBranch { get; set; }

        [StringLength(150)]
        public string? AuthorizedSignatory { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }
    }
}