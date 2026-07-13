using System.Runtime.CompilerServices;
using AryamanBMS.Models;

namespace AryamanBMS.Services.Interfaces
{
    public interface INotificationService
    {
        Task CreateAsync(
            string userId,
            string title,
            string message,
            string notificationType,
            string? referenceType = null,
            int? referenceId = null,
            string? actionUrl = null);

        Task<List<NotificationModel>> GetRecentAsync(string userId,int count = 10);

        Task<List<NotificationModel>> GetAllAsync(string userId);

        Task<NotificationModel?> GetByIdAsync(int notificationId,string userId);

        Task<int> GetUnreadCountAsync(string userId);

        Task<bool> MarkAsReadAsync(int notificationId,string userId);

        Task<int> MarkAllAsReadAsync(string userId);

        Task<bool> ExistsAsync(string userId,string notificationType,string referenceType,int referenceId);
    }
}