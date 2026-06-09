using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    public class EmployeeModel
    {
        public int Id { get; set; }

        [StringLength(20)]
        public string? EmployeeCode { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        [NotMapped]
        public string FullName =>
         $"{FirstName ?? string.Empty} {LastName ?? string.Empty}".Trim();

        //public string? Email { get; set; }

        [Phone]
        [StringLength(20)]
        public string? MobileNumber { get; set; }

        public DateTime JoiningDate { get; set; }

        public int DepartmentId { get; set; }

        public int DesignationId { get; set; }

        public bool IsActive { get; set; }

        // ==========================
        // Personal Information
        // ==========================

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        [EmailAddress]
        public string? PersonalEmail { get; set; }

        [EmailAddress]
        public string? OfficialEmail { get; set; }

        // ==========================
        // Address Information
        // ==========================

        public string? PermanentAddress { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        [StringLength(6)]
        public string? PinCode { get; set; }

        // ==========================
        // Emergency Contact
        // ==========================

        public string? EmergencyContact { get; set; }

        [StringLength(20)]
        public string? EmergencyPhone { get; set; }

        // ==========================
        // Statutory Information
        // ==========================

        [MaxLength(12)]
        public string? AadhaarNo { get; set; }

        [MaxLength(10)]
        public string? PanNo { get; set; }

        [MaxLength(30)]
        public string? UanNo { get; set; }

        // ==========================
        // Employment Information
        // ==========================

        public string? EmploymentType { get; set; }
        // Permanent / Contract / Intern / Consultant

        // ==========================
        // Relationships
        // ==========================

        public DepartmentModel? Department { get; set; }

        public DesignationModel? Designation { get; set; }

        public string? ApplicationUserId { get; set; }

        public ApplicationUserModel? ApplicationUser { get; set; }
    }
}