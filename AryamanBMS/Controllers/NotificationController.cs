using AryamanBMS.Models;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUserModel> _userManager;

        public NotificationController(
            INotificationService notificationService,
            UserManager<ApplicationUserModel> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }

            var notifications =
                await _notificationService.GetAllAsync(user.Id);

            return View(notifications);
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
    }
}