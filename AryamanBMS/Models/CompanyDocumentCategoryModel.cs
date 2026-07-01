using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class CompanyDocumentCategoryModel
    {
        [Key]
        public int DocumentCategoryId { get; set; }

        [StringLength(20)]
        public string CategoryCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;

        // Compliance
        public bool IsMandatory { get; set; }

        public int ExpiryReminderDays { get; set; } = 30;

        // Upload Rules
        public bool AllowMultipleDocuments { get; set; }

        [StringLength(200)]
        public string? AllowedExtensions { get; set; }

        public long? MaxFileSizeMB { get; set; }

        // Future Workflow
        public bool HasExpiry { get; set; } = true;

        public bool RequireDocumentNumber { get; set; }

        // Audit
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

       
    }
}