using AryamanBMS.Data;
using AryamanBMS.Extensions;
using AryamanBMS.Models;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        #region Actions

        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly ApplicationDbContext _context;

        public NotificationController(
            INotificationService notificationService,
            UserManager<ApplicationUserModel> userManager,
            ApplicationDbContext context)
        {
            _notificationService = notificationService;
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? searchText,
            string? notificationType,
            string? readStatus,
            int page = 1)
        {
            const int pageSize = 15;
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }

            var query = _context.TableNotification
                .AsNoTracking()
                .Where(x => x.UserId == user.Id);

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string keyword = searchText.Trim();
                query = query.Where(x =>
                    x.Title.Contains(keyword) ||
                    x.Message.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(notificationType))
            {
                query = query.Where(x => x.NotificationType == notificationType);
            }

            if (readStatus == "Unread")
            {
                query = query.Where(x => !x.IsRead);
            }
            else if (readStatus == "Read")
            {
                query = query.Where(x => x.IsRead);
            }

            query = query
                .OrderByDescending(x => x.CreatedOn)
                .ThenByDescending(x => x.Id);

            var routeValues = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(searchText)) routeValues["searchText"] = searchText.Trim();
            if (!string.IsNullOrWhiteSpace(notificationType)) routeValues["notificationType"] = notificationType;
            if (!string.IsNullOrWhiteSpace(readStatus)) routeValues["readStatus"] = readStatus;

            var paged = await query.ToPagedListAsync(page, pageSize, routeValues);

            ViewBag.SearchText = searchText;
            ViewBag.NotificationType = notificationType;
            ViewBag.ReadStatus = readStatus;
            ViewBag.Page = paged.Pagination.CurrentPage;
            ViewBag.PageSize = paged.Pagination.PageSize;
            ViewBag.TotalRecords = paged.Pagination.TotalRecords;
            ViewBag.NotificationTypes = await _context.TableNotification
                .AsNoTracking()
                .Where(x => x.UserId == user.Id)
                .Select(x => x.NotificationType)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            return View(paged.Items);
        }

        [HttpGet]
        public async Task<IActionResult> Open(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var notification =
                await _notificationService.GetByIdAsync(
                    id,
                    user.Id);

            if (notification == null)
            {
                return NotFound();
            }

            await _notificationService.MarkAsReadAsync(
                notification.Id,
                user.Id);

            if (string.IsNullOrWhiteSpace(notification.ActionUrl))
            {
                return RedirectToAction(nameof(Index));
            }

            if (!Url.IsLocalUrl(notification.ActionUrl))
            {
                return RedirectToAction(nameof(Index));
            }

            return Redirect(notification.ActionUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }

            await _notificationService.MarkAllAsReadAsync(
                user.Id);

            TempData["Success"] =
                "All notifications marked as read.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePreferences(
            bool enableRealtimeNotifications,
            bool enableNotificationToast,
            bool enableNotificationSound,
            string? returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }

            user.EnableRealtimeNotifications =
                enableRealtimeNotifications;

            user.EnableNotificationToast =
                enableNotificationToast;

            user.EnableNotificationSound =
                enableNotificationSound;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["Error"] =
                    "Notification preferences could not be updated.";

                return RedirectToLocal(returnUrl);
            }

            TempData["Success"] =
                "Notification preferences updated successfully.";

            return RedirectToLocal(returnUrl);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Audit(
            string? searchText,
            string? notificationType,
            string? readStatus)
        {
            var query =
                _context.TableNotification
                    .AsNoTracking()
                    .Include(x => x.User)
                    .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var keyword = searchText.Trim();

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

            ViewBag.SearchText = searchText;
            ViewBag.NotificationType = notificationType;
            ViewBag.ReadStatus = readStatus;

            ViewBag.NotificationTypes =
                await _context.TableNotification
                    .AsNoTracking()
                    .Select(x => x.NotificationType)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync();

            var notifications =
                await query
                    .OrderByDescending(x => x.CreatedOn)
                    .Take(300)
                    .ToListAsync();

            return View(notifications);
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }
        #endregion
    }
}
