using Microsoft.AspNetCore.Identity;

namespace AryamanBMS.Models
{
    public class ApplicationUserModel : IdentityUser
    {
        public string? FullName { get; set; }

        public bool IsActive { get; set; } = true;

        public string? ProfilePhotoPath { get; set; }

        public string ActivityStatus { get; set; } = "Offline";
        public string? ActivityStatusMessage { get; set; }
        public DateTime? ActivityStatusUpdatedOn { get; set; }

        public DateTime? LastSeenOn { get; set; }

        public bool IsActivityStatusManual { get; set; } = false;

        public bool EnableRealtimeNotifications { get; set; } = true;

        public bool EnableNotificationToast { get; set; } = true;

        public bool EnableNotificationSound { get; set; } = false;
    }
}
