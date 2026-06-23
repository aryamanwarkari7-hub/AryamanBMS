using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class ProjectFlowModel
    {
        public int Id { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a project.")]
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Stage name is required.")]
        [StringLength(100)]
        public string StageName { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Stage order must be greater than 0.")]
        public int StageOrder { get; set; }

        [Required]
        [StringLength(30)]
        public string StageStatus { get; set; } = "Pending";

        [DataType(DataType.Date)]
        public DateTime? PlannedStartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? PlannedEndDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ActualStartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ActualEndDate { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        public ProjectModel? Project { get; set; }
    }
}