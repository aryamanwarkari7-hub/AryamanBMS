namespace AryamanBMS.ViewModels
{
    public class UserListViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public string ActivityStatus { get; set; } = "Offline";

        public string? ActivityStatusMessage { get; set; }

        public DateTime? LastSeenOn { get; set; }
    }
}