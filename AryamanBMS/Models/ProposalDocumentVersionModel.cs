using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class ProposalDocumentVersionModel
    {
        [Key]
        public int ProposalDocumentVersionId { get; set; }

        public int ProposalId { get; set; }

        public int ProposalTemplateId { get; set; }

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
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        public long FileSize { get; set; }

        [Required]
        [StringLength(450)]
        public string GeneratedByUserId { get; set; } =
            string.Empty;

        public DateTime GeneratedOn { get; set; } =
            DateTime.Now;

        public bool IsCurrent { get; set; } = true;

        [StringLength(500)]
        public string? Remarks { get; set; }

        public ProposalModel Proposal { get; set; } =
            null!;

        public ProposalTemplateModel ProposalTemplate
        {
            get;
            set;
        } = null!;
    }
}