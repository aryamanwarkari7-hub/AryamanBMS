namespace AryamanBMS.ViewModels
{
    public class RoleListViewModel
    {
        public string RoleId { get; set; } = string.Empty;

        public string RoleName { get; set; } = string.Empty;

        public int UsersAssigned { get; set; }

        public bool CanDelete => UsersAssigned == 0;
    }
}