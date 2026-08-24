using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class GstLutDocumentModel
    {
        [Key]
        public int GstLutDocumentId { get; set; }

        [Required]
        public int GstConfigurationId { get; set; }

        [StringLength(100)]
        public string? LutReference { get; set; }

        [DataType(DataType.Date)]
        public DateTime? LutValidFrom { get; set; }

        [DataType(DataType.Date)]
        public DateTime? LutValidTo { get; set; }

        [Required]
        [StringLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string StoredFileName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public DateTime UploadedOn { get; set; } = DateTime.Now;

        [StringLength(450)]
        public string? UploadedByUserId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}