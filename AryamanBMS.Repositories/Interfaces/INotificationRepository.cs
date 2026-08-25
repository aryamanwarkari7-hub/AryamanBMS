using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        Task AddAsync(NotificationModel notification);

        Task<bool> IsRealtimeNotificationsEnabledAsync(string userId);

        Task<List<NotificationModel>> GetRecentAsync(
            string userId,
            int count);

        Task<List<NotificationModel>> GetAllAsync(string userId);

        Task<NotificationModel?> GetByIdAsync(
            int notificationId,
            string userId);

        Task<int> GetUnreadCountAsync(string userId);

        Task<bool> MarkAsReadAsync(
            int notificationId,
            string userId);

        Task<int> MarkAllAsReadAsync(string userId);

        Task<bool> ExistsAsync(
            string userId,
            string notificationType,
            string referenceType,
            int referenceId);

        Task<(List<NotificationModel> Records, int TotalRecords)>
            SearchForUserAsync(
                string userId,
                string? searchText,
                string? notificationType,
                string? readStatus,
                int page,
                int pageSize);

        Task<List<string>> GetNotificationTypesAsync(
            string? userId = null);

        Task<List<NotificationModel>> SearchAuditAsync(
            string? searchText,
            string? notificationType,
            string? readStatus,
            int maximumRecords = 300);
    }
}