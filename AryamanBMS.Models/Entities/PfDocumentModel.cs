using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    public class PfDocumentModel
    {
        [Key]
        public int PfDocumentId { get; set; }

        [Required]
        public int PfSnapshotId { get; set; }

        [ForeignKey(nameof(PfSnapshotId))]
        [ValidateNever]
        public virtual PfMonthlySnapshotModel? Snapshot { get; set; }

        /// <summary>
        /// ECR, Challan, Other
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
