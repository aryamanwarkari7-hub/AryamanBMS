using AryamanBMS.Models;

namespace AryamanBMS.ViewModels
{
    public class AccountProfileViewModel
    {
        public ApplicationUserModel User { get; set; } = null!;

        public EmployeeModel? Employee { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public string Initials { get; set; } = string.Empty;

        public bool HasEmployeeProfile => Employee != null;
    }
}