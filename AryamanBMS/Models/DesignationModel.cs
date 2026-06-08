using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class DesignationModel
    {
        public int Id { get; set; }

        public string DesignationName { get; set; }

        public string DisplayCode { get; set; }

        public int DepartmentId { get; set; }

        public bool IsActive { get; set; }

        public DepartmentModel? Department { get; set; }

        public ICollection<EmployeeModel>? Employees { get; set; }
    }
}