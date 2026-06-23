using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class ProjectMeetingModel
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a project.")]
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Meeting title is required.")]
        [StringLength(200)]
        public string MeetingTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Meeting date is required.")]
        [DataType(DataType.Date)]
        public DateTime MeetingDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Start time is required.")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? EndTime { get; set; }

        [Required]
        [StringLength(30)]
        public string MeetingMode { get; set; } = "In Person";

        [StringLength(300)]
        public string? LocationOrLink { get; set; }

        [Required(ErrorMessage = "Agenda is required.")]
        [StringLength(2000)]
        public string Agenda { get; set; } = string.Empty;

        [StringLength(4000)]
        public string? DiscussionSummary { get; set; }

        [StringLength(3000)]
        public string? DecisionsTaken { get; set; }

        [DataType(DataType.Date)]
        public DateTime? NextMeetingDate { get; set; }

        [Required]
        [StringLength(30)]
        public string MeetingStatus { get; set; } = "Scheduled";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        public ProjectModel? Project { get; set; }

        public ICollection<ProjectMeetingAttendeeModel> Attendees { get; set; }
            = new List<ProjectMeetingAttendeeModel>();

        public ICollection<ProjectMeetingActionItemModel> ActionItems { get; set; }
            = new List<ProjectMeetingActionItemModel>();
    }
}