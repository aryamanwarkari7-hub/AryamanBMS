using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    public class ProjectTimelineModel
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }

        [Required]
        [StringLength(50)]
        public string EventType { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string EventTitle { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? EventDescription { get; set; }

        [StringLength(50)]
        public string? RelatedEntityType { get; set; }

        public int? RelatedEntityId { get; set; }

        [StringLength(500)]
        public string? PreviousValue { get; set; }

        [StringLength(500)]
        public string? NewValue { get; set; }

        public DateTime EventDate { get; set; }

        [StringLength(255)]
        public string? CreatedByUserId { get; set; }

        [StringLength(200)]
        public string? CreatedByName { get; set; }

        public bool IsSystemGenerated { get; set; } = true;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [ForeignKey(nameof(ProjectId))]
        public ProjectModel? Project { get; set; }
    }
}