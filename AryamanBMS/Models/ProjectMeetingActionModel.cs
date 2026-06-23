using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class ProjectMeetingActionItemModel
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue)]
        public int MeetingId { get; set; }

        [Required(ErrorMessage = "Action item title is required.")]
        [StringLength(250)]
        public string ActionTitle { get; set; } = string.Empty;

        [StringLength(1500)]
        public string? Description { get; set; }

        public int? AssignedEmployeeId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Required]
        [StringLength(30)]
        public string ActionStatus { get; set; } = "Pending";

        [DataType(DataType.Date)]
        public DateTime? CompletedOn { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        public ProjectMeetingModel? Meeting { get; set; }

        public EmployeeModel? AssignedEmployee { get; set; }
    }
}