using AryamanBMS.Models;
using AryamanBMS.Services.Interfaces;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.ViewComponents
{
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUserModel> _userManager;

        public NotificationBellViewComponent(
            INotificationService notificationService,
            UserManager<ApplicationUserModel> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (UserClaimsPrincipal.Identity?.IsAuthenticated != true)
            {
                return Content(string.Empty);
            }

            var user = await _userManager.GetUserAsync(
                UserClaimsPrincipal);

            if (user == null)
            {
                return Content(string.Empty);
            }

            var model = new NotificationBellViewModel
            {
                UnreadCount =
                    await _notificationService
                        .GetUnreadCountAsync(user.Id),

                Notifications =
                    await _notificationService
                        .GetRecentAsync(user.Id, 8)
            };

            return View(model);
        }
    }
}