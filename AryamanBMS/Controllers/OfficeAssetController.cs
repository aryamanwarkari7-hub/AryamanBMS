using System.Security.Claims;
using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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
            "Disposed",
            "Lost",
            "Damaged",
            "Retired"
        };

        private readonly IOfficeAssetRepository _repository;
        private readonly ApplicationDbContext _context;
        private readonly IFileStorageService _fileStorageService;

        private const string DocumentFolder = "OfficeAssetDocuments";

        public OfficeAssetController(
            IOfficeAssetRepository repository,
            ApplicationDbContext context,
            IFileStorageService fileStorageService)
        {
            _repository = repository;
            _context = context;
            _fileStorageService = fileStorageService;
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
            await ApplyAssetDefaultsAsync(model);
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
            await ApplyAssetDefaultsAsync(model, id);

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
            existing.SerialNumber = model.SerialNumber;
            existing.ModelNumber = model.ModelNumber;
            existing.Manufacturer = model.Manufacturer;
            existing.Brand = model.Brand;
            existing.ConfigurationDetails = model.ConfigurationDetails;
            existing.Barcode = model.Barcode;
            existing.PurchaseDate = model.PurchaseDate;
            existing.PurchaseValue = model.PurchaseValue;
            existing.TaxableAmount = model.TaxableAmount;
            existing.CGSTAmount = model.CGSTAmount;
            existing.SGSTAmount = model.SGSTAmount;
            existing.IGSTAmount = model.IGSTAmount;
            existing.TotalGSTAmount = model.TotalGSTAmount;
            existing.ITCEligible = model.ITCEligible;
            existing.ITCStatus = model.ITCStatus;
            existing.CapitalizedValue = model.CapitalizedValue;
            existing.VendorName = model.VendorName;
            existing.VendorId = model.VendorId;
            existing.ExpenseVoucherId = model.ExpenseVoucherId;
            existing.PurchaseOrderId = model.PurchaseOrderId;
            existing.VendorInvoiceNumber = model.VendorInvoiceNumber;
            existing.VendorInvoiceDate = model.VendorInvoiceDate;
            existing.LocationName = model.LocationName;
            existing.Building = model.Building;
            existing.Floor = model.Floor;
            existing.RoomOrSeat = model.RoomOrSeat;
            existing.FinancialYear = model.FinancialYear;
            existing.Status = model.Status;
            existing.Remarks = model.Remarks;
            existing.WarrantyStartDate = model.WarrantyStartDate;
            existing.WarrantyEndDate = model.WarrantyEndDate;
            existing.HasAmc = model.HasAmc;
            existing.AmcVendorName = model.AmcVendorName;
            existing.AmcStartDate = model.AmcStartDate;
            existing.AmcEndDate = model.AmcEndDate;
            existing.DepreciationRate = model.DepreciationRate;
            existing.AccumulatedDepreciation = model.AccumulatedDepreciation;
            existing.WrittenDownValue = model.WrittenDownValue;
            existing.DisposalDate = model.DisposalDate;
            existing.DisposalValue = model.DisposalValue;
            existing.DisposalReason = model.DisposalReason;
            existing.LostOrDamagedOn = model.LostOrDamagedOn;
            existing.LostOrDamagedReason = model.LostOrDamagedReason;

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
            TempData["Error"] = "Archive reason is required.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(int id, string reason)
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

            if (asset.AssignmentHistory.Any())
            {
                TempData["Error"] =
                    "Assets with transaction history cannot be archived. Use Retired or Disposed status instead.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "Archive reason is required.";
                return RedirectToAction(nameof(Details), new { id });
            }

            asset.IsActive = false;
            asset.ArchiveReason = reason.Trim();
            asset.ArchivedByUserId = GetCurrentUserId();
            asset.ArchivedOn = DateTime.Now;
            asset.UpdatedOn = DateTime.Now;

            await _repository.UpdateAsync(asset);
            await _repository.SaveAsync();

            TempData["Success"] = "Asset archived successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Capitalize(int id)
        {
            var asset = await _repository.GetByIdAsync(id);
            if (asset == null)
                return NotFound();

            if (asset.IsCapitalized)
            {
                TempData["Error"] = "Asset is already capitalized.";
                return RedirectToAction(nameof(Details), new { id });
            }

            asset.IsCapitalized = true;
            asset.CapitalizedOn = DateTime.Now;
            asset.CapitalizedByUserId = GetCurrentUserId();
            asset.CapitalizedValue = asset.PurchaseValue;
            asset.WrittenDownValue = asset.CapitalizedValue - asset.AccumulatedDepreciation;

            await _repository.UpdateAsync(asset);
            await _repository.SaveAsync();

            TempData["Success"] = "Asset capitalized successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMaintenance(
            int id,
            DateTime maintenanceDate,
            string maintenanceType,
            string? serviceVendorName,
            decimal cost,
            string? issueDescription,
            string? resolution,
            string status)
        {
            var asset = await _repository.GetByIdAsync(id);
            if (asset == null)
                return NotFound();

            if (cost < 0)
            {
                TempData["Error"] = "Maintenance cost cannot be negative.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await _context.OfficeAssetMaintenances.AddAsync(new OfficeAssetMaintenanceModel
            {
                OfficeAssetId = id,
                MaintenanceDate = maintenanceDate,
                MaintenanceType = maintenanceType?.Trim() ?? "Repair",
                ServiceVendorName = serviceVendorName?.Trim(),
                Cost = cost,
                IssueDescription = issueDescription?.Trim(),
                Resolution = resolution?.Trim(),
                Status = string.IsNullOrWhiteSpace(status) ? "Completed" : status.Trim(),
                CreatedByUserId = GetCurrentUserId()
            });

            asset.Status = "UnderRepair";
            if (string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                asset.Status = asset.AssignedEmployeeId.HasValue ? "InUse" : "Idle";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Asset maintenance entry added.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(
            int id,
            string verificationStatus,
            string? verifiedLocation,
            string? remarks)
        {
            var asset = await _repository.GetByIdAsync(id);
            if (asset == null)
                return NotFound();

            string userId = GetCurrentUserId();
            await _context.OfficeAssetVerifications.AddAsync(new OfficeAssetVerificationModel
            {
                OfficeAssetId = id,
                VerificationStatus = string.IsNullOrWhiteSpace(verificationStatus) ? "Found" : verificationStatus.Trim(),
                VerifiedLocation = verifiedLocation?.Trim(),
                VerifiedByUserId = userId,
                Remarks = remarks?.Trim()
            });

            asset.LastVerifiedByUserId = userId;
            asset.LastVerifiedOn = DateTime.Now;
            asset.LastVerificationStatus = verificationStatus;
            asset.LocationName = string.IsNullOrWhiteSpace(verifiedLocation)
                ? asset.LocationName
                : verifiedLocation.Trim();

            await _context.SaveChangesAsync();

            TempData["Success"] = "Physical verification recorded.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(
            int id,
            string documentType,
            IFormFile file,
            string? remarks)
        {
            var asset = await _repository.GetByIdAsync(id);
            if (asset == null)
                return NotFound();

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Select a file to upload.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var uploadResult = await _fileStorageService.UploadAsync(file, DocumentFolder);
            if (!uploadResult.Success)
            {
                TempData["Error"] = uploadResult.ErrorMessage;
                return RedirectToAction(nameof(Details), new { id });
            }

            await _context.OfficeAssetDocuments.AddAsync(new OfficeAssetDocumentModel
            {
                OfficeAssetId = id,
                DocumentType = documentType?.Trim() ?? "Other",
                OriginalFileName = uploadResult.OriginalFileName,
                StoredFilePath = uploadResult.RelativePath,
                UploadedByUserId = GetCurrentUserId(),
                Remarks = remarks?.Trim()
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Asset document uploaded.";
            return RedirectToAction(nameof(Details), new { id });
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

            if ((model.Status == "Lost" || model.Status == "Damaged") &&
                !model.LostOrDamagedOn.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.LostOrDamagedOn),
                    "Lost/damaged date is required for this status.");
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

            if (hasActiveAssignment && (model.Status == "Lost" || model.Status == "Damaged"))
            {
                ModelState.AddModelError(
                    nameof(model.Status),
                    "Return the assigned asset before marking it lost or damaged.");
            }
        }

        private void NormalizeAsset(OfficeAssetModel model)
        {
            model.AssetName = model.AssetName?.Trim() ?? string.Empty;
            model.AssetCategory = model.AssetCategory?.Trim() ?? string.Empty;
            model.AssetCode = string.IsNullOrWhiteSpace(model.AssetCode)
                ? null
                : model.AssetCode.Trim().ToUpperInvariant();
            model.SerialNumber = string.IsNullOrWhiteSpace(model.SerialNumber) ? null : model.SerialNumber.Trim();
            model.ModelNumber = string.IsNullOrWhiteSpace(model.ModelNumber) ? null : model.ModelNumber.Trim();
            model.Manufacturer = string.IsNullOrWhiteSpace(model.Manufacturer) ? null : model.Manufacturer.Trim();
            model.Brand = string.IsNullOrWhiteSpace(model.Brand) ? null : model.Brand.Trim();
            model.ConfigurationDetails = string.IsNullOrWhiteSpace(model.ConfigurationDetails) ? null : model.ConfigurationDetails.Trim();
            model.Barcode = string.IsNullOrWhiteSpace(model.Barcode) ? null : model.Barcode.Trim();
            model.VendorName = string.IsNullOrWhiteSpace(model.VendorName)
                ? null
                : model.VendorName.Trim();
            model.VendorInvoiceNumber = string.IsNullOrWhiteSpace(model.VendorInvoiceNumber) ? null : model.VendorInvoiceNumber.Trim();
            model.LocationName = string.IsNullOrWhiteSpace(model.LocationName) ? null : model.LocationName.Trim();
            model.Building = string.IsNullOrWhiteSpace(model.Building) ? null : model.Building.Trim();
            model.Floor = string.IsNullOrWhiteSpace(model.Floor) ? null : model.Floor.Trim();
            model.RoomOrSeat = string.IsNullOrWhiteSpace(model.RoomOrSeat) ? null : model.RoomOrSeat.Trim();
            model.FinancialYear = model.FinancialYear?.Trim() ?? string.Empty;
            model.Status = model.Status?.Trim() ?? "Idle";
            model.Remarks = string.IsNullOrWhiteSpace(model.Remarks)
                ? null
                : model.Remarks.Trim();
        }

        private async Task ApplyAssetDefaultsAsync(
            OfficeAssetModel model,
            int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(model.AssetCode))
            {
                model.AssetCode = await _repository.GenerateAssetCodeAsync(model.AssetCategory);
            }

            if (model.TaxableAmount <= 0)
            {
                model.TaxableAmount = model.PurchaseValue;
            }

            model.TotalGSTAmount =
                model.CGSTAmount +
                model.SGSTAmount +
                model.IGSTAmount;

            model.CapitalizedValue =
                model.ITCEligible
                    ? model.PurchaseValue - model.TotalGSTAmount
                    : model.PurchaseValue;

            if (model.WrittenDownValue <= 0)
            {
                model.WrittenDownValue =
                    Math.Max(model.CapitalizedValue - model.AccumulatedDepreciation, 0);
            }

            if (model.VendorId.HasValue)
            {
                var vendor = await _context.Vendors
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.VendorId == model.VendorId.Value);

                if (vendor != null)
                {
                    model.VendorName = vendor.VendorName;
                }
            }
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
