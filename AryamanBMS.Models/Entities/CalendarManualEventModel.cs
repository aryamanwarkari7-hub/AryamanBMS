using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    public class CalendarManualEventModel
    {
        public int Id { get; set; }

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

        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        [StringLength(450)]
        public string? UpdatedByUserId { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(CreatedByUserId))]
        public ApplicationUserModel? CreatedByUser { get; set; }

        [ForeignKey(nameof(UpdatedByUserId))]
        public ApplicationUserModel? UpdatedByUser { get; set; }
    }
}