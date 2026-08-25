using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    public class OfficeAssetDocumentModel
    {
        [Key]
        public int OfficeAssetDocumentId { get; set; }

        public int OfficeAssetId { get; set; }

        [Required]
        [StringLength(50)]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string StoredFilePath { get; set; } = string.Empty;

        [StringLength(450)]
        public string UploadedByUserId { get; set; } = string.Empty;

        public DateTime UploadedOn { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string? Remarks { get; set; }

        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(OfficeAssetId))]
        [ValidateNever]
        public OfficeAssetModel? OfficeAsset { get; set; }
    }
}
