using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.ViewModels
{
    public class ProposalTemplateEditViewModel
    {
        public int ProposalTemplateId { get; set; }

        [Required]
        [StringLength(150)]
        public string TemplateName { get; set; } =
            string.Empty;

        [Range(1, 999)]
        public int VersionNumber { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        public bool IsActive { get; set; }

        public bool CanEditVersion { get; set; }

        public string OriginalFileName { get; set; } =
            string.Empty;
    }
}