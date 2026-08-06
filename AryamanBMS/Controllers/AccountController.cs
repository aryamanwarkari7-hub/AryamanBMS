using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AryamanBMS.Data;

namespace AryamanBMS.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUserModel> _signInManager;
        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly INotificationService _notificationService;
        private readonly ILogger<AccountController> _logger;
        private readonly ILoginHistoryService _loginHistoryService;
        private readonly ApplicationDbContext _context;

        public AccountController(
            SignInManager<ApplicationUserModel> signInManager,
            UserManager<ApplicationUserModel> userManager,
            IEmployeeRepository employeeRepository,
            IAttendanceRepository attendanceRepository,
            IWebHostEnvironment webHostEnvironment,
            INotificationService notificationService,
            ILogger<AccountController> logger,
            ILoginHistoryService loginHistoryService,
            ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _employeeRepository = employeeRepository;
            _attendanceRepository = attendanceRepository;
            _webHostEnvironment = webHostEnvironment;
            _notificationService = notificationService;
            _logger = logger;
            _loginHistoryService = loginHistoryService;
            _context = context;
        }

        // ==========================================
        // SIGN IN WORKFLOWS
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);

                if (user != null)
                {
                    if (string.IsNullOrWhiteSpace(user.ActivityStatus) ||
                        user.ActivityStatus == "Offline")
                    {
                        await SetActivityStatusAsync(
                           user,
                           "Available",
                           message: null,
                           isManual: false);
                    }

                    return await RedirectByRoleAsync(user);
                }

                await _signInManager.SignOutAsync();
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string attemptedUserName =
                model.UserName?.Trim() ?? string.Empty;

            var user =
                await _userManager.FindByNameAsync(attemptedUserName);

            if (user == null)
            {
                await RecordLoginHistorySafeAsync(
                    attemptedUserName: attemptedUserName,
                    eventType: "UnknownUser",
                    isSuccessful: false,
                    failureReason: "No user account matched the supplied username.");

                ModelState.AddModelError(
                    "",
                    "Invalid username or password.");

                return View(model);
            }

            if (!user.IsActive)
            {
                await RecordLoginHistorySafeAsync(
                    attemptedUserName: attemptedUserName,
                    eventType: "InactiveAccount",
                    isSuccessful: false,
                    userId: user.Id,
                    failureReason: "Login attempted using an inactive account.");

                ModelState.AddModelError(
                    "",
                    "Your account is inactive. Please contact the administrator.");

                return View(model);
            }

            var result =
                await _signInManager.PasswordSignInAsync(
                    user.UserName ?? attemptedUserName,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: true);

            if (result.Succeeded)
            {
                bool alreadyLoggedToday =
                    await _loginHistoryService
                        .HasSuccessfulLoginTodayAsync(user.Id);

                if (!alreadyLoggedToday)
                {
                    await RecordLoginHistorySafeAsync(
                        attemptedUserName:
                            user.UserName ?? attemptedUserName,
                        eventType: "Login",
                        isSuccessful: true,
                        userId: user.Id);

                    await NotifyAdminsOfLoginAsync(user);
                }

                await SetActivityStatusAsync(
                    user,
                    "Available",
                    message: null,
                    isManual: false);

                return await RedirectByRoleAsync(user);
            }

            if (result.IsLockedOut)
            {
                await RecordLoginHistorySafeAsync(
                    attemptedUserName: attemptedUserName,
                    eventType: "AccountLocked",
                    isSuccessful: false,
                    userId: user.Id,
                    failureReason:
                        "Account locked after repeated failed login attempts.");

                ModelState.AddModelError(
                    "",
                    "This account has been temporarily locked due to multiple failed entry attempts. Please wait 15 minutes.");

                return View(model);
            }

            await RecordLoginHistorySafeAsync(
                attemptedUserName: attemptedUserName,
                eventType: "InvalidPassword",
                isSuccessful: false,
                userId: user.Id,
                failureReason: "Invalid password supplied.");

            ModelState.AddModelError(
                "",
                "Invalid username or password.");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                await SetActivityStatusAsync(user, "Offline");
            }

            await _signInManager.SignOutAsync();

            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ==========================================
        // PASSWORD RECOVERY WORKFLOWS
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 1. Locate the employee account within your system registry
            var user = await _userManager.FindByEmailAsync(model.Email);

            // Security Check: Guarding against unauthorized access or inactive employee profiles
            if (user == null || !user.IsActive)
            {
                TempData["SuccessMessage"] = "If an active workspace profile is linked, verification tracking instructions have been generated.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            // 2. Generate the single-use cryptographic security token via Identity Manager
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // 3. Associate it directly with your custom ResetPassword action
            // This perfectly binds to your ResetPasswordViewModel properties when the link is clicked
            var callbackUrl = Url.Action("ResetPassword", "Account",
                new { userId = user.Id, token = token },
                protocol: Request.Scheme);

            if (_webHostEnvironment.IsDevelopment())
            {
                _logger.LogInformation(
                    "ARYAMAN BMS development reset link generated: {ResetLink}",
                    callbackUrl);

                TempData["DebugResetLink"] = callbackUrl;
            }

            TempData["SuccessMessage"] = "If an active workspace profile is linked, verification tracking instructions have been generated.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        // ==========================================
        // REVENUE CONTROL LOGGED PROFILES
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var roles = await _userManager.GetRolesAsync(user);

            var employee = await _employeeRepository.Employees
                .AsNoTracking()
                .Include(x => x.Department)
                .Include(x => x.Designation)
                .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);

            string displayName =
                employee?.FullName ??
                user.FullName ??
                user.UserName ??
                "User";

            var model = new AccountProfileViewModel
            {
                User = user,
                Employee = employee,
                RoleName = roles.FirstOrDefault() ?? "-",
                Initials = BuildInitials(displayName)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadProfilePhoto(IFormFile? profilePhoto, string? croppedProfilePhoto)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            if (!string.IsNullOrWhiteSpace(croppedProfilePhoto))
            {
                var commaIndex = croppedProfilePhoto.IndexOf(',');
                string? declaredContentType = null;

                if (commaIndex >= 0)
                {
                    string metadata =
                        croppedProfilePhoto[..commaIndex];

                    if (metadata.StartsWith(
                        "data:",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        int separatorIndex = metadata.IndexOf(';');

                        if (separatorIndex > 5)
                        {
                            declaredContentType =
                                metadata[5..separatorIndex];
                        }
                    }

                    croppedProfilePhoto = croppedProfilePhoto[(commaIndex + 1)..];
                }

                byte[] bytes;

                try
                {
                    bytes = Convert.FromBase64String(croppedProfilePhoto);
                }
                catch (FormatException)
                {
                    TempData["Error"] =
                        "Profile photo data is invalid.";

                    return RedirectToAction(nameof(Profile));
                }

                const long croppedMaxBytes = 2 * 1024 * 1024;

                if (bytes.Length > croppedMaxBytes)
                {
                    TempData["Error"] = "Profile photo must be 2 MB or smaller.";
                    return RedirectToAction(nameof(Profile));
                }

                if (!TryGetAllowedImageType(
                    bytes,
                    out string croppedExtension,
                    out string croppedContentType) ||
                    (!string.IsNullOrWhiteSpace(declaredContentType) &&
                     !string.Equals(
                         declaredContentType,
                         croppedContentType,
                         StringComparison.OrdinalIgnoreCase)))
                {
                    TempData["Error"] =
                        "Only valid JPG, PNG or WEBP profile photos are allowed.";

                    return RedirectToAction(nameof(Profile));
                }

                string croppedFolderPath = Path.Combine(
                    _webHostEnvironment.WebRootPath,
                    "uploads",
                    "profile-photos");

                Directory.CreateDirectory(croppedFolderPath);

                string croppedFileName = $"{user.Id}{croppedExtension}";
                string croppedFullPath = Path.Combine(
                    croppedFolderPath,
                    croppedFileName);

                DeleteExistingProfilePhotoFiles(
                    croppedFolderPath,
                    user.Id);

                await System.IO.File.WriteAllBytesAsync(croppedFullPath, bytes);

                user.ProfilePhotoPath =
                    $"/uploads/profile-photos/{croppedFileName}";

                var croppedResult = await _userManager.UpdateAsync(user);

                if (!croppedResult.Succeeded)
                {
                    TempData["Error"] = "Profile photo could not be updated.";
                    return RedirectToAction(nameof(Profile));
                }

                TempData["Success"] = "Profile photo updated successfully.";
                return RedirectToAction(nameof(Profile));
            }

            if (profilePhoto == null || profilePhoto.Length == 0)
            {
                TempData["Error"] = "Please select a profile photo.";
                return RedirectToAction(nameof(Profile));
            }

            const long maxBytes = 2 * 1024 * 1024;

            if (profilePhoto.Length > maxBytes)
            {
                TempData["Error"] = "Profile photo must be 2 MB or smaller.";
                return RedirectToAction(nameof(Profile));
            }

            string extension = Path.GetExtension(profilePhoto.FileName).ToLowerInvariant();

            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "Only JPG, PNG or WEBP profile photos are allowed.";
                return RedirectToAction(nameof(Profile));
            }

            byte[] uploadedBytes;

            await using (var memoryStream = new MemoryStream())
            {
                await profilePhoto.CopyToAsync(memoryStream);
                uploadedBytes = memoryStream.ToArray();
            }

            if (!TryGetAllowedImageType(
                uploadedBytes,
                out string detectedExtension,
                out string detectedContentType) ||
                !string.Equals(
                    profilePhoto.ContentType,
                    detectedContentType,
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Uploaded profile photo content is not valid.";

                return RedirectToAction(nameof(Profile));
            }

            string folderPath = Path.Combine(
                _webHostEnvironment.WebRootPath,
                "uploads",
                "profile-photos");

            Directory.CreateDirectory(folderPath);

            string fileName = $"{user.Id}{detectedExtension}";

            string fullPath = Path.Combine(folderPath, fileName);

            DeleteExistingProfilePhotoFiles(
                folderPath,
                user.Id);

            await System.IO.File.WriteAllBytesAsync(
                fullPath,
                uploadedBytes);

            user.ProfilePhotoPath = $"/uploads/profile-photos/{fileName}";

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["Error"] = "Profile photo could not be updated.";
                return RedirectToAction(nameof(Profile));
            }

            TempData["Success"] = "Profile photo updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        private static bool TryGetAllowedImageType(
            byte[] bytes,
            out string extension,
            out string contentType)
        {
            extension = string.Empty;
            contentType = string.Empty;

            if (bytes.Length >= 3 &&
                bytes[0] == 0xFF &&
                bytes[1] == 0xD8 &&
                bytes[2] == 0xFF)
            {
                extension = ".jpg";
                contentType = "image/jpeg";
                return true;
            }

            if (bytes.Length >= 8 &&
                bytes[0] == 0x89 &&
                bytes[1] == 0x50 &&
                bytes[2] == 0x4E &&
                bytes[3] == 0x47 &&
                bytes[4] == 0x0D &&
                bytes[5] == 0x0A &&
                bytes[6] == 0x1A &&
                bytes[7] == 0x0A)
            {
                extension = ".png";
                contentType = "image/png";
                return true;
            }

            if (bytes.Length >= 12 &&
                bytes[0] == 0x52 &&
                bytes[1] == 0x49 &&
                bytes[2] == 0x46 &&
                bytes[3] == 0x46 &&
                bytes[8] == 0x57 &&
                bytes[9] == 0x45 &&
                bytes[10] == 0x42 &&
                bytes[11] == 0x50)
            {
                extension = ".webp";
                contentType = "image/webp";
                return true;
            }

            return false;
        }

        private static void DeleteExistingProfilePhotoFiles(
            string folderPath,
            string userId)
        {
            foreach (string extension in new[] { ".jpg", ".jpeg", ".png", ".webp" })
            {
                string existingPath =
                    Path.Combine(
                        folderPath,
                        $"{userId}{extension}");

                if (System.IO.File.Exists(existingPath))
                {
                    System.IO.File.Delete(existingPath);
                }
            }
        }

        private static string BuildInitials(string name)
        {
            return string.Join(
                "",
                name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Take(2)
                    .Select(x => x[0]))
                .ToUpperInvariant();
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ChangePasswordAsync(
                user!,
                model.CurrentPassword,
                model.NewPassword);

            if (result.Succeeded)
            {
                await LogPasswordChangeAsync(
                    user,
                    user,
                    "SelfChange");

                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                var hrUsers = await _userManager.GetUsersInRoleAsync("HR");

                var recipients = admins
                    .Concat(hrUsers)
                    .Where(x => x.IsActive)
                    .GroupBy(x => x.Id)
                    .Select(x => x.First());

                foreach (var recipient in recipients)
                {
                    // Don't notify the user who changed their own password.
                    if (recipient.Id == user.Id)
                    {
                        continue;
                    }

                    await _notificationService.CreateAsync(
                        recipient.Id,
                        "Password Changed",
                        $"{user.FullName ?? user.UserName} changed their account password.",
                        "Security",
                        "PasswordChangeLog",
                        null,
                        Url.Action("Index", "PasswordChangeLog"));
                }

                TempData["Success"] =
                    "Password changed successfully.";

                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        // ==========================================
        // HANDSHAKE VERIFICATION & PASSWORD RESET
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                ModelState.AddModelError("", "Invalid or expired security validation credentials.");
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Populate your existing model properties along with the security token
            var model = new ResetPasswordViewModel
            {
                UserId = userId,
                Token = token,
                UserName = user.UserName ?? string.Empty
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 1. Retrieve the target user using the hidden UserId field
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                // Security best practice: Redirect to login with a vague success message 
                // to avoid disclosing user presence to enumeration bots.
                TempData["Success"] = "Your system security credentials have been updated.";
                return RedirectToAction(nameof(Login));
            }

            // 2. Perform the secure reset using the single-use token and the new password
            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (result.Succeeded)
            {
                // 3. Clear any lockout restrictions if the user was locked out due to prior failed log-ins
                await _userManager.ResetAccessFailedCountAsync(user);

                TempData["Success"] = "Your password has been successfully reset. Please log in with your new credentials.";
                return RedirectToAction(nameof(Login));
            }

            // 4. If Identity password policies fail (e.g., password too short), append errors to UI
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }


        #region Helpers
        private async Task<IActionResult> RedirectByRoleAsync(ApplicationUserModel user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Employee") &&
                !roles.Contains("Admin") &&
                !roles.Contains("HR"))
            {
                var employee = await _employeeRepository.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);

                if (employee == null)
                {
                    return RedirectToAction("Index", "Attendance");
                }

                bool attendanceMarked = await _attendanceRepository.Attendances
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.EmployeeId == employee.Id &&
                        x.AttendanceDate.Date == DateTime.Today);

                if (!attendanceMarked)
                {
                    return RedirectToAction("Index", "Attendance");
                }

                return RedirectToAction("MyDashboard", "Employee");
            }

            return RedirectToAction("Index", "Dashboard");
        }

        private static readonly HashSet<string> AllowedActivityStatuses =
          new(StringComparer.OrdinalIgnoreCase)
    {
        "Available",
        "Busy",
        "Away",
        "Do Not Disturb",
        "Offline"
    };

        private async Task SetActivityStatusAsync(
          ApplicationUserModel user,
          string status,
          string? message = null,
          bool isManual = false)
        {
            if (!AllowedActivityStatuses.Contains(status))
            {
                status = "Available";
                isManual = false;
            }

            bool allowsStatusMessage =
                status.Equals(
                    "Busy",
                    StringComparison.OrdinalIgnoreCase) ||
                status.Equals(
                    "Do Not Disturb",
                    StringComparison.OrdinalIgnoreCase);

            var now = DateTime.Now;

            user.ActivityStatus = status;

            user.ActivityStatusMessage =
                allowsStatusMessage &&
                !string.IsNullOrWhiteSpace(message)
                    ? message.Trim()
                    : null;

            user.ActivityStatusUpdatedOn = now;
            user.LastSeenOn = now;
            user.IsActivityStatusManual = isManual;

            await _userManager.UpdateAsync(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateActivityStatus(
            string activityStatus,
            string? activityStatusMessage,
            string? returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            if (!AllowedActivityStatuses.Contains(activityStatus))
            {
                TempData["Error"] = "Invalid activity status.";
                return RedirectToAction(nameof(Profile));
            }

            bool isManual =
                activityStatus.Equals(
                    "Busy",
                    StringComparison.OrdinalIgnoreCase) ||
                activityStatus.Equals(
                    "Do Not Disturb",
                    StringComparison.OrdinalIgnoreCase);

            await SetActivityStatusAsync(
                user,
                activityStatus,
                activityStatusMessage,
                isManual);

            TempData["Success"] = "Activity status updated.";

            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivityHeartbeat(bool isActive)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }

            var now = DateTime.Now;

            user.LastSeenOn = now;

            if (!user.IsActivityStatusManual &&
                !string.Equals(
                    user.ActivityStatus,
                    "Offline",
                    StringComparison.OrdinalIgnoreCase))
            {
                string requiredStatus = isActive
                    ? "Available"
                    : "Away";

                if (!string.Equals(
                    user.ActivityStatus,
                    requiredStatus,
                    StringComparison.OrdinalIgnoreCase))
                {
                    user.ActivityStatus = requiredStatus;
                    user.ActivityStatusMessage = null;
                    user.ActivityStatusUpdatedOn = now;
                }
            }

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message = "Activity status could not be updated."
                    });
            }

            return Ok(new
            {
                status = user.ActivityStatus,
                lastSeenOn = user.LastSeenOn,
                isManual = user.IsActivityStatusManual
            });
        }

        private async Task NotifyAdminsOfLoginAsync(
    ApplicationUserModel loggedInUser)
        {
            try
            {
                var admins =
                    await _userManager.GetUsersInRoleAsync("Admin");

                string displayName =
                    !string.IsNullOrWhiteSpace(loggedInUser.FullName)
                        ? loggedInUser.FullName
                        : loggedInUser.UserName ?? "User";

                string ipAddress =
                    HttpContext.Connection.RemoteIpAddress?
                        .MapToIPv4()
                        .ToString()
                    ?? "Unknown IP";

                string loginTime =
                    DateTime.Now.ToString("dd MMM yyyy, hh:mm tt");

                foreach (var admin in admins)
                {
                    if (!admin.IsActive)
                    {
                        continue;
                    }

                    // Do not notify an Admin about their own login.
                    if (admin.Id == loggedInUser.Id)
                    {
                        continue;
                    }

                    await _notificationService.CreateAsync(
                        userId: admin.Id,
                        title: "User Logged In",
                        message:
                            $"{displayName} logged in on " +
                            $"{loginTime} from {ipAddress}.",
                        notificationType: "Login",
                        referenceType: "ApplicationUser",
                        referenceId: null,
                        actionUrl: "/User/Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Admin login notification failed for user {UserId}.",
                    loggedInUser.Id);
            }
        }

        private async Task RecordLoginHistorySafeAsync(
            string attemptedUserName,
            string eventType,
            bool isSuccessful,
            string? userId = null,
            string? failureReason = null)
        {
            try
            {
                string ipAddress =
                    HttpContext.Connection.RemoteIpAddress?
                        .MapToIPv4()
                        .ToString()
                    ?? "Unknown";

                string userAgent =
                    Request.Headers.UserAgent.ToString();

                if (userAgent.Length > 500)
                {
                    userAgent = userAgent[..500];
                }

                await _loginHistoryService.RecordAsync(
                    attemptedUserName: attemptedUserName,
                    eventType: eventType,
                    isSuccessful: isSuccessful,
                    userId: userId,
                    failureReason: failureReason,
                    ipAddress: ipAddress,
                    userAgent: userAgent);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Login history could not be recorded for {UserName}.",
                    attemptedUserName);
            }
        }

        private async Task LogPasswordChangeAsync(
    ApplicationUserModel targetUser,
    ApplicationUserModel? changedByUser,
    string changeType)
        {
            _context.PasswordChangeLogs.Add(new PasswordChangeLogModel
            {
                UserId = targetUser.Id,
                UserName = targetUser.UserName,
                Email = targetUser.Email,
                ChangedByUserId = changedByUser?.Id,
                ChangedByUserName = changedByUser?.UserName,
                ChangeType = changeType,
                ChangedOn = DateTime.Now,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            });

            await _context.SaveChangesAsync();
        }

        #endregion
    }

}
