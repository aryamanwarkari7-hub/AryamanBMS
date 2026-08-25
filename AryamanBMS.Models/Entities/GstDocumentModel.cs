using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    public class GstDocumentModel
    {
        [Key]
        public int GstDocumentId { get; set; }

        [Required]
        public int SnapshotId { get; set; }

        [ForeignKey(nameof(SnapshotId))]
        [ValidateNever]
        public virtual GstMonthlySnapshotModel? Snapshot { get; set; }

        /// <summary>
        /// Working
        /// GSTR1
        /// GSTR3B
        /// Challan
        /// Excel
        /// Other
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
    }
}
