using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    public class ProjectModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Project code is required.")]
        [StringLength(20)]
        public string ProjectCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Project name is required.")]
        [StringLength(150)]
        public string ProjectName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Project type is required.")]
        [StringLength(50)]
        public string ProjectType { get; set; } = string.Empty;

        [StringLength(150)]
        public string? ClientName { get; set; }

        public string? BusinessObjective { get; set; }

        public string? Scope { get; set; }

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Range(0, double.MaxValue,
            ErrorMessage = "Budget cannot be negative.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Budget { get; set; }

        [Required]
        [StringLength(20)]
        public string Priority { get; set; } = "Medium";

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Planning";

        [Range(1, int.MaxValue,
            ErrorMessage = "Please select a project manager.")]
        public int ProjectManagerId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        public EmployeeModel? ProjectManager { get; set; }
    }
}