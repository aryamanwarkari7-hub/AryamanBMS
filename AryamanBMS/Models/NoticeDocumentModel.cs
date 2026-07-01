using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    public class NoticeDocumentModel
    {
        [Key]
        public int NoticeDocumentId { get; set; }

        [Required]
        public int NoticeId { get; set; }

        [ForeignKey(nameof(NoticeId))]
        [ValidateNever]
        public virtual NoticeModel? Notice { get; set; }

        /// <summary>
        /// NoticeCopy, ReplyCopy, SupportingEvidence, Other
        /// </summary>
        [Required]
        [StringLength(30)]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Remarks { get; set; }

        public DateTime UploadedOn { get; set; } = DateTime.Now;
    }
}