namespace AryamanBMS.Models
{
    public class DesignationModel
    {
        public int Id { get; set; }

        public string DesignationName { get; set; } = string.Empty;

        public string DisplayCode { get; set; } = string.Empty;

        public int DepartmentId { get; set; }

        public bool IsActive { get; set; }

        public DepartmentModel? Department { get; set; }

        public ICollection<EmployeeModel>? Employees { get; set; }
    }
}