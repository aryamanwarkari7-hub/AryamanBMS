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
            Console.WriteLine("[Middleware] 1 - Enter");

            if (context.User.Identity?.IsAuthenticated == true &&
                !IsStaticFileRequest(context) &&
                !IsHeartbeatRequest(context))
            {
                Console.WriteLine("[Middleware] 2 - Authenticated request");

                var user = await userManager.GetUserAsync(context.User);

                Console.WriteLine("[Middleware] 3 - GetUserAsync completed");

                if (user != null)
                {
                    Console.WriteLine("[Middleware] 4 - User found");

                    var now = DateTime.Now;

                    bool shouldUpdate =
                        !user.LastSeenOn.HasValue ||
                        now - user.LastSeenOn.Value >= UpdateThrottle;

                    Console.WriteLine($"[Middleware] 5 - ShouldUpdate = {shouldUpdate}");

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

                        Console.WriteLine("[Middleware] 6 - Before UpdateAsync");

                        var result = await userManager.UpdateAsync(user);

                        Console.WriteLine(
                            $"[Middleware] 7 - After UpdateAsync | Success = {result.Succeeded}");

                        if (!result.Succeeded)
                        {
                            foreach (var error in result.Errors)
                            {
                                Console.WriteLine(
                                    $"[Middleware] Identity Error: {error.Code} - {error.Description}");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("[Middleware] User not found");
                }
            }
            else
            {
                Console.WriteLine("[Middleware] Anonymous or ignored request");
            }

            Console.WriteLine("[Middleware] 8 - Before _next");

            await _next(context);

            Console.WriteLine("[Middleware] 9 - After _next");
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