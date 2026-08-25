using AryamanBMS.Database.Context;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories.Implementations
{
    public class NotificationRepository
        : INotificationRepository
    {
        private readonly NotificationDbContext _context;

        public NotificationRepository(
            NotificationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(
            NotificationModel notification)
        {
            await _context.TableNotification.AddAsync(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<bool>
            IsRealtimeNotificationsEnabledAsync(
                string userId)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(x => x.Id == userId)
                .Select(x => x.EnableRealtimeNotifications)
                .FirstOrDefaultAsync();
        }

        public async Task<List<NotificationModel>>
            GetRecentAsync(
                string userId,
                int count)
        {
            return await _context.TableNotification
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedOn)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<NotificationModel>>
            GetAllAsync(string userId)
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

        public async Task<int> GetUnreadCountAsync(
            string userId)
        {
            return await _context.TableNotification
                .AsNoTracking()
                .CountAsync(x =>
                    x.UserId == userId &&
                    !x.IsRead);
        }

        public async Task<bool> MarkAsReadAsync(
            int notificationId,
            string userId)
        {
            var notification = await _context.TableNotification
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
            var notifications = await _context.TableNotification
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

                public async Task<(List<NotificationModel> Records, int TotalRecords)>
            SearchForUserAsync(
                string userId,
                string? searchText,
                string? notificationType,
                string? readStatus,
                int page,
                int pageSize)
        {
            if (page < 1)
            {
                page = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 15;
            }

            var query = _context.TableNotification
                .AsNoTracking()
                .Where(x => x.UserId == userId);

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string keyword = searchText.Trim();

                query = query.Where(x =>
                    x.Title.Contains(keyword) ||
                    x.Message.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(notificationType))
            {
                query = query.Where(x =>
                    x.NotificationType == notificationType);
            }

            if (readStatus == "Unread")
            {
                query = query.Where(x => !x.IsRead);
            }
            else if (readStatus == "Read")
            {
                query = query.Where(x => x.IsRead);
            }

            int totalRecords = await query.CountAsync();

            int totalPages = totalRecords == 0
                ? 1
                : (int)Math.Ceiling(
                    totalRecords / (double)pageSize);

            if (page > totalPages)
            {
                page = totalPages;
            }

            var records = await query
                .OrderByDescending(x => x.CreatedOn)
                .ThenByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (records, totalRecords);
        }

        public async Task<List<string>> GetNotificationTypesAsync(
            string? userId = null)
        {
            var query = _context.TableNotification
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(userId))
            {
                query = query.Where(x => x.UserId == userId);
            }

            return await query
                .Select(x => x.NotificationType)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
        }

        public async Task<List<NotificationModel>> SearchAuditAsync(
            string? searchText,
            string? notificationType,
            string? readStatus,
            int maximumRecords = 300)
        {
            if (maximumRecords < 1)
            {
                maximumRecords = 300;
            }

            var query = _context.TableNotification
                .AsNoTracking()
                .Include(x => x.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string keyword = searchText.Trim();

                query = query.Where(x =>
                    x.Title.Contains(keyword) ||
                    x.Message.Contains(keyword) ||
                    x.UserId.Contains(keyword) ||
                    (x.User != null &&
                     ((x.User.FullName ?? string.Empty).Contains(keyword) ||
                      (x.User.Email ?? string.Empty).Contains(keyword))));
            }

            if (!string.IsNullOrWhiteSpace(notificationType))
            {
                query = query.Where(x =>
                    x.NotificationType == notificationType);
            }

            if (readStatus == "Unread")
            {
                query = query.Where(x => !x.IsRead);
            }
            else if (readStatus == "Read")
            {
                query = query.Where(x => x.IsRead);
            }

            return await query
                .OrderByDescending(x => x.CreatedOn)
                .Take(maximumRecords)
                .ToListAsync();
        }
    }
}