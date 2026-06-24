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

        [StringLength(100)]
        public string? MiddleName { get; set; }

        public string? LastName { get; set; }

        [NotMapped]
        public string FullName => string.Join(" ",
        new[]
        {
            FirstName,
            MiddleName,
            LastName
        }
        .Where(name => !string.IsNullOrWhiteSpace(name)));

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
        [RegularExpression(
         @"^[0-9]{6}$",
         ErrorMessage = "PIN code must contain exactly 6 digits.")]
        public string? PinCode { get; set; }

        public string? LocalAddress { get; set; }

        public string? LocalCity { get; set; }

        public string? LocalState { get; set; }

        [StringLength(6)]
        [RegularExpression(
            @"^[0-9]{6}$",
            ErrorMessage = "Local PIN code must contain exactly 6 digits.")]
        public string? LocalPinCode { get; set; }

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

        [Display(Name = "ESIC Number")]
        [StringLength(10, MinimumLength = 10,
            ErrorMessage = "ESIC Number must contain exactly 10 digits.")]
        [RegularExpression(@"^[0-9]{10}$",
            ErrorMessage = "ESIC Number must contain exactly 10 digits.")]
        public string? EsicNo { get; set; }

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

        // Leave 
        public ICollection<LeaveApplicationModel>
        LeaveApplications
        { get; set; } = new List<LeaveApplicationModel>();

        public ICollection<LeaveBalanceModel>
        LeaveBalances
        { get; set; } = new List<LeaveBalanceModel>();

        // Document Uploads and Records
        public ICollection<EmployeeAcademicModel> AcademicRecords { get; set; }
    = new List<EmployeeAcademicModel>();

        public ICollection<EmployeeDocumentModel> Documents { get; set; }
            = new List<EmployeeDocumentModel>();
    }
}