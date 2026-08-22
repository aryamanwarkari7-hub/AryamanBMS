using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.ViewModels
{
    public class GstConfigurationViewModel
    {
        public int GstConfigurationId { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal SgstRate { get; set; } = 9;

        [Required]
        [Range(0, 100)]
        public decimal CgstRate { get; set; } = 9;

        [Required]
        [Range(0, 100)]
        public decimal IgstRate { get; set; } = 18;

        [Required]
        [StringLength(15)]
        public string CompanyGstin { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string RegisteredState { get; set; } = "MH";

        [StringLength(100)]
        public string? LutReference { get; set; }

        [DataType(DataType.Date)]
        public DateTime? LutValidFrom { get; set; }

        [DataType(DataType.Date)]
        public DateTime? LutValidTo { get; set; }

        public bool IsActive { get; set; } = true;

        public string? UpdatedByUserId { get; set; }

        public DateTime LastUpdatedOn { get; set; } = DateTime.Now;
    }
}
