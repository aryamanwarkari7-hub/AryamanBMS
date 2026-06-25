using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.ViewModels
{
    public class CompOffRequestViewModel
    {
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Worked Date")]
        public DateTime WorkedDate { get; set; }

        [Required]
        [Range(0.5, 1.0)]
        [Display(Name = "Credit Days")]
        public decimal CreditDays { get; set; } = 1.0m;

        [Required]
        [StringLength(500)]
        public string Remarks { get; set; } = string.Empty;
    }
}