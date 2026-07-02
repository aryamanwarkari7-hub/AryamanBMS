using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class OfficeAssetController : Controller
    {
        private readonly IOfficeAssetRepository _repository;

        public OfficeAssetController(IOfficeAssetRepository repository)
        {
            _repository = repository;
        }

        #region Index

        public async Task<IActionResult> Index(
    string? financialYear,
    string? category,
    string? status)
        {
            var assets = await _repository.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(financialYear))
            {
                assets = assets
                    .Where(x => x.FinancialYear == financialYear)
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                assets = assets
                    .Where(x => x.AssetCategory == category)
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                assets = assets
                    .Where(x => x.Status == status)
                    .ToList();
            }

            ViewBag.FilterFinancialYear = financialYear;
            ViewBag.FilterCategory = category;
            ViewBag.FilterStatus = status;

            return View(assets);
        }

        #endregion

        #region Create

        public IActionResult Create()
        {
            return View(new OfficeAssetModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OfficeAssetModel model)
        {
            model.AssetName = model.AssetName?.Trim() ?? string.Empty;
            model.AssetCategory = model.AssetCategory?.Trim() ?? string.Empty;
            model.AssetCode = model.AssetCode?.Trim();
            model.VendorName = model.VendorName?.Trim();
            model.AssignedTo = model.AssignedTo?.Trim();
            model.FinancialYear = model.FinancialYear?.Trim() ?? string.Empty;
            model.Status = model.Status?.Trim() ?? "InUse";
            model.Remarks = model.Remarks?.Trim();
            model.IsActive = true;
            model.CreatedOn = DateTime.Now;

            if (!ModelState.IsValid)
                return View(model);

            await _repository.AddAsync(model);
            await _repository.SaveAsync();

            TempData["Success"] = "Asset added successfully.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Edit

        public async Task<IActionResult> Edit(int id)
        {
            var asset = await _repository.GetByIdAsync(id);
            if (asset == null) return NotFound();

            return View(asset);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OfficeAssetModel model)
        {
            if (id != model.OfficeAssetId) return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.AssetName = model.AssetName;
            existing.AssetCategory = model.AssetCategory;
            existing.AssetCode = model.AssetCode;
            existing.PurchaseDate = model.PurchaseDate;
            existing.PurchaseValue = model.PurchaseValue;
            existing.VendorName = model.VendorName;
            existing.AssignedTo = model.AssignedTo;
            existing.FinancialYear = model.FinancialYear;
            existing.Status = model.Status;
            existing.Remarks = model.Remarks;

            await _repository.UpdateAsync(existing);
            await _repository.SaveAsync();

            TempData["Success"] = "Asset updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Details

        public async Task<IActionResult> Details(int id)
        {
            var asset = await _repository.GetByIdAsync(id);
            if (asset == null) return NotFound();

            return View(asset);
        }

        #endregion

        #region Delete

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var asset = await _repository.GetByIdAsync(id);

            if (asset == null)
            {
                return NotFound();
            }

            asset.IsActive = !asset.IsActive;
            asset.UpdatedOn = DateTime.Now;

            await _repository.UpdateAsync(asset);
            await _repository.SaveAsync();

            TempData["Success"] = asset.IsActive
                ? "Asset activated successfully."
                : "Asset archived successfully.";

            return RedirectToAction(nameof(Index));
        }

        #endregion
    }
}