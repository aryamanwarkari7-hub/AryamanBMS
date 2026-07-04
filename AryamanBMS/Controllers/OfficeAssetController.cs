using System.Security.Claims;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class OfficeAssetController : Controller
    {
        private static readonly string[] AllowedStatuses =
        {
            "InUse",
            "Idle",
            "UnderRepair",
            "Disposed"
        };

        private readonly IOfficeAssetRepository _repository;

        public OfficeAssetController(IOfficeAssetRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
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

        [HttpGet]
        public IActionResult Create()
        {
            return View(new OfficeAssetModel
            {
                PurchaseDate = DateTime.Today,
                WarrantyStartDate = DateTime.Today,
                Status = "Idle"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OfficeAssetModel model)
        {
            NormalizeAsset(model);
            model.AssignedTo = null;
            model.AssignedEmployeeId = null;
            model.IsActive = true;
            model.CreatedOn = DateTime.Now;

            await ValidateAssetAsync(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _repository.AddAsync(model);
            await _repository.SaveAsync();

            TempData["Success"] = "Asset added successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var asset = await _repository.GetByIdAsync(id);
            if (asset == null)
            {
                return NotFound();
            }

            return View(asset);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OfficeAssetModel model)
        {
            if (id != model.OfficeAssetId)
            {
                return NotFound();
            }

            NormalizeAsset(model);

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            var activeAssignment = await _repository.GetActiveAssignmentAsync(id);

            await ValidateAssetAsync(model, id);
            ValidateStatusTransition(existing, model, activeAssignment != null);

            if (!ModelState.IsValid)
            {
                model.AssignedEmployeeId = existing.AssignedEmployeeId;
                model.AssignedTo = existing.AssignedTo;
                model.IsActive = existing.IsActive;
                model.CreatedOn = existing.CreatedOn;
                return View(model);
            }

            existing.AssetName = model.AssetName;
            existing.AssetCategory = model.AssetCategory;
            existing.AssetCode = model.AssetCode;
            existing.PurchaseDate = model.PurchaseDate;
            existing.PurchaseValue = model.PurchaseValue;
            existing.VendorName = model.VendorName;
            existing.FinancialYear = model.FinancialYear;
            existing.Status = model.Status;
            existing.Remarks = model.Remarks;
            existing.WarrantyStartDate = model.WarrantyStartDate;
            existing.WarrantyEndDate = model.WarrantyEndDate;
            existing.DisposalDate = model.DisposalDate;

            await _repository.UpdateAsync(existing);
            await _repository.SaveAsync();

            TempData["Success"] = "Asset updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var asset = await _repository.GetByIdAsync(id);
            if (asset == null)
            {
                return NotFound();
            }

            await LoadDetailsDataAsync(id);

            return View(asset);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(
            int id,
            int employeeId,
            string? conditionOnAssignment,
            string? remarks)
        {
            var asset = await _repository.GetByIdAsync(id);
            if (asset == null)
            {
                return NotFound();
            }

            if (!asset.IsActive)
            {
                TempData["Error"] = "Archived assets cannot be assigned.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (asset.Status == "Disposed")
            {
                TempData["Error"] = "Disposed assets cannot be assigned.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var activeAssignment = await _repository.GetActiveAssignmentAsync(id);
            if (activeAssignment != null)
            {
                TempData["Error"] = "This asset already has an active assignment.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                await _repository.AssignAsync(
                    id,
                    employeeId,
                    GetCurrentUserId(),
                    conditionOnAssignment?.Trim(),
                    remarks?.Trim());

                TempData["Success"] = "Asset assigned successfully.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Asset assignment could not be completed.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(
            int id,
            string? conditionOnReturn,
            string? remarks)
        {
            var asset = await _repository.GetByIdAsync(id);
            if (asset == null)
            {
                return NotFound();
            }

            var activeAssignment = await _repository.GetActiveAssignmentAsync(id);
            if (activeAssignment == null)
            {
                TempData["Error"] = "This asset has no active assignment.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                await _repository.ReturnAsync(
                    id,
                    GetCurrentUserId(),
                    conditionOnReturn?.Trim(),
                    remarks?.Trim());

                TempData["Success"] = "Asset returned successfully.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Asset return could not be completed.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var asset = await _repository.GetByIdAsync(id);

            if (asset == null)
            {
                return NotFound();
            }

            var activeAssignment = await _repository.GetActiveAssignmentAsync(id);
            if (activeAssignment != null)
            {
                TempData["Error"] =
                    "Assigned assets must be returned before they can be archived.";
                return RedirectToAction(nameof(Details), new { id });
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

        private async Task ValidateAssetAsync(
            OfficeAssetModel model,
            int? excludeId = null)
        {
            if (!AllowedStatuses.Contains(model.Status))
            {
                ModelState.AddModelError(nameof(model.Status), "Invalid asset status.");
            }

            if (model.PurchaseValue < 0)
            {
                ModelState.AddModelError(
                    nameof(model.PurchaseValue),
                    "Purchase value cannot be negative.");
            }

            if (model.PurchaseDate.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.PurchaseDate),
                    "Purchase date cannot be in the future.");
            }

            if (model.WarrantyStartDate.HasValue &&
                model.WarrantyStartDate.Value.Date < model.PurchaseDate.Date)
            {
                ModelState.AddModelError(
                    nameof(model.WarrantyStartDate),
                    "Warranty start cannot be before purchase date.");
            }

            if (model.WarrantyStartDate.HasValue &&
                model.WarrantyEndDate.HasValue &&
                model.WarrantyEndDate.Value.Date < model.WarrantyStartDate.Value.Date)
            {
                ModelState.AddModelError(
                    nameof(model.WarrantyEndDate),
                    "Warranty end cannot be before warranty start.");
            }

            if (model.WarrantyEndDate.HasValue &&
                model.WarrantyEndDate.Value.Date < model.PurchaseDate.Date)
            {
                ModelState.AddModelError(
                    nameof(model.WarrantyEndDate),
                    "Warranty end cannot be before purchase date.");
            }

            if (model.DisposalDate.HasValue &&
                model.DisposalDate.Value.Date < model.PurchaseDate.Date)
            {
                ModelState.AddModelError(
                    nameof(model.DisposalDate),
                    "Disposal date cannot be before purchase date.");
            }

            if (model.Status == "Disposed" && !model.DisposalDate.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.DisposalDate),
                    "Disposal date is required when status is Disposed.");
            }

            if (model.Status != "Disposed" && model.DisposalDate.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.DisposalDate),
                    "Disposal date should only be set when the asset is Disposed.");
            }

            if (!string.IsNullOrWhiteSpace(model.AssetCode) &&
                await _repository.AssetCodeExistsAsync(model.AssetCode, excludeId))
            {
                ModelState.AddModelError(
                    nameof(model.AssetCode),
                    "This asset code already exists.");
            }
        }

        private void ValidateStatusTransition(
            OfficeAssetModel existing,
            OfficeAssetModel model,
            bool hasActiveAssignment)
        {
            if (existing.Status == "Disposed" && model.Status != "Disposed")
            {
                ModelState.AddModelError(
                    nameof(model.Status),
                    "Disposed assets cannot be moved back to another status.");
            }

            if (hasActiveAssignment && model.Status == "Disposed")
            {
                ModelState.AddModelError(
                    nameof(model.Status),
                    "Assigned assets must be returned before disposal.");
            }
        }

        private void NormalizeAsset(OfficeAssetModel model)
        {
            model.AssetName = model.AssetName?.Trim() ?? string.Empty;
            model.AssetCategory = model.AssetCategory?.Trim() ?? string.Empty;
            model.AssetCode = string.IsNullOrWhiteSpace(model.AssetCode)
                ? null
                : model.AssetCode.Trim();
            model.VendorName = string.IsNullOrWhiteSpace(model.VendorName)
                ? null
                : model.VendorName.Trim();
            model.FinancialYear = model.FinancialYear?.Trim() ?? string.Empty;
            model.Status = model.Status?.Trim() ?? "Idle";
            model.Remarks = string.IsNullOrWhiteSpace(model.Remarks)
                ? null
                : model.Remarks.Trim();
        }

        private async Task LoadDetailsDataAsync(int assetId)
        {
            var employees = await _repository.GetActiveEmployeesAsync();
            var history = await _repository.GetAssignmentHistoryAsync(assetId);
            var activeAssignment = await _repository.GetActiveAssignmentAsync(assetId);

            ViewBag.EmployeeOptions = employees.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = string.IsNullOrWhiteSpace(x.EmployeeCode)
                    ? x.FullName
                    : $"{x.EmployeeCode} - {x.FullName}"
            }).ToList();

            ViewBag.AssignmentHistory = history;
            ViewBag.ActiveAssignment = activeAssignment;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }
    }
}
