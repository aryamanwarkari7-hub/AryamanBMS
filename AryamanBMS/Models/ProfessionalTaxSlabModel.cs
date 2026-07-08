using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    public class ProfessionalTaxSlabModel
    {
        [Key]
        public int ProfessionalTaxSlabId { get; set; }

        [Required]
        [StringLength(100)]
        public string State { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SalaryFrom { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SalaryTo { get; set; }

        public int? Month { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }
}
