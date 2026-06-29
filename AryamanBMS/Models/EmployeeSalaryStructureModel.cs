using System.ComponentModel.DataAnnotations;

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

        [Required]
        [Range(0, 99999999)]
        public decimal ActualSalary { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }
    }
}