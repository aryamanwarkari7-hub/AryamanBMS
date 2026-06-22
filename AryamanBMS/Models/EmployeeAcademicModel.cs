using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class EmployeeAcademicModel
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        [Required]
        [StringLength(100)]
        public string QualificationLevel { get; set; } = string.Empty;

        [StringLength(150)]
        public string? CourseName { get; set; }

        [StringLength(150)]
        public string? Specialization { get; set; }

        [StringLength(200)]
        public string? InstituteName { get; set; }

        [StringLength(200)]
        public string? BoardOrUniversity { get; set; }

        public int? PassingYear { get; set; }

        [StringLength(30)]
        public string? ResultType { get; set; }

        public decimal? Score { get; set; }

        [StringLength(20)]
        public string? Grade { get; set; }

        public bool IsHighestQualification { get; set; }

        public EmployeeModel? Employee { get; set; }

        public ICollection<EmployeeDocumentModel> Documents { get; set; }
            = new List<EmployeeDocumentModel>();
    }
}