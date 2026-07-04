using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class FinancialAuditDocumentModel
    {
        [Key]
        public int FinancialAuditDocumentId { get; set; }

        /// <summary>
        /// BankStatement
        /// AuditReport
        /// CADocument
        /// CSDocument
        /// Other
        /// </summary>
        [Required]
        [StringLength(30)]
        public string DocumentCategory { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string FinancialYear { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [StringLength(450)]
        public string UploadedByUserId { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Remarks { get; set; }

        public DateTime UploadedOn { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        public bool IsFinalized { get; set; }

        [StringLength(450)]
        public string? FinalizedByUserId { get; set; }

        public DateTime? FinalizedOn { get; set; }
    }
}
