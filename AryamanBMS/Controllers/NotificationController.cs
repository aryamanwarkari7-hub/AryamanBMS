
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


        public NotificationController(
            INotificationService notificationService,
            UserManager<ApplicationUserModel> userManager
           )
        {
            _notificationService = notificationService;
            _userManager = userManager;

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

            var result = await _notificationService
                .SearchForUserAsync(
                    user.Id,
                    searchText,
                    notificationType,
                    readStatus,
                    page,
                    pageSize);

            int totalPages = result.TotalRecords == 0
                ? 1
                : (int)Math.Ceiling(
                    result.TotalRecords / (double)pageSize);

            page = Math.Clamp(page, 1, totalPages);

            ViewBag.SearchText = searchText;
            ViewBag.NotificationType = notificationType;
            ViewBag.ReadStatus = readStatus;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRecords = result.TotalRecords;
            ViewBag.NotificationTypes =
                await _notificationService
                    .GetNotificationTypesAsync(user.Id);

            return View(result.Records);
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
            ViewBag.SearchText = searchText;
            ViewBag.NotificationType = notificationType;
            ViewBag.ReadStatus = readStatus;

            ViewBag.NotificationTypes =
                await _notificationService
                    .GetNotificationTypesAsync();

            var notifications = await _notificationService
                .SearchAuditAsync(
                    searchText,
                    notificationType,
                    readStatus);

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
