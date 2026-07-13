using AryamanBMS.Models;

namespace AryamanBMS.ViewModels
{
    public class NotificationBellViewModel
    {
        public int UnreadCount { get; set; }

        public List<NotificationModel> Notifications { get; set; }
            = new();
    }
}