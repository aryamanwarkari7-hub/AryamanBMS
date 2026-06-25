using System.ComponentModel.DataAnnotations;
using AryamanBMS.Models;

namespace AryamanBMS.ViewModels
{
    public class EmployeeAcademicInputViewModel
    {
        public int? Id { get; set; }

        [Required]
        public string QualificationLevel { get; set; } = string.Empty;

        public string? CourseName { get; set; }

        public string? Specialization { get; set; }

        public string? InstituteName { get; set; }

        public string? BoardOrUniversity { get; set; }

        public int? PassingYear { get; set; }

        public string? ResultType { get; set; }

        public decimal? Score { get; set; }

        public string? Grade { get; set; }

        public bool IsHighestQualification { get; set; }

        public string DocumentType { get; set; } = "Academic Document";

        public List<IFormFile>? Documents { get; set; }

        public List<EmployeeDocumentModel> ExistingDocuments { get; set; }
            = new();
    }
}