using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    public class DailyWorkReportModel
    {
        [Key]
        public int DailyWorkReportId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime ReportDate { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal TotalPlannedHours { get; set; } = 0m;

        [Column(TypeName = "decimal(5,2)")]
        public decimal TotalActualHours { get; set; } = 0m;

        [StringLength(1000)]
        public string? GeneralRemarks { get; set; }

        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        [ForeignKey(nameof(EmployeeId))]
        [ValidateNever]
        public virtual EmployeeModel? Employee { get; set; }

        [ForeignKey(nameof(CreatedByUserId))]
        [ValidateNever]
        public virtual ApplicationUserModel? CreatedByUser { get; set; }

        public virtual ICollection<DailyWorkReportDetailModel> WorkDetails { get; set; } =
            new List<DailyWorkReportDetailModel>();

        public virtual ICollection<DailyWorkReportPlanModel> NextDayPlans { get; set; } =
            new List<DailyWorkReportPlanModel>();
    }
}
