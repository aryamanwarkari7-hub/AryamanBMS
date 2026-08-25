using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class ProjectMeetingAttendeeModel
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue)]
        public int MeetingId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select an employee.")]
        public int EmployeeId { get; set; }

        public bool IsPresent { get; set; } = true;

        [StringLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public ProjectMeetingModel? Meeting { get; set; }

        public EmployeeModel? Employee { get; set; }
    }
}