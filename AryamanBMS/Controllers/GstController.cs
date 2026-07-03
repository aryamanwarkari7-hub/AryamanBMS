using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AryamanBMS.Controllers
{
    /// <summary>
    /// GST Management Controller
    /// Handles GST calculations, snapshots, compliance tracking, and configuration
    /// </summary>
    [Authorize(Roles = "Admin,Finance")]
    public class GstController : Controller
    {
        private readonly IGstCalculationService _calculationService;
        private readonly IGstDashboardService _dashboardService;
        private readonly IGstSnapshotRepository _snapshotRepository;
        private readonly IGstConfigurationRepository _configurationRepository;
        private readonly IGstReturnRepository _returnRepository;
        private readonly IGstChallanRepository _challanRepository;
        private readonly IGstDocumentRepository _documentRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly ILogger<GstController> _logger;

        private const string DocumentFolder = "GstDocuments";

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        public GstController(
           IGstCalculationService calculationService,
           IGstDashboardService dashboardService,
           IGstSnapshotRepository snapshotRepository,
           IGstConfigurationRepository configurationRepository,
           IGstReturnRepository returnRepository,
           IGstChallanRepository challanRepository,
           IGstDocumentRepository documentRepository,
           IFileStorageService fileStorageService,
           UserManager<ApplicationUserModel> userManager,
           ILogger<GstController> logger)
        {
            _calculationService = calculationService;
            _dashboardService = dashboardService;
            _snapshotRepository = snapshotRepository;
            _configurationRepository = configurationRepository;
            _returnRepository = returnRepository;
            _challanRepository = challanRepository;
            _documentRepository = documentRepository;
            _fileStorageService = fileStorageService;
            _userManager = userManager;
            _logger = logger;
        }

        #region Index - Landing Page

        /// <summary>
        /// GET: /Gst/Index
        /// Displays GST management landing page with month selector
        /// </summary>
        /// <returns>View with navigation options and recent snapshots</returns>
        [HttpGet]
        public async Task<IActionResult> Index(int month = 0, int year = 0)
        {
            try
            {
                bool hasSelectedPeriod = month > 0 && year > 0;
                if (month == 0) month = DateTime.Now.Month;
                if (year == 0) year = DateTime.Now.Year;

                if (month < 1 || month > 12)
                {
                    TempData["Error"] = "Invalid month selected.";
                    return RedirectToAction(nameof(Index));
                }

                if (year < 2000 || year > DateTime.Now.Year + 1)
                {
                    TempData["Error"] = "Invalid year selected.";
                    return RedirectToAction(nameof(Index));
                }

                var snapshots = await _snapshotRepository.GetAllAsync();

                ViewBag.SelectedMonth = month;
                ViewBag.SelectedYear = year;
                ViewBag.SelectedSnapshot = snapshots
                    .FirstOrDefault(x => x.Month == month && x.Year == year);

                ViewBag.HasSelectedPeriod = hasSelectedPeriod;
                return View(snapshots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading GST Index page");
                TempData["Error"] = "An error occurred while loading GST page.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        #endregion

        #region Dashboard - GST Summary & Compliance

        /// <summary>
        /// GET: /Gst/Dashboard?month=6&year=2026
        /// Displays detailed GST calculations for the specified month/year
        /// Shows Output GST, Input GST, Net Payable, and compliance status
        /// </summary>
        /// <param name="month">Month (1-12)</param>
        /// <param name="year">Financial year</param>
        /// <returns>GST Dashboard view with calculations and compliance data</returns>
        [HttpGet]
        public async Task<IActionResult> Dashboard(int month = 0, int year = 0)
        {
            try
            {
                // Default to current month if not specified
                if (month == 0) month = DateTime.Now.Month;
                if (year == 0) year = DateTime.Now.Year;

                // Validate month range
                if (month < 1 || month > 12)
                {
                    TempData["Error"] = "Invalid month. Please select a month between 1 and 12.";
                    return RedirectToAction(nameof(Index));
                }

                if (year < 2000 || year > DateTime.Now.Year + 1)
                {
                    TempData["Error"] = "Invalid year selected.";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation($"Loading GST Dashboard for {month}/{year}");

                // Get GST dashboard data from service
                var model = await _dashboardService.GetDashboardAsync(month, year);

                if (model == null)
                {
                    _logger.LogWarning($"No GST data found for {month}/{year}");
                    model = new GstDashboardViewModel
                    {
                        Month = month,
                        Year = year,
                        FinancialYear = GetFinancialYear(month, year),
                        Gstr1Status = "Pending",
                        Gstr3BStatus = "Pending",
                        ChallanStatus = "Pending"
                    };
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading GST Dashboard for {month}/{year}");
                TempData["Error"] = "An error occurred while loading GST dashboard.";
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion

        #region Generate - Create/Recalculate Snapshot

        /// <summary>
        /// POST: /Gst/Generate
        /// Generates or regenerates the GST monthly snapshot for the specified month/year
        /// Calculates Output GST, Input GST, and Net Payable amount
        /// </summary>
        /// <param name="month">Month (1-12)</param>
        /// <param name="year">Financial year</param>
        /// <returns>Redirect to Dashboard with success/error message</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(int month, int year)
        {
            try
            {
                // Validate inputs
                if (month < 1 || month > 12)
                {
                    TempData["Error"] = "Invalid month. Please select a valid month.";
                    return RedirectToAction(nameof(Index));
                }

                if (year < 2000 || year > DateTime.Now.Year + 1)
                {
                    TempData["Error"] = "Invalid year selected.";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation($"Generating GST snapshot for {month}/{year}");

                // Check if snapshot is already locked
                var isLocked = await _calculationService.IsSnapshotLockedAsync(month, year);
                if (isLocked)
                {
                    TempData["Error"] = "This GST snapshot is already filed and cannot be regenerated.";
                    return RedirectToAction(nameof(Dashboard), new { month, year });
                }

                // Generate the monthly snapshot
                var snapshot = await _calculationService.GenerateMonthlySnapshotAsync(month, year);

                if (snapshot != null)
                {
                    TempData["Success"] = $"GST snapshot generated successfully for {GetMonthName(month)} {year}.";
                    _logger.LogInformation($"GST snapshot generated: OutputGST={snapshot.TotalOutputGST}, InputGST={snapshot.TotalInputGST}, NetPayable={snapshot.NetGSTPayable}");
                }
                else
                {
                    TempData["Warning"] = "Snapshot generated but no data was calculated for this period.";
                }

                return RedirectToAction(nameof(Dashboard), new { month, year });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"GST snapshot not generated for {month}/{year}");
                TempData["Warning"] = ex.Message;
                return RedirectToAction(nameof(Index), new { month, year });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating GST snapshot for {month}/{year}");
                TempData["Error"] = "Unable to generate GST snapshot right now. Please check the selected period and try again.";
                return RedirectToAction(nameof(Dashboard), new { month, year });
            }
        }

        #endregion

        #region Configuration - Settings & Setup

        /// <summary>
        /// GET: /Gst/Configuration
        /// Displays GST configuration page for managing rates and company details
        /// </summary>
        /// <returns>Configuration view with current GST settings</returns>
        [HttpGet]
        public async Task<IActionResult> Configuration()
        {
            try
            {
                _logger.LogInformation("GST Configuration page accessed");

                var configuration =
                    await _configurationRepository.GetActiveAsync();

                var model = configuration == null
                    ? new GstConfigurationViewModel
                    {
                        SgstRate = 9,
                        CgstRate = 9,
                        IgstRate = 18,
                        RegisteredState = "MH",
                        LastUpdatedOn = DateTime.Now
                    }
                    : new GstConfigurationViewModel
                    {
                        GstConfigurationId = configuration.GstConfigurationId,
                        CompanyName = configuration.CompanyName,
                        CompanyGstin = configuration.CompanyGstin,
                        RegisteredState = configuration.RegisteredState,
                        CgstRate = configuration.CgstRate,
                        SgstRate = configuration.SgstRate,
                        IgstRate = configuration.IgstRate,
                        IsActive = configuration.IsActive,
                        UpdatedByUserId = configuration.UpdatedByUserId,
                        LastUpdatedOn = configuration.UpdatedOn
                    };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading GST Configuration page");
                TempData["Error"] = "An error occurred while loading configuration page.";
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion

        #region Update GST Rates

        /// <summary>
        /// POST: /Gst/UpdateGstRates
        /// Updates GST rates (SGST, CGST, IGST) in the system
        /// </summary>
        /// <param name="sgstRate">State GST rate (0-100%)</param>
        /// <param name="cgstRate">Central GST rate (0-100%)</param>
        /// <param name="igstRate">Integrated GST rate (0-100%)</param>
        /// <returns>Redirect to Configuration page with success/error message</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateGstRates(
            [Bind("SgstRate,CgstRate,IgstRate")] GstConfigurationViewModel model)
        {
            try
            {
                if (!AreValidGstRates(model.CgstRate, model.SgstRate, model.IgstRate))
                {
                    TempData["Error"] = "GST rates must be between 0 and 100, and IGST should equal CGST + SGST.";
                    return RedirectToAction(nameof(Configuration));
                }

                _logger.LogInformation($"Updating GST rates: SGST={model.SgstRate}%, CGST={model.CgstRate}%, IGST={model.IgstRate}%");

                var configuration =
                    await GetOrCreateConfigurationAsync();

                configuration.CgstRate = model.CgstRate;
                configuration.SgstRate = model.SgstRate;
                configuration.IgstRate = model.IgstRate;
                configuration.UpdatedByUserId = _userManager.GetUserId(User);

                await _configurationRepository.SaveActiveAsync(configuration);

                TempData["Success"] = "GST rates updated successfully.";
                return RedirectToAction(nameof(Configuration));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating GST rates");
                TempData["Error"] = $"Error updating GST rates: {ex.Message}";
                return RedirectToAction(nameof(Configuration));
            }
        }

        #endregion

        #region Update Company GST Details

        /// <summary>
        /// POST: /Gst/UpdateCompanyGst
        /// Updates company GST registration details (GSTIN, Name, State)
        /// </summary>
        /// <param name="gstin">15-digit GSTIN</param>
        /// <param name="companyName">Legal company name</param>
        /// <param name="state">Registered state code</param>
        /// <returns>Redirect to Configuration page with success/error message</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCompanyGst(
            [Bind("CompanyGstin,CompanyName,RegisteredState")] GstConfigurationViewModel model)
        {
            try
            {
                model.CompanyName = model.CompanyName?.Trim() ?? string.Empty;
                model.CompanyGstin = model.CompanyGstin?.Trim().ToUpperInvariant() ?? string.Empty;
                model.RegisteredState = model.RegisteredState?.Trim().ToUpperInvariant() ?? string.Empty;

                if (!IsValidGstin(model.CompanyGstin))
                {
                    TempData["Error"] = "Enter a valid 15-character GSTIN.";
                    return RedirectToAction(nameof(Configuration));
                }

                // Validate company name
                if (string.IsNullOrWhiteSpace(model.CompanyName) || model.CompanyName.Length < 3)
                {
                    TempData["Error"] = "Company name must be at least 3 characters long.";
                    return RedirectToAction(nameof(Configuration));
                }

                if (!IsValidRegisteredState(model.RegisteredState))
                {
                    TempData["Error"] = "Please select a valid state.";
                    return RedirectToAction(nameof(Configuration));
                }

                _logger.LogInformation($"Updating company GST details: GSTIN={model.CompanyGstin}, Company={model.CompanyName}, State={model.RegisteredState}");

                var configuration =
                    await GetOrCreateConfigurationAsync();

                configuration.CompanyName = model.CompanyName;
                configuration.CompanyGstin = model.CompanyGstin;
                configuration.RegisteredState = model.RegisteredState;
                configuration.UpdatedByUserId = _userManager.GetUserId(User);

                await _configurationRepository.SaveActiveAsync(configuration);

                TempData["Success"] = "Company GST details updated successfully.";
                return RedirectToAction(nameof(Configuration));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company GST details");
                TempData["Error"] = $"Error updating company details: {ex.Message}";
                return RedirectToAction(nameof(Configuration));
            }
        }

        #endregion

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReturn(
               int snapshotId,
               string returnType,
               string status,
               string? arnNumber,
               DateTime? filedDate,
               string? remarks)
        {
            var snapshot =
                await _snapshotRepository.GetByIdAsync(snapshotId);

            if (snapshot == null)
                return NotFound();

            if (snapshot.Status == "Filed")
            {
                TempData["Error"] =
                    "Filed GST periods cannot be edited.";

                return RedirectToAction(
                    nameof(Dashboard),
                    new { snapshot.Month, snapshot.Year });
            }

            if (returnType != "GSTR1" &&
                returnType != "GSTR3B")
            {
                TempData["Error"] = "Invalid GST return type.";

                return RedirectToAction(
                    nameof(Dashboard),
                    new { snapshot.Month, snapshot.Year });
            }

            if (status != "Pending" &&
                status != "Filed")
            {
                TempData["Error"] = "Invalid return status.";

                return RedirectToAction(
                    nameof(Dashboard),
                    new { snapshot.Month, snapshot.Year });
            }

            if (status == "Filed")
            {
                if (string.IsNullOrWhiteSpace(arnNumber))
                {
                    TempData["Error"] =
                        "ARN number is required when filing a return.";

                    return RedirectToAction(
                        nameof(Dashboard),
                        new { snapshot.Month, snapshot.Year });
                }

                if (!filedDate.HasValue)
                {
                    TempData["Error"] =
                        "Filing date is required.";

                    return RedirectToAction(
                        nameof(Dashboard),
                        new { snapshot.Month, snapshot.Year });
                }

                if (filedDate.Value.Date > DateTime.Today)
                {
                    TempData["Error"] =
                        "Filing date cannot be in the future.";

                    return RedirectToAction(
                        nameof(Dashboard),
                        new { snapshot.Month, snapshot.Year });
                }

                if (filedDate.Value.Date < GetTaxPeriodStartDate(
                        snapshot.Month,
                        snapshot.Year))
                {
                    TempData["Error"] =
                        "Filing date cannot be before the selected GST period.";

                    return RedirectToAction(
                        nameof(Dashboard),
                        new { snapshot.Month, snapshot.Year });
                }
            }

            var gstReturn =
                await _returnRepository.GetByReturnTypeAsync(
                    snapshotId,
                    returnType);

            if (gstReturn != null &&
                string.Equals(
                    gstReturn.Status,
                    "Filed",
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    status,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Filed GST returns cannot be moved back to Pending without an Admin reopen workflow.";

                return RedirectToAction(
                    nameof(Dashboard),
                    new { snapshot.Month, snapshot.Year });
            }

            if (gstReturn == null)
            {
                gstReturn = new GstReturnModel
                {
                    SnapshotId = snapshotId,
                    ReturnType = returnType,
                    CreatedOn = DateTime.Now
                };

                await _returnRepository.AddAsync(gstReturn);
            }

            gstReturn.Status = status;
            gstReturn.ArnNumber =
                string.IsNullOrWhiteSpace(arnNumber)
                    ? null
                    : arnNumber.Trim();

            gstReturn.FiledDate =
                status == "Filed"
                    ? filedDate
                    : null;

            gstReturn.FiledBy =
                status == "Filed"
                    ? User.Identity?.Name
                    : null;

            gstReturn.Remarks = remarks;
            gstReturn.UpdatedOn = DateTime.Now;

            await _returnRepository.SaveAsync();

            TempData["Success"] =
                $"{returnType} status updated successfully.";

            return RedirectToAction(
                nameof(Dashboard),
                new { snapshot.Month, snapshot.Year });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateChallan(
               int snapshotId,
               string challanNumber,
               string status,
               decimal amountPaid,
               DateTime? paymentDate,
               string? paymentMode,
               string? bankName,
               string? cin,
               string? cpin,
               string? remarks)
        {
            var snapshot =
                await _snapshotRepository.GetByIdAsync(snapshotId);

            if (snapshot == null)
                return NotFound();

            if (snapshot.Status == "Filed")
            {
                TempData["Error"] =
                    "Filed GST periods cannot be edited.";

                return RedirectToAction(
                    nameof(Dashboard),
                    new { snapshot.Month, snapshot.Year });
            }

            if (status != "Pending" &&
                status != "Paid")
            {
                TempData["Error"] = "Invalid challan status.";

                return RedirectToAction(
                    nameof(Dashboard),
                    new { snapshot.Month, snapshot.Year });
            }

            if (string.IsNullOrWhiteSpace(challanNumber))
            {
                TempData["Error"] =
                    "Challan number is required.";

                return RedirectToAction(
                    nameof(Dashboard),
                    new { snapshot.Month, snapshot.Year });
            }

            if (status == "Paid")
            {
                if (amountPaid <= 0)
                {
                    TempData["Error"] =
                        "Paid amount must be greater than zero.";

                    return RedirectToAction(
                        nameof(Dashboard),
                        new { snapshot.Month, snapshot.Year });
                }

                if (!paymentDate.HasValue)
                {
                    TempData["Error"] =
                        "Payment date is required.";

                    return RedirectToAction(
                        nameof(Dashboard),
                        new { snapshot.Month, snapshot.Year });
                }

                if (paymentDate.Value.Date > DateTime.Today)
                {
                    TempData["Error"] =
                        "Challan payment date cannot be in the future.";

                    return RedirectToAction(
                        nameof(Dashboard),
                        new { snapshot.Month, snapshot.Year });
                }

                if (paymentDate.Value.Date < GetTaxPeriodStartDate(
                        snapshot.Month,
                        snapshot.Year))
                {
                    TempData["Error"] =
                        "Challan payment date cannot be before the selected GST period.";

                    return RedirectToAction(
                        nameof(Dashboard),
                        new { snapshot.Month, snapshot.Year });
                }

                if (string.IsNullOrWhiteSpace(cin) ||
                    string.IsNullOrWhiteSpace(cpin))
                {
                    TempData["Error"] =
                        "CIN and CPIN are required when challan status is Paid.";

                    return RedirectToAction(
                        nameof(Dashboard),
                        new { snapshot.Month, snapshot.Year });
                }
            }

            var challan =
                await _challanRepository.GetByChallanNumberAsync(
                    challanNumber.Trim());

            if (challan != null &&
                string.Equals(
                    challan.Status,
                    "Paid",
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    status,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Paid GST challans cannot be moved back to Pending.";

                return RedirectToAction(
                    nameof(Dashboard),
                    new { snapshot.Month, snapshot.Year });
            }

            if (challan == null)
            {
                challan = new GstChallanModel
                {
                    SnapshotId = snapshotId,
                    ChallanNumber = challanNumber.Trim(),
                    CreatedOn = DateTime.Now
                };

                await _challanRepository.AddAsync(challan);
            }
            else if (challan.SnapshotId != snapshotId)
            {
                TempData["Error"] =
                    "This challan number belongs to another GST period.";

                return RedirectToAction(
                    nameof(Dashboard),
                    new { snapshot.Month, snapshot.Year });
            }

            challan.Status = status;
            challan.AmountPaid = amountPaid;
            challan.PaymentDate =
                status == "Paid"
                    ? paymentDate
                    : null;

            challan.PaymentMode = paymentMode;
            challan.BankName = bankName;
            challan.CIN = cin;
            challan.CPIN = cpin;
            challan.Remarks = remarks;
            challan.UpdatedOn = DateTime.Now;

            await _challanRepository.SaveAsync();

            TempData["Success"] =
                "GST challan updated successfully.";

            return RedirectToAction(
                nameof(Dashboard),
                new { snapshot.Month, snapshot.Year });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(
            int snapshotId,
            string documentType,
            IFormFile file,
            string? remarks)
        {
            var snapshot =
                await _snapshotRepository.GetByIdAsync(snapshotId);

            if (snapshot == null)
                return NotFound();

            documentType = documentType?.Trim() ?? string.Empty;
            remarks = remarks?.Trim();

            if (string.IsNullOrWhiteSpace(documentType))
            {
                TempData["Error"] = "Please select a document type.";

                return RedirectToAction(
                    nameof(Dashboard),
                    new { snapshot.Month, snapshot.Year });
            }

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";

                return RedirectToAction(
                    nameof(Dashboard),
                    new { snapshot.Month, snapshot.Year });
            }

            var uploadResult =
                await _fileStorageService.UploadAsync(file, DocumentFolder);

            if (!uploadResult.Success)
            {
                TempData["Error"] = uploadResult.ErrorMessage;

                return RedirectToAction(
                    nameof(Dashboard),
                    new { snapshot.Month, snapshot.Year });
            }

            var document = new GstDocumentModel
            {
                SnapshotId = snapshotId,
                DocumentType = documentType,
                FileName = uploadResult.OriginalFileName,
                FilePath = uploadResult.RelativePath,
                UploadedByUserId = _userManager.GetUserId(User),
                Remarks = remarks
            };

            await _documentRepository.AddAsync(document);
            await _documentRepository.SaveAsync();

            TempData["Success"] = "GST document uploaded successfully.";

            return RedirectToAction(
                nameof(Dashboard),
                new { snapshot.Month, snapshot.Year });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var document =
                await _documentRepository.GetByIdAsync(id);

            if (document == null)
                return NotFound();

            var fileBytes =
                await _fileStorageService.DownloadAsync(document.FilePath);

            if (fileBytes == null)
            {
                TempData["Error"] = "Document file was not found.";

                int month =
                    document.Snapshot?.Month ?? DateTime.Now.Month;

                int year =
                    document.Snapshot?.Year ?? DateTime.Now.Year;

                return RedirectToAction(
                    nameof(Dashboard),
                    new { month, year });
            }

            return File(
                fileBytes,
                GetContentType(document.FileName),
                Path.GetFileName(document.FileName));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(
            int id,
            int snapshotId)
        {
            var document =
                await _documentRepository.GetByIdAsync(id);

            if (document == null)
                return NotFound();

            var snapshot =
                document.Snapshot
                ?? await _snapshotRepository.GetByIdAsync(document.SnapshotId);

            if (snapshot == null)
                return NotFound();

            if (string.Equals(
                    snapshot.Status,
                    "Filed",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Documents linked to a filed GST snapshot cannot be deleted.";

                return RedirectToAction(
                    nameof(Dashboard),
                    new { snapshot.Month, snapshot.Year });
            }

            await _documentRepository.DeleteAsync(document);
            await _documentRepository.SaveAsync();

            await _fileStorageService.DeleteAsync(document.FilePath);

            TempData["Success"] = "GST document deleted successfully.";

            return RedirectToAction(
                nameof(Dashboard),
                new { snapshot.Month, snapshot.Year });
        }

        #region Helper Methods

        private DateTime GetTaxPeriodStartDate(int month, int year)
        {
            return new DateTime(year, month, 1);
        }

        /// <summary>
        /// Get financial year from month and calendar year
        /// Financial year in India: April (Month 4) to March (Month 3)
        /// e.g., April 2026 - March 2027 = FY 2026-27
        /// </summary>
        private string GetFinancialYear(int month, int year)
        {
            try
            {
                int fyStart = month < 4 ? year - 1 : year;
                int fyEnd = fyStart + 1;
                return $"{fyStart}-{fyEnd.ToString().Substring(2)}";
            }
            catch
            {
                return $"{year}-{year + 1}";
            }
        }

        /// <summary>
        /// Get month name from month number
        /// </summary>
        private string GetMonthName(int month)
        {
            return new System.Globalization.CultureInfo("en-US")
                .DateTimeFormat.GetMonthName(month);
        }

        private async Task<GstConfigurationModel> GetOrCreateConfigurationAsync()
        {
            var configuration =
                await _configurationRepository.GetActiveAsync();

            if (configuration != null)
                return configuration;

            return new GstConfigurationModel
            {
                CompanyName = "Aryaman Technologies Private Limited",
                CompanyGstin = "27AABCA1234A1Z5",
                RegisteredState = "MH",
                CgstRate = 9,
                SgstRate = 9,
                IgstRate = 18,
                IsActive = true
            };
        }

        private bool AreValidGstRates(
            decimal cgstRate,
            decimal sgstRate,
            decimal igstRate)
        {
            if (cgstRate < 0 || cgstRate > 100)
                return false;

            if (sgstRate < 0 || sgstRate > 100)
                return false;

            if (igstRate < 0 || igstRate > 100)
                return false;

            return igstRate == cgstRate + sgstRate;
        }

        private bool IsValidGstin(string gstin)
        {
            return Regex.IsMatch(
                gstin,
                @"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$");
        }

        private bool IsValidRegisteredState(string stateCode)
        {
            var states = new HashSet<string>
            {
                "AN", "AP", "AR", "AS", "BR", "CH", "CG", "DD", "DL",
                "DN", "GA", "GJ", "HR", "HP", "JK", "JH", "KA", "KL",
                "LA", "LD", "MP", "MH", "MN", "ML", "MZ", "NL", "OD",
                "PY", "PB", "RJ", "SK", "TN", "TS", "TR", "UP", "UK",
                "WB"
            };

            return states.Contains(stateCode);
        }

        private static string GetContentType(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }

        #endregion

        #region Optional API Methods

        /// <summary>
        /// GET: /Gst/Api/GetSnapshot
        /// API endpoint to get GST snapshot data (for AJAX calls)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSnapshot(int month, int year)
        {
            try
            {
                if (month < 1 || month > 12)
                    return BadRequest("Invalid month");

                var snapshot = await _snapshotRepository.GetByMonthYearAsync(month, year);

                if (snapshot == null)
                    return NotFound("No GST snapshot found for this period");

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        month = snapshot.Month,
                        year = snapshot.Year,
                        outputGst = snapshot.TotalOutputGST,
                        inputGst = snapshot.TotalInputGST,
                        netGstPayable = snapshot.NetGSTPayable,
                        status = snapshot.Status,
                        invoiceCount = snapshot.InvoiceCount,
                        expenseCount = snapshot.ExpenseVoucherCount
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching GST snapshot data");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST: /Gst/Api/LockSnapshot
        /// Lock GST snapshot after filing (prevent recalculation)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockSnapshot(
    int month,
    int year)
        {
            if (month < 1 || month > 12)
            {
                TempData["Error"] = "Invalid month.";

                return RedirectToAction(nameof(Index));
            }

            if (year < 2000 ||
                year > DateTime.Now.Year + 1)
            {
                TempData["Error"] = "Invalid year.";

                return RedirectToAction(nameof(Index));
            }

            var snapshot =
                await _snapshotRepository.GetByMonthYearAsync(
                    month,
                    year);

            if (snapshot == null)
            {
                TempData["Error"] =
                    "Generate the GST snapshot before filing it.";

                return RedirectToAction(
                    nameof(Dashboard),
                    new { month, year });
            }

            if (string.Equals(
                    snapshot.Status,
                    "Filed",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "This GST snapshot is already filed and locked.";

                return RedirectToAction(
                    nameof(Dashboard),
                    new { month, year });
            }

            bool gstr1Filed = snapshot.Returns.Any(x =>
               x.ReturnType == "GSTR1" &&
               x.Status == "Filed");

            bool gstr3bFiled = snapshot.Returns.Any(x =>
                x.ReturnType == "GSTR3B" &&
                x.Status == "Filed");

            bool challanPaid = snapshot.Challans.Any(x =>
                x.Status == "Paid");

            if (!gstr1Filed || !gstr3bFiled || !challanPaid)
            {
                TempData["Error"] =
                    "GSTR-1, GSTR-3B and GST challan must be completed before locking this period.";

                return RedirectToAction(
                    nameof(Dashboard),
                    new { month, year });
            }

            bool locked =
                await _snapshotRepository.LockAsync(
                    month,
                    year);

            if (!locked)
            {
                TempData["Error"] =
                    "GST snapshot could not be locked.";

                return RedirectToAction(
                    nameof(Dashboard),
                    new { month, year });
            }

            TempData["Success"] =
                $"GST snapshot for {GetMonthName(month)} {year} " +
                "was filed and locked successfully.";

            return RedirectToAction(
                nameof(Dashboard),
                new { month, year });
        }

        #endregion
    }
}
