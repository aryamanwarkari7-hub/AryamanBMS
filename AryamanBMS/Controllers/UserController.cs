using AryamanBMS.Extensions;
using AryamanBMS.Models;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(
            UserManager<ApplicationUserModel> userManager,
        RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index(
    string? searchText,
    string sortBy = "NameAsc",
    int page = 1)
        {
            const int pageSize = 10;

            var users = await _userManager.Users
                .AsNoTracking()
                .ToListAsync();

            var userList = new List<UserListViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                userList.Add(new UserListViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "Not Assigned",
                    IsActive = user.IsActive
                });
            }

            // Search
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                searchText = searchText.Trim();

                userList = userList
                    .Where(user =>
                        user.FullName.Contains(
                            searchText,
                            StringComparison.OrdinalIgnoreCase) ||

                        user.UserName.Contains(
                            searchText,
                            StringComparison.OrdinalIgnoreCase) ||

                        user.Email.Contains(
                            searchText,
                            StringComparison.OrdinalIgnoreCase) ||

                        user.Role.Contains(
                            searchText,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Sort
            userList = sortBy switch
            {
                "NameDesc" => userList
                    .OrderByDescending(user => user.FullName)
                    .ThenBy(user => user.UserName)
                    .ToList(),

                "UserNameAsc" => userList
                    .OrderBy(user => user.UserName)
                    .ToList(),

                "UserNameDesc" => userList
                    .OrderByDescending(user => user.UserName)
                    .ToList(),

                "EmailAsc" => userList
                    .OrderBy(user => user.Email)
                    .ToList(),

                "EmailDesc" => userList
                    .OrderByDescending(user => user.Email)
                    .ToList(),

                "RoleAsc" => userList
                    .OrderBy(user => user.Role)
                    .ThenBy(user => user.FullName)
                    .ToList(),

                "RoleDesc" => userList
                    .OrderByDescending(user => user.Role)
                    .ThenBy(user => user.FullName)
                    .ToList(),

                "StatusAsc" => userList
                    .OrderByDescending(user => user.IsActive)
                    .ThenBy(user => user.FullName)
                    .ToList(),

                "StatusDesc" => userList
                    .OrderBy(user => user.IsActive)
                    .ThenBy(user => user.FullName)
                    .ToList(),

                _ => userList
                    .OrderBy(user => user.FullName)
                    .ThenBy(user => user.UserName)
                    .ToList()
            };

            int totalRecords = userList.Count;

            int totalPages = (int)Math.Ceiling(
                totalRecords / (double)pageSize);

            page = page < 1 ? 1 : page;

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var pagedUsers = userList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var routeValues = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                routeValues["searchText"] = searchText;
            }

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                routeValues["sortBy"] = sortBy;
            }

            var model = new PagedListViewModel<UserListViewModel>
            {
                Items = pagedUsers,

                Pagination = new PaginationViewModel
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    ControllerName = "User",
                    ActionName = nameof(Index),
                    RouteValues = routeValues
                }
            };

            ViewBag.SearchText = searchText;
            ViewBag.SortBy = sortBy;

            return View(model);
        }
        public IActionResult Create()
        {
            ViewBag.Roles = _roleManager.Roles.ToList();

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = _roleManager.Roles.ToList();
                return View(model);
            }

            var existingUser =
                await _userManager.FindByNameAsync(model.UserName);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "UserName",
                    "Username already exists.");

                ViewBag.Roles = _roleManager.Roles.ToList();

                return View(model);
            }

            var user = new ApplicationUserModel
            {
                FullName = model.FullName,
                UserName = model.UserName,
                Email = model.Email,
                IsActive = model.IsActive,
                EmailConfirmed = true
            };

            var result =
                await _userManager.CreateAsync(
                    user,
                    model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(
                    user,
                    model.Role);

                TempData["Success"] =
                    "User created successfully.";

                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            ViewBag.Roles = _roleManager.Roles.ToList();

            return View(model);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            var model = new UserEditViewModel
            {
                Id = user.Id,
                FullName = user.FullName ?? "",
                UserName = user.UserName ?? "",
                Email = user.Email ?? "",
                Role = roles.FirstOrDefault() ?? "",
                IsActive = user.IsActive
            };

            ViewBag.Roles = _roleManager.Roles.ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = _roleManager.Roles.ToList();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);

            if (user == null)
            {
                return NotFound();
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.IsActive = model.IsActive;

            await _userManager.UpdateAsync(user);

            var currentRoles =
                await _userManager.GetRolesAsync(user);

            await _userManager.RemoveFromRolesAsync(
                user,
                currentRoles);

            await _userManager.AddToRoleAsync(
                user,
                model.Role);

            TempData["Success"] =
                "User updated successfully.";

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var model = new ResetPasswordViewModel
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);

            if (user == null)
            {
                return NotFound();
            }

            var token =
                await _userManager.GeneratePasswordResetTokenAsync(user);

            var result =
                await _userManager.ResetPasswordAsync(
                    user,
                    token,
                    model.NewPassword);

            if (result.Succeeded)
            {
                TempData["Success"] =
                    "Password reset successfully.";

                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }
    }
}