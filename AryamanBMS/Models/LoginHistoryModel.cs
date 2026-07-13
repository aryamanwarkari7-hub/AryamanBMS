using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    public class LoginHistoryModel
    {
        public int Id { get; set; }

        [StringLength(450)]
        public string? UserId { get; set; }

        [Required]
        [StringLength(256)]
        public string AttemptedUserName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string EventType { get; set; } = string.Empty;

        public bool IsSuccessful { get; set; }

        [StringLength(250)]
        public string? FailureReason { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        public DateTime OccurredOn { get; set; } = DateTime.Now;

        [ForeignKey(nameof(UserId))]
        public ApplicationUserModel? User { get; set; }
    }
}