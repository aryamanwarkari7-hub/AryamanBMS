using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    public class EmployeeSalaryStructureModel
    {
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        public EmployeeModel? Employee { get; set; }

        [Required]
        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        [Required]
        [Range(0, 99999999)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ActualSalary { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BasicSalary { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal HRA { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DA { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Conveyance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MedicalAllowance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal EducationAllowance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SpecialAllowance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OtherAllowances { get; set; }

        public bool IsPfApplicable { get; set; }

        public bool IsEsicApplicable { get; set; }

        public bool IsPtApplicable { get; set; }

        public bool IsTdsApplicable { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PreviousSalary { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RevisedSalary { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        public decimal RevisionPercentage { get; set; }

        public DateTime? RevisionEffectiveDate { get; set; }

        [StringLength(500)]
        public string? RevisionReason { get; set; }

        [StringLength(450)]
        public string? ApprovedByUserId { get; set; }

        public DateTime? ApprovedOn { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        [StringLength(450)]
        public string? UpdatedByUserId { get; set; }
    }
}
