using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class ProposalTemplateModel
    {
        [Key]
        public int ProposalTemplateId { get; set; }

        [Required]
        [StringLength(150)]
        public string TemplateName { get; set; } =
            string.Empty;

        public int VersionNumber { get; set; }

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

        public bool IsActive { get; set; } = true;

        [Required]
        [StringLength(450)]
        public string UploadedByUserId { get; set; } =
            string.Empty;

        public DateTime UploadedOn { get; set; } =
            DateTime.Now;

        [StringLength(500)]
        public string? Remarks { get; set; }

        public ICollection<ProposalDocumentVersionModel>
            ProposalDocuments
        { get; set; } =
                new List<ProposalDocumentVersionModel>();
    }
}