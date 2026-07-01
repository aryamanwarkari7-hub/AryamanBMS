using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    public class CompanyDocumentModel
    {
        [Key]
        public int CompanyDocumentId { get; set; }

        [Required]
        public int DocumentCategoryId { get; set; }

        [Required]
        [StringLength(200)]
        public string DocumentName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? DocumentNumber { get; set; }

        [StringLength(200)]
        public string? IssuedBy { get; set; }

        public DateTime? IssueDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        // These three are NOT entered by the user — they're populated
        // by the controller AFTER ModelState.IsValid is checked.
        // [ValidateNever] stops MVC from implicitly requiring them.
        [ValidateNever]
        public string FileName { get; set; } = string.Empty;

        [ValidateNever]
        public string StoredFileName { get; set; } = string.Empty;

        [ValidateNever]
        public string? FileExtension { get; set; }

        [ValidateNever]
        public string? ContentType { get; set; }

        public long FileSize { get; set; }

        [ValidateNever]
        public string FilePath { get; set; } = string.Empty;

        public int VersionNo { get; set; } = 1;

        public bool IsMandatory { get; set; }

        public string? Remarks { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        [ForeignKey(nameof(DocumentCategoryId))]
        [ValidateNever]
        public virtual CompanyDocumentCategoryModel? Category { get; set; }
    }
}