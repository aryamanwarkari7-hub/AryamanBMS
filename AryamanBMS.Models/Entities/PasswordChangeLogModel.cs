using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class PasswordChangeLogModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [StringLength(150)]
        public string? UserName { get; set; }

        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(450)]
        public string? ChangedByUserId { get; set; }

        [StringLength(150)]
        public string? ChangedByUserName { get; set; }

        [Required]
        [StringLength(30)]
        public string ChangeType { get; set; } = string.Empty;
        // SelfChange, AdminReset

        public DateTime ChangedOn { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }
    }
}