using AryamanBMS.Hubs;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace AryamanBMS.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository
            _notificationRepository;

        private readonly IHubContext<NotificationHub>
            _notificationHub;

        public NotificationService(
            INotificationRepository notificationRepository,
            IHubContext<NotificationHub> notificationHub)
        {
            _notificationRepository =
                notificationRepository;

            _notificationHub = notificationHub;
        }

        public async Task CreateAsync(
            string userId,
            string title,
            string message,
            string notificationType,
            string? referenceType = null,
            int? referenceId = null,
            string? actionUrl = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException(
                    "Recipient user ID is required.",
                    nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException(
                    "Notification title is required.",
                    nameof(title));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException(
                    "Notification message is required.",
                    nameof(message));
            }

            var notification = new NotificationModel
            {
                UserId = userId,
                Title = title.Trim(),
                Message = message.Trim(),

                NotificationType =
                    string.IsNullOrWhiteSpace(notificationType)
                        ? "System"
                        : notificationType.Trim(),

                ReferenceType =
                    string.IsNullOrWhiteSpace(referenceType)
                        ? null
                        : referenceType.Trim(),

                ReferenceId = referenceId,

                ActionUrl =
                    string.IsNullOrWhiteSpace(actionUrl)
                        ? null
                        : actionUrl.Trim(),

                IsRead = false,
                CreatedOn = DateTime.Now
            };

            await _notificationRepository.AddAsync(notification);

            bool realtimeNotificationsEnabled =
                await _notificationRepository
                    .IsRealtimeNotificationsEnabledAsync(userId);

            if (!realtimeNotificationsEnabled)
            {
                return;
            }

            int unreadCount = await GetUnreadCountAsync(userId);

            await _notificationHub.Clients
                .User(userId)
                .SendAsync(
                    "ReceiveNotification",
                    new
                    {
                        notification.Id,
                        notification.Title,
                        notification.Message,
                        notification.NotificationType,
                        notification.ActionUrl,
                        CreatedOn =
                            notification.CreatedOn.ToString(
                                "dd MMM yyyy, hh:mm tt"),
                        UnreadCount = unreadCount
                    });
        }

        public async Task<List<NotificationModel>>
            GetRecentAsync(
                string userId,
                int count = 10)
        {
            if (count <= 0)
            {
                count = 10;
            }

            if (count > 50)
            {
                count = 50;
            }

            return await _notificationRepository
                .GetRecentAsync(userId, count);
        }

        public async Task<List<NotificationModel>>
            GetAllAsync(string userId)
        {
            return await _notificationRepository
                .GetAllAsync(userId);
        }

        public async Task<NotificationModel?> GetByIdAsync(
            int notificationId,
            string userId)
        {
            return await _notificationRepository
                .GetByIdAsync(notificationId, userId);
        }

        public async Task<int> GetUnreadCountAsync(
            string userId)
        {
            return await _notificationRepository
                .GetUnreadCountAsync(userId);
        }

        public async Task<bool> MarkAsReadAsync(
            int notificationId,
            string userId)
        {
            return await _notificationRepository
                .MarkAsReadAsync(notificationId, userId);
        }

        public async Task<int> MarkAllAsReadAsync(
            string userId)
        {
            return await _notificationRepository
                .MarkAllAsReadAsync(userId);
        }

        public async Task<bool> ExistsAsync(
            string userId,
            string notificationType,
            string referenceType,
            int referenceId)
        {
            return await _notificationRepository
                .ExistsAsync(
                    userId,
                    notificationType,
                    referenceType,
                    referenceId);
        }

        public async Task<(List<NotificationModel> Records, int TotalRecords)>
    SearchForUserAsync(
        string userId,
        string? searchText,
        string? notificationType,
        string? readStatus,
        int page,
        int pageSize)
        {
            return await _notificationRepository.SearchForUserAsync(
                userId,
                searchText,
                notificationType,
                readStatus,
                page,
                pageSize);
        }

        public async Task<List<string>> GetNotificationTypesAsync(
            string? userId = null)
        {
            return await _notificationRepository
                .GetNotificationTypesAsync(userId);
        }

        public async Task<List<NotificationModel>> SearchAuditAsync(
            string? searchText,
            string? notificationType,
            string? readStatus,
            int maximumRecords = 300)
        {
            return await _notificationRepository.SearchAuditAsync(
                searchText,
                notificationType,
                readStatus,
                maximumRecords);
        }
    }
}