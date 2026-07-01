using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class FinancialSequenceModel
    {
        [Key]
        public int SequenceId { get; set; }

        [Required]
        [StringLength(50)]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string FinancialYear { get; set; } = string.Empty;

        public int LastNumber { get; set; }

        public DateTime UpdatedOn { get; set; } = DateTime.Now;
    }
}