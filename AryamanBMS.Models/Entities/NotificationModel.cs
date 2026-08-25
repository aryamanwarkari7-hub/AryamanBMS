using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    public class NotificationModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string NotificationType { get; set; } = "System";

        [StringLength(100)]
        public string? ReferenceType { get; set; }

        public int? ReferenceId { get; set; }

        [StringLength(500)]
        public string? ActionUrl { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? ReadOn { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUserModel? User { get; set; }
    }
}