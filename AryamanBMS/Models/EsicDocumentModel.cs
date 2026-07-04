using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    public class EsicDocumentModel
    {
        [Key]
        public int EsicDocumentId { get; set; }

        [Required]
        public int EsicSnapshotId { get; set; }

        [ForeignKey(nameof(EsicSnapshotId))]
        [ValidateNever]
        public virtual EsicMonthlySnapshotModel? Snapshot { get; set; }

        /// <summary>
        /// Challan, Return, Other
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

        [StringLength(450)]
        public string? UploadedByUserId { get; set; }

        public DateTime UploadedOn { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;
    }
}
