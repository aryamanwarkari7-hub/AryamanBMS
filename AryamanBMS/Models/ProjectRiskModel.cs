using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class ProjectRiskModel
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a project.")]
        public int ProjectId { get; set; }

        public int? RiskOwnerEmployeeId { get; set; }

        [Required(ErrorMessage = "Risk title is required.")]
        [StringLength(250)]
        public string RiskTitle { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? RiskDescription { get; set; }

        [Required]
        [StringLength(50)]
        public string RiskCategory { get; set; } = "Technical";

        [Range(1, 5, ErrorMessage = "Probability must be between 1 and 5.")]
        public int Probability { get; set; } = 1;

        [Range(1, 5, ErrorMessage = "Impact must be between 1 and 5.")]
        public int Impact { get; set; } = 1;

        public int RiskScore { get; set; }

        [Required]
        [StringLength(20)]
        public string Severity { get; set; } = "Low";

        [Required]
        [StringLength(30)]
        public string RiskStatus { get; set; } = "Open";

        [StringLength(2000)]
        public string? MitigationPlan { get; set; }

        [StringLength(2000)]
        public string? ContingencyPlan { get; set; }

        [DataType(DataType.Date)]
        public DateTime? TargetResolutionDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ResolvedOn { get; set; }

        [StringLength(1000)]
        public string? ResolutionRemarks { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        public ProjectModel? Project { get; set; }

        public EmployeeModel? RiskOwnerEmployee { get; set; }
    }
}