using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class EmployeeDocumentModel
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public int? EmployeeAcademicId { get; set; }

        [Required]
        [StringLength(100)]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string StoredFileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string StoragePath { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ContentType { get; set; }

        public long FileSize { get; set; }

        public DateTime UploadedOn { get; set; } = DateTime.Now;

        [StringLength(256)]
        public string? UploadedBy { get; set; }

        public EmployeeModel? Employee { get; set; }

        public EmployeeAcademicModel? EmployeeAcademic { get; set; }
    }
}