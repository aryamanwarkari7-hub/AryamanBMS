using Microsoft.AspNetCore.Identity;

namespace AryamanBMS.Models
{
    public class ApplicationUserModel : IdentityUser
    {
        public string? FullName { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
