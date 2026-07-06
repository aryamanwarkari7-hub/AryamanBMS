using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.ViewModels
{
    public class ProposalTemplateViewModel
    {
        [Required]
        [StringLength(150)]
        public string TemplateName { get; set; } =
            "Standard Proposal Template";

        [StringLength(500)]
        public string? Remarks { get; set; }

        [Required(ErrorMessage = "Please select a template file.")]
        public IFormFile? TemplateFile { get; set; }
    }
}