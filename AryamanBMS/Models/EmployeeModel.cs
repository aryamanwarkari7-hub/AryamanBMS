namespace AryamanBMS.Models
{
    public class EmployeeModel
    {
        public int Id { get; set; }

        public string EmployeeCode { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public string MobileNumber { get; set; }

        public DateTime JoiningDate { get; set; }

        public int DepartmentId { get; set; }

        public int DesignationId { get; set; }

        public bool IsActive { get; set; }

        public DepartmentModel? Department { get; set; }

        public DesignationModel? Designation { get; set; }

        public string? ApplicationUserId { get; set; }

        public ApplicationUserModel? ApplicationUser { get; set; }
    }
}