using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(
            ApplicationDbContext context)
        {
            _context = context;
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

            _context.TableNotification.Add(notification);

            await _context.SaveChangesAsync();
        }

        public async Task<List<NotificationModel>> GetRecentAsync(string userId,int count = 10)
        {
            if (count <= 0)
            {
                count = 10;
            }

            if (count > 50)
            {
                count = 50;
            }

            return await _context.TableNotification
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedOn)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<NotificationModel>> GetAllAsync(string userId)
        {
            return await _context.TableNotification
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();
        }

        public async Task<NotificationModel?> GetByIdAsync(
            int notificationId,
            string userId)
        {
            return await _context.TableNotification
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == notificationId &&
                    x.UserId == userId);
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.TableNotification
                .AsNoTracking()
                .CountAsync(x =>
                    x.UserId == userId &&
                    !x.IsRead);
        }

        public async Task<bool> MarkAsReadAsync(  int notificationId, string userId)
        {
            var notification =
                await _context.TableNotification
                    .FirstOrDefaultAsync(x =>
                        x.Id == notificationId &&
                        x.UserId == userId);

            if (notification == null)
            {
                return false;
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadOn = DateTime.Now;

                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<int> MarkAllAsReadAsync(
            string userId)
        {
            var notifications =
                await _context.TableNotification
                    .Where(x =>
                        x.UserId == userId &&
                        !x.IsRead)
                    .ToListAsync();

            if (notifications.Count == 0)
            {
                return 0;
            }

            var now = DateTime.Now;

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadOn = now;
            }

            await _context.SaveChangesAsync();

            return notifications.Count;
        }

        public async Task<bool> ExistsAsync(
          string userId,
          string notificationType,
          string referenceType,
          int referenceId)
        {
            return await _context.TableNotification
                .AsNoTracking()
                .AnyAsync(x =>
                    x.UserId == userId &&
                    x.NotificationType == notificationType &&
                    x.ReferenceType == referenceType &&
                    x.ReferenceId == referenceId);
        }
    }
}