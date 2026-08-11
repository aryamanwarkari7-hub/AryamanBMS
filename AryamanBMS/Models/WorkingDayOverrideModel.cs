using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class WorkingDayOverrideModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime OverrideDate { get; set; }

        [Required]
        [StringLength(20)]
        public string OverrideType { get; set; } = "Working Day";

        [StringLength(250)]
        public string? Reason { get; set; }

        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        public bool IsActive { get; set; } = true;
    }
}