using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class DepartmentModel
    {
        public int Id { get; set; }

        public string DepartmentName { get; set; }

        public string DisplayCode { get; set; }

        public bool IsActive { get; set; }

        public ICollection<DesignationModel>? Designations { get; set; }

        public ICollection<EmployeeModel>? Employees { get; set; }
    }
}