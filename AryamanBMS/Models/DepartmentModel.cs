

using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class DepartmentModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Department is required.")]
        public string DepartmentName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Display Code is required.")]
        public string DisplayCode { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public ICollection<DesignationModel>? Designations { get; set; }

        public ICollection<EmployeeModel>? Employees { get; set; }
    }
}