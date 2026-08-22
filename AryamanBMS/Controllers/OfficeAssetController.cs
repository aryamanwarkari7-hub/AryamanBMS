using System.Security.Claims;
using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize]
    public class OfficeAssetController : Controller
    {
        #region Actions

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
        private readonly INotificationService _notificationService;

        private readonly UserManager<ApplicationUserModel> _userManager;

        private const string DocumentFolder = "OfficeAssetDocuments";

        public OfficeAssetController(
            IOfficeAssetRepository repository,
            ApplicationDbContext context,
            IFileStorageService fileStorageService,
            UserManager<ApplicationUserModel> userManager,
            INotificationService notificationService)
        {
            _repository = repository;
            _context = context;
            _fileStorageService = fileStorageService;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        [Authorize(Roles = "Admin,Master")]
        [HttpGet]
        public async Task<IActionResult> Index(
            string? financialYear,
            string? category,
            string? status,
            string? search,
            string sortBy = "PurchaseDate",
            string sortOrder = "desc")
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

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim().ToLower();

                assets = assets
                    .Where(x =>
                        (!string.IsNullOrWhiteSpace(x.AssetName) &&
                            x.AssetName.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.AssetCode) &&
                            x.AssetCode.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.AssetCategory) &&
                            x.AssetCategory.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.LocationName) &&
                            x.LocationName.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.AssignedTo) &&
                            x.AssignedTo.ToLower().Contains(keyword)) ||
                        (x.AssignedEmployee != null &&
                            !string.IsNullOrWhiteSpace(x.AssignedEmployee.FullName) &&
                            x.AssignedEmployee.FullName.ToLower().Contains(keyword)))
                    .ToList();
            }

            bool desc =
                string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            assets = sortBy switch
            {
                "AssetName" => desc
                    ? assets.OrderByDescending(x => x.AssetName).ToList()
                    : assets.OrderBy(x => x.AssetName).ToList(),

                "AssetCode" => desc
                    ? assets.OrderByDescending(x => x.AssetCode).ToList()
                    : assets.OrderBy(x => x.AssetCode).ToList(),

                "Category" => desc
                    ? assets.OrderByDescending(x => x.AssetCategory).ToList()
                    : assets.OrderBy(x => x.AssetCategory).ToList(),

                "Value" => desc
                    ? assets.OrderByDescending(x => x.PurchaseValue).ToList()
                    : assets.OrderBy(x => x.PurchaseValue).ToList(),

                "AssignedTo" => desc
                    ? assets.OrderByDescending(x => x.AssignedEmployee?.FullName ?? x.AssignedTo).ToList()
                    : assets.OrderBy(x => x.AssignedEmployee?.FullName ?? x.AssignedTo).ToList(),

                "FinancialYear" => desc
                    ? assets.OrderByDescending(x => x.FinancialYear).ToList()
                    : assets.OrderBy(x => x.FinancialYear).ToList(),

                "Status" => desc
                    ? assets.OrderByDescending(x => x.Status).ToList()
                    : assets.OrderBy(x => x.Status).ToList(),

                _ => desc
                    ? assets.OrderByDescending(x => x.PurchaseDate).ToList()
                    : assets.OrderBy(x => x.PurchaseDate).ToList()
            };

            ViewBag.FilterFinancialYear = financialYear;
            ViewBag.FilterCategory = category;
            ViewBag.FilterStatus = status;
            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

            return View(assets);
        }

        [Authorize(Roles = "Admin,Master")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadAssetLookupsAsync();

            return View(new OfficeAssetModel
            {
                PurchaseDate = DateTime.Today,
                WarrantyStartDate = DateTime.Today,
                Status = "Idle"
            });
        }

        [Authorize(Roles = "Admin,Master")]
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
                await LoadAssetLookupsAsync();
                return View(model);
            }

            await _repository.AddAsync(model);
            await _repository.SaveAsync();

            TempData["Success"] = "Asset added successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Master")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var asset = await _repository.GetByIdAsync(id);
            if (asset == null)
            {
                return NotFound();
            }
            await LoadAssetLookupsAsync();
            return View(asset);
        }

        [Authorize(Roles = "Admin,Master")]
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

                await LoadAssetLookupsAsync();

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

        [Authorize(Roles = "Admin,Master")]
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


        [Authorize(Roles = "Admin,Master")]
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

                var employee = await _context.Employees
                     .AsNoTracking()
                     .FirstOrDefaultAsync(x =>
                         x.Id == employeeId);

                if (employee != null &&
                    !string.IsNullOrWhiteSpace(employee.ApplicationUserId))
                {
                    bool notificationExists =
                        await _notificationService.ExistsAsync(
                            employee.ApplicationUserId,
                            "OfficeAssetAssigned",
                            "OfficeAsset",
                            asset.OfficeAssetId);

                    if (!notificationExists)
                    {
                        await _notificationService.CreateAsync(
                            userId: employee.ApplicationUserId,
                            title: "Office Asset Assigned",
                            message:
                                $"{asset.AssetName} ({asset.AssetCode}) " +
                                $"has been assigned to you.",
                            notificationType: "OfficeAssetAssigned",
                            referenceType: "OfficeAsset",
                            referenceId: asset.OfficeAssetId,
                            actionUrl:
                                $"/OfficeAsset/MyAssetDetails/{asset.OfficeAssetId}");
                    }
                }

                TempData["Success"] = "Asset assigned successfully.";
            }
            catch (Exception)
            {
                TempData["Error"] = "Asset assignment could not be completed.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }


        [Authorize(Roles = "Admin,Master")]
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

            var activeAssignment =
                await _repository.GetActiveAssignmentAsync(id);

            if (activeAssignment == null)
            {
                TempData["Error"] =
                    "This asset has no active assignment.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var employee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == activeAssignment.EmployeeId);

            try
            {
                await _repository.ReturnAsync(
                    id,
                    GetCurrentUserId(),
                    conditionOnReturn?.Trim(),
                    remarks?.Trim());

                if (employee != null &&
                    !string.IsNullOrWhiteSpace(
                        employee.ApplicationUserId))
                {
                    bool notificationExists =
                        await _notificationService.ExistsAsync(
                            employee.ApplicationUserId,
                            "OfficeAssetReturned",
                            "OfficeAssetAssignment",
                            activeAssignment.OfficeAssetAssignmentHistoryId);

                    if (!notificationExists)
                    {
                        await _notificationService.CreateAsync(
                            userId:
                                employee.ApplicationUserId,
                            title:
                                "Office Asset Returned",
                            message:
                                $"{asset.AssetName} " +
                                $"({asset.AssetCode}) has been " +
                                $"marked as returned.",
                            notificationType:
                                "OfficeAssetReturned",
                            referenceType:
                                "OfficeAssetAssignment",
                            referenceId:
                                activeAssignment.OfficeAssetAssignmentHistoryId,
                            actionUrl:
                                "/OfficeAsset/MyAssets");
                    }
                }

                TempData["Success"] =
                    "Asset returned successfully.";
            }
            catch (Exception)
            {
                TempData["Error"] =
                    "Asset return could not be completed.";
            }

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        [Authorize(Roles = "Admin,Master")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            TempData["Error"] = "Archive reason is required.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = "Admin,Master")]
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


        [Authorize(Roles = "Admin,Master")]
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

        [Authorize(Roles = "Admin,Master")]
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
            {
                return NotFound();
            }

            if (cost < 0)
            {
                TempData["Error"] =
                    "Maintenance cost cannot be negative.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var employee = asset.AssignedEmployeeId.HasValue
                ? await _context.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.Id == asset.AssignedEmployeeId.Value)
                : null;

            string maintenanceStatus =
                string.IsNullOrWhiteSpace(status)
                    ? "Completed"
                    : status.Trim();

            var maintenance =
                new OfficeAssetMaintenanceModel
                {
                    OfficeAssetId = id,
                    MaintenanceDate = maintenanceDate,
                    MaintenanceType =
                        string.IsNullOrWhiteSpace(maintenanceType)
                            ? "Repair"
                            : maintenanceType.Trim(),
                    ServiceVendorName =
                        string.IsNullOrWhiteSpace(serviceVendorName)
                            ? null
                            : serviceVendorName.Trim(),
                    Cost = cost,
                    IssueDescription =
                        string.IsNullOrWhiteSpace(issueDescription)
                            ? null
                            : issueDescription.Trim(),
                    Resolution =
                        string.IsNullOrWhiteSpace(resolution)
                            ? null
                            : resolution.Trim(),
                    Status = maintenanceStatus,
                    CreatedByUserId = GetCurrentUserId()
                };

            await _context.OfficeAssetMaintenances
                .AddAsync(maintenance);

            bool isCompleted =
                maintenanceStatus.Equals(
                    "Completed",
                    StringComparison.OrdinalIgnoreCase);

            asset.Status = isCompleted
                ? asset.AssignedEmployeeId.HasValue
                    ? "InUse"
                    : "Idle"
                : "UnderRepair";

            await _context.SaveChangesAsync();

            if (employee != null &&
                !string.IsNullOrWhiteSpace(
                    employee.ApplicationUserId))
            {
                string notificationType = isCompleted
                    ? "OfficeAssetMaintenanceCompleted"
                    : "OfficeAssetUnderRepair";

                string notificationTitle = isCompleted
                    ? "Asset Maintenance Completed"
                    : "Asset Sent for Maintenance";

                string notificationMessage = isCompleted
                    ? $"{asset.AssetName} ({asset.AssetCode}) maintenance has been completed."
                    : $"{asset.AssetName} ({asset.AssetCode}) has been sent for maintenance.";

                bool notificationExists =
                    await _notificationService.ExistsAsync(
                        employee.ApplicationUserId,
                        notificationType,
                        "OfficeAssetMaintenance",
                        maintenance.OfficeAssetMaintenanceId);

                if (!notificationExists)
                {
                    await _notificationService.CreateAsync(
                        userId: employee.ApplicationUserId,
                        title: notificationTitle,
                        message: notificationMessage,
                        notificationType: notificationType,
                        referenceType: "OfficeAssetMaintenance",
                        referenceId:
                            maintenance.OfficeAssetMaintenanceId,
                        actionUrl:
                            $"/OfficeAsset/MyAssetDetails/{asset.OfficeAssetId}");
                }
            }

            TempData["Success"] =
                "Asset maintenance entry added.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }


        [Authorize(Roles = "Admin,Master")]
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

        [Authorize(Roles = "Admin,Master")]
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

        [Authorize(Roles = "Employee")]
        [HttpGet]
        public async Task<IActionResult> MyAssets()
        {
            string userId =
                _userManager.GetUserId(User)
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var employee =
                await _context.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == userId);

            if (employee == null)
            {
                TempData["Error"] =
                    "No employee record is linked to your user account.";

                return View(new List<OfficeAssetModel>());
            }

            var assets =
                await _context.OfficeAssets
                    .AsNoTracking()
                    .Where(x =>
                        x.AssignedEmployeeId == employee.Id &&
                        x.IsActive)
                    .OrderBy(x => x.AssetName)
                    .ThenBy(x => x.AssetCode)
                    .ToListAsync();

            return View(assets);
        }

        [Authorize(Roles = "Employee")]
        [HttpGet]
        public async Task<IActionResult> MyAssetDetails(int id)
        {
            string userId =
                _userManager.GetUserId(User)
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var employee =
                await _context.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == userId);

            if (employee == null)
            {
                TempData["Error"] =
                    "No employee record is linked to your user account.";

                return RedirectToAction(nameof(MyAssets));
            }

            var asset =
                await _context.OfficeAssets
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.OfficeAssetId == id &&
                        x.AssignedEmployeeId == employee.Id &&
                        x.IsActive);

            if (asset == null)
            {
                return Forbid();
            }

            return View(asset);
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
        private async Task LoadAssetLookupsAsync()
        {
            ViewBag.Vendors = await _context.Vendors
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.VendorName)
                .ToListAsync();

            ViewBag.States = await _context.States
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.StateName)
                .ToListAsync();
        }
        #endregion
    }
}
