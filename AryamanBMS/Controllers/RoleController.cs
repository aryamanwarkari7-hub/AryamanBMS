using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoleController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RoleController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            var roles = _roleManager.Roles
                .OrderBy(x => x.Name)
                .ToList();

            return View(roles);
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
                ModelState.AddModelError("", "Role name is required.");
                return View();
            }

            bool exists = await _roleManager.RoleExistsAsync(roleName);

            if (exists)
            {
                ModelState.AddModelError("", "Role already exists.");
                return View();
            }

            await _roleManager.CreateAsync(new IdentityRole(roleName));

            TempData["Success"] = "Role created successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);

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

            if (protectedRoles.Contains(role.Name))
            {
                TempData["Error"] =
                    "This role is protected and cannot be deleted.";

                return RedirectToAction(nameof(Index));
            }

            await _roleManager.DeleteAsync(role);

            TempData["Success"] =
                "Role deleted successfully.";

            return RedirectToAction(nameof(Index));
        }


    }
}