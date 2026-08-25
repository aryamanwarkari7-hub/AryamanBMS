using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    [Table("TableEmployeePreviousEmployment")]
    public class EmployeePreviousEmploymentModel
    {
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [Required]
        [StringLength(150)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Designation { get; set; }

        [StringLength(100)]
        public string? Department { get; set; }

        [StringLength(50)]
        public string? EmploymentType { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? LastSalary { get; set; }

        [StringLength(500)]
        public string? ReasonForLeaving { get; set; }

        [StringLength(500)]
        public string? CompanyAddress { get; set; }

        [StringLength(100)]
        public string? CompanyCity { get; set; }

        [StringLength(100)]
        public string? CompanyState { get; set; }

        [StringLength(10)]
        public string? CompanyPinCode { get; set; }

        [StringLength(200)]
        public string? CompanyWebsite { get; set; }

        [StringLength(150)]
        public string? HRContactName { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? HRContactEmail { get; set; }

        [StringLength(20)]
        public string? HRContactNumber { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        public EmployeeModel? Employee { get; set; }

        public ICollection<EmployeeDocumentModel> Documents { get; set; }
            = new List<EmployeeDocumentModel>();
    }
}