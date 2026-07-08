using AryamanBMS.Models;
using AryamanBMS.ViewModels;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUserModel> _signInManager;
        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IAttendanceRepository _attendanceRepository;

        public AccountController(
             SignInManager<ApplicationUserModel> signInManager,
             UserManager<ApplicationUserModel> userManager,
             IEmployeeRepository employeeRepository,
             IAttendanceRepository attendanceRepository)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _employeeRepository = employeeRepository;
            _attendanceRepository = attendanceRepository;
        }

        // ==========================================
        // SIGN IN WORKFLOWS
        // ==========================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
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

            var user = await _userManager.FindByNameAsync(model.UserName);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(
                    "",
                    "Your account is inactive. Please contact the administrator.");

                return View(model);
            }

            // Standard Identity password execution check tracking lockouts defined in Program.cs
            var result = await _signInManager.PasswordSignInAsync(
                 user.UserName ?? model.UserName,
                 model.Password,
                 model.RememberMe,
                 lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Contains("Employee") &&
                    !roles.Contains("Admin") &&
                    !roles.Contains("HR"))
                {
                    var employee =
                        await _employeeRepository.Employees
                            .FirstOrDefaultAsync(e =>
                                e.ApplicationUserId == user.Id);

                    if (employee == null)
                    {
                        return RedirectToAction("Index", "Attendance");
                    }

                    bool attendanceMarked =
                        await _attendanceRepository.Attendances
                            .AnyAsync(a =>
                                a.EmployeeId == employee.Id &&
                                a.AttendanceDate.Date == DateTime.Today);

                    if (!attendanceMarked)
                    {
                        return RedirectToAction("Index", "Attendance");
                    }

                    return RedirectToAction("Profile", "Employee");
                }

                return RedirectToAction("Index", "Dashboard");
            }

            // Lockout message response fallback
            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "This account has been temporarily locked due to multiple failed entry attempts. Please wait 15 minutes.");
                return View(model);
            }

            ModelState.AddModelError("", "Invalid username or password.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
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

            // 4. DEVELOPMENT LOGGING (Simulating the output under your domain context)
            // You can copy this link straight out of your Output window in Visual Studio to test your Reset page!
            Console.WriteLine("\n==========================================================");
            Console.WriteLine($"[ARYAMAN BMS RESET LINK]: {callbackUrl}");
            Console.WriteLine("==========================================================\n");

            // Storing it in TempData so you can print it or access it easily while testing offline
            TempData["DebugResetLink"] = callbackUrl;

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

            return View(user);
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
    }
}