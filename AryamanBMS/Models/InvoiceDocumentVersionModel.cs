using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    public class InvoiceDocumentVersionModel
    {
        [Key]
        public int InvoiceDocumentVersionId { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        [Required]
        public int VersionNumber { get; set; }

        [Required]
        [StringLength(20)]
        public string DocumentFormat { get; set; } =
            "DOCX";

        [Required]
        [StringLength(255)]
        public string OriginalFileName { get; set; } =
            string.Empty;

        [Required]
        [StringLength(500)]
        public string StoredFilePath { get; set; } =
            string.Empty;

        [Required]
        [StringLength(150)]
        public string ContentType { get; set; } =
            string.Empty;

        public long FileSize { get; set; }

        [Required]
        [StringLength(450)]
        public string GeneratedByUserId { get; set; } =
            string.Empty;

        public DateTime GeneratedOn { get; set; }

        public bool IsCurrent { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        [ForeignKey(nameof(InvoiceId))]
        [ValidateNever]
        public virtual InvoiceModel? Invoice { get; set; }
    }
}