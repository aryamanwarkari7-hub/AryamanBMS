using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    public class DailyWorkReportDetailModel
    {
        [Key]
        public int DailyWorkReportDetailId { get; set; }

        [Required]
        public int DailyWorkReportId { get; set; }

        public int? ProjectId { get; set; }

        [Required]
        [StringLength(150)]
        public string ProjectName { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Activity { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal PlannedEffortHours { get; set; } = 0m;

        [Column(TypeName = "decimal(5,2)")]
        public decimal ActualEffortHours { get; set; } = 0m;

        [StringLength(500)]
        public string? Dependencies { get; set; }

        [StringLength(500)]
        public string? Issues { get; set; }

        public int SortOrder { get; set; } = 1;

        // Navigation
        [ForeignKey(nameof(DailyWorkReportId))]
        [ValidateNever]
        public virtual DailyWorkReportModel? DailyWorkReport { get; set; }

        [ForeignKey(nameof(ProjectId))]
        [ValidateNever]
        public virtual ProjectModel? Project { get; set; }
    }
}
