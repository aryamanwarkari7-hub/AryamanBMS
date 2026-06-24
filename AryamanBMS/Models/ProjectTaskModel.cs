using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    public class ProjectTaskModel
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        public int? AssignedEmployeeId { get; set; }

        [Required(ErrorMessage = "Task code is required.")]
        [StringLength(30)]
        public string TaskCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Task title is required.")]
        [StringLength(200)]
        public string TaskTitle { get; set; } = string.Empty;

        public string? Description { get; set; }

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Priority { get; set; } = "Medium";

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Not Started";

        [Range(0, 100)]
        public int ProgressPercent { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Range(0, double.MaxValue)]
        public decimal EstimatedHours { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Range(0, double.MaxValue)]
        public decimal ActualHours { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        public bool IsActive { get; set; } = true;

        public ProjectModel? Project { get; set; }

        public EmployeeModel? AssignedEmployee { get; set; }
    }
}