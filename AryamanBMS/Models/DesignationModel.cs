using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class DesignationModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Designation is required.")]
        public string DesignationName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Display Code is required.")]
        public string DisplayCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Department is required.")]
        public int? DepartmentId { get; set; }

        public bool IsActive { get; set; }

        public DepartmentModel? Department { get; set; }

        public ICollection<EmployeeModel>? Employees { get; set; }
    }
}