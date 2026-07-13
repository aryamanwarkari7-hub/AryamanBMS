using AryamanBMS.Models;
using Microsoft.AspNetCore.Identity;

namespace AryamanBMS.Middleware
{
    public class UserActivityMiddleware
    {
        private readonly RequestDelegate _next;

        private static readonly TimeSpan UpdateThrottle =
            TimeSpan.FromMinutes(1);

        public UserActivityMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            UserManager<ApplicationUserModel> userManager)
        {
            if (context.User.Identity?.IsAuthenticated == true &&
                !IsStaticFileRequest(context) &&
                !IsHeartbeatRequest(context))
            {
                var user = await userManager.GetUserAsync(context.User);

                if (user != null)
                {
                    var now = DateTime.Now;

                    bool shouldUpdate =
                        !user.LastSeenOn.HasValue ||
                        now - user.LastSeenOn.Value >= UpdateThrottle;

                    if (shouldUpdate)
                    {
                        user.LastSeenOn = now;

                        if (!user.IsActivityStatusManual &&
                            string.Equals(
                                user.ActivityStatus,
                                "Away",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            user.ActivityStatus = "Available";
                            user.ActivityStatusUpdatedOn = now;
                        }

                        await userManager.UpdateAsync(user);
                    }
                }
            }

            await _next(context);
        }

        private static bool IsStaticFileRequest(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            return
                path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/images", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsHeartbeatRequest(HttpContext context)
        {
            return context.Request.Path.StartsWithSegments(
                "/Account/ActivityHeartbeat");
        }
    }
}