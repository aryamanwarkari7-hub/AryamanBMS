using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Master")]
    public class VendorController : Controller
    {
        #region Actions

        private readonly IVendorService _vendorService;
        private readonly UserManager<ApplicationUserModel> _userManager;

        public VendorController(
            IVendorService vendorService,
            UserManager<ApplicationUserModel> userManager)
        {
            _vendorService = vendorService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(
    string? search,
    string sortBy = "VendorName",
    string sortOrder = "asc")
        {
            var vendors = await _vendorService.GetActiveAsync(
                search,
                sortBy,
                sortOrder);

            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

            return View(vendors);
        }

        public IActionResult Create()
        {
            return View(new VendorModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VendorModel model)
        {
            var errors = await _vendorService.ValidateForCreateAsync(model);
            AddValidationErrors(errors);

            if (!ModelState.IsValid)
                return View(model);

            await _vendorService.CreateAsync(
                model,
                _userManager.GetUserId(User));

            TempData["Success"] = $"Vendor '{model.VendorName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var vendor = await _vendorService.GetActiveByIdAsync(id);

            if (vendor == null)
                return NotFound();

            return View(vendor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VendorModel model)
        {
            var errors = await _vendorService.ValidateForUpdateAsync(model);
            AddValidationErrors(errors);

            if (!ModelState.IsValid)
                return View(model);

            var existing = await _vendorService.UpdateAsync(model);

            if (existing == null)
                return NotFound();

            TempData["Success"] = $"Vendor '{existing.VendorName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            if (!await _vendorService.DeactivateAsync(id))
                return NotFound();

            TempData["Success"] = "Vendor deactivated.";
            return RedirectToAction(nameof(Index));
        }

        private void AddValidationErrors(
            IReadOnlyDictionary<string, string> errors)
        {
            foreach (var error in errors)
            {
                ModelState.AddModelError(error.Key, error.Value);
            }
        }
        #endregion
    }
}
