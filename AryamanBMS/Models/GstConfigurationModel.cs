using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    public class GstConfigurationModel
    {
        [Key]
        public int GstConfigurationId { get; set; }

        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        public string CompanyGstin { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string RegisteredState { get; set; } = string.Empty;

        [Column(TypeName = "decimal(5,2)")]
        public decimal CgstRate { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal SgstRate { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal IgstRate { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(450)]
        public string? UpdatedByUserId { get; set; }

        public DateTime UpdatedOn { get; set; } = DateTime.Now;
    }
}
