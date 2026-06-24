using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    public class ProjectTaskProgressModel
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a task.")]
        public int ProjectTaskId { get; set; }

        [Required(ErrorMessage = "Progress date is required.")]
        [DataType(DataType.Date)]
        public DateTime ProgressDate { get; set; } = DateTime.Today;

        [Range(0, 24, ErrorMessage = "Hours worked must be between 0 and 24.")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal HoursWorked { get; set; }

        [Range(0, 100, ErrorMessage = "Completion percentage must be between 0 and 100.")]
        public int CompletionPercentage { get; set; }

        [Required(ErrorMessage = "Progress notes are required.")]
        [StringLength(1000)]
        public string ProgressNotes { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        public ProjectTaskModel? ProjectTask { get; set; }
    }
}