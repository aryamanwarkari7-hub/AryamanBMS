using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.ViewModels
{
    public class CalendarManualEventInputViewModel
    {
        public int? Id { get; set; }

        [Required]
        [StringLength(160)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public DateTime StartDateTime { get; set; }

        public DateTime? EndDateTime { get; set; }

        public bool IsAllDay { get; set; }

        [Required]
        [StringLength(40)]
        public string EventType { get; set; } = "Manual";

        [Required]
        [StringLength(40)]
        public string VisibilityScope { get; set; } = "All";
    }
}