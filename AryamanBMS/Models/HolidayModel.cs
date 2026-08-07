using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class HolidayModel
    {
        [Key]
        public int HolidayId { get; set; }

        [Required]
        public DateTime HolidayDate { get; set; }

        [Required]
        [StringLength(160)]
        public string HolidayName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? MonthName { get; set; }

        [StringLength(20)]
        public string? DayName { get; set; }

        [StringLength(80)]
        public string HolidayType { get; set; } = "Office Holiday";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }
    }
}