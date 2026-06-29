using AryamanBMS.Models;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoleController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUserModel> _userManager;

        public RoleController(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUserModel> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? status)
        {
            var roles = _roleManager.Roles
                .Where(role => role.Name != null)
                .OrderBy(role => role.Name)
                .ToList();

            var roleList = new List<RoleListViewModel>();

            foreach (var role in roles)
            {
                var usersInRole =
                    await _userManager.GetUsersInRoleAsync(role.Name!);

                roleList.Add(new RoleListViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name ?? string.Empty,
                    UsersAssigned = usersInRole.Count
                });
            }

            if (status == "Active")
            {
                roleList = roleList
                    .Where(role => role.UsersAssigned > 0)
                    .ToList();
            }
            else if (status == "Inactive")
            {
                roleList = roleList
                    .Where(role => role.UsersAssigned == 0)
                    .ToList();
            }

            ViewBag.Status = status;

            return View(roleList);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                ModelState.AddModelError(
                    "",
                    "Role name is required.");

                return View();
            }

            roleName = roleName.Trim();

            bool exists =
                await _roleManager.RoleExistsAsync(roleName);

            if (exists)
            {
                ModelState.AddModelError(
                    "",
                    "Role already exists.");

                return View();
            }

            var result =
                await _roleManager.CreateAsync(
                    new IdentityRole(roleName));

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                return View();
            }

            TempData["Success"] =
                "Role created successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var role =
                await _roleManager.FindByIdAsync(id);

            if (role == null)
            {
                return NotFound();
            }

            var protectedRoles = new[]
            {
                "Admin",
                "HR",
                "Employee"
            };

            if (protectedRoles.Contains(
                role.Name,
                StringComparer.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "This role is protected and cannot be deleted.";

                return RedirectToAction(nameof(Index));
            }

            var usersInRole =
                await _userManager.GetUsersInRoleAsync(
                    role.Name ?? string.Empty);

            if (usersInRole.Any())
            {
                TempData["Error"] =
                    $"Cannot delete the role because " +
                    $"{usersInRole.Count} user(s) are assigned to it.";

                return RedirectToAction(nameof(Index));
            }

            var result =
                await _roleManager.DeleteAsync(role);

            if (!result.Succeeded)
            {
                TempData["Error"] =
                    "Role could not be deleted.";

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] =
                "Role deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}