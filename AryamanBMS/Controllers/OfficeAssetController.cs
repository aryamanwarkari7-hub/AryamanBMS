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

        public async Task<IActionResult> Index(string? financialYear, string? category, string? status)
        {
            List<OfficeAssetModel> assets;

            if (!string.IsNullOrEmpty(financialYear))
                assets = await _repository.GetByFinancialYearAsync(financialYear);
            else if (!string.IsNullOrEmpty(category))
                assets = await _repository.GetByCategoryAsync(category);
            else if (!string.IsNullOrEmpty(status))
                assets = await _repository.GetByStatusAsync(status);
            else
                assets = await _repository.GetAllAsync();

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
            if (asset == null) return NotFound();

            await _repository.DeleteAsync(asset);
            await _repository.SaveAsync();

            TempData["Success"] = "Asset deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        #endregion
    }
}