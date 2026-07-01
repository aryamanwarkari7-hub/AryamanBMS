using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly ILogger<GstController> _logger;

        /// <summary>
        /// Constructor with dependency injection
        /// </summary>
        public GstController(
            IGstCalculationService calculationService,
            IGstDashboardService dashboardService,
            IGstSnapshotRepository snapshotRepository,
            ILogger<GstController> logger)
        {
            _calculationService = calculationService
                ?? throw new ArgumentNullException(nameof(calculationService));
            _dashboardService = dashboardService
                ?? throw new ArgumentNullException(nameof(dashboardService));
            _snapshotRepository = snapshotRepository
                ?? throw new ArgumentNullException(nameof(snapshotRepository));
            _logger = logger
                ?? throw new ArgumentNullException(nameof(logger));
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
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating GST snapshot for {month}/{year}");
                TempData["Error"] = $"Error generating GST snapshot: {ex.Message}";
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

                // TODO: Load GST configuration from database
                // For now, return hardcoded defaults
                var model = new GstConfigurationViewModel
                {
                    SgstRate = 9,
                    CgstRate = 9,
                    IgstRate = 18,
                    CompanyGstin = "27AABCR5055N1Z0",
                    CompanyName = "Your Company Name",
                    RegisteredState = "MH",
                    LastUpdatedOn = DateTime.Now
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
                // Validate model
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors);
                    _logger.LogWarning($"Invalid GST rates: {string.Join(", ", errors.Select(e => e.ErrorMessage))}");
                    TempData["Error"] = "Please enter valid GST rates (0-100).";
                    return RedirectToAction(nameof(Configuration));
                }

                // Validate rate ranges
                if (model.SgstRate < 0 || model.SgstRate > 100)
                {
                    TempData["Error"] = "SGST rate must be between 0 and 100.";
                    return RedirectToAction(nameof(Configuration));
                }

                if (model.CgstRate < 0 || model.CgstRate > 100)
                {
                    TempData["Error"] = "CGST rate must be between 0 and 100.";
                    return RedirectToAction(nameof(Configuration));
                }

                if (model.IgstRate < 0 || model.IgstRate > 100)
                {
                    TempData["Error"] = "IGST rate must be between 0 and 100.";
                    return RedirectToAction(nameof(Configuration));
                }

                _logger.LogInformation($"Updating GST rates: SGST={model.SgstRate}%, CGST={model.CgstRate}%, IGST={model.IgstRate}%");

                // TODO: Save GST rates to database
                // var gstConfig = new GstConfigurationModel
                // {
                //     SgstRate = model.SgstRate,
                //     CgstRate = model.CgstRate,
                //     IgstRate = model.IgstRate,
                //     LastUpdatedOn = DateTime.Now,
                //     UpdatedByUserId = GetCurrentUserId()
                // };
                // await _gstConfigRepository.SaveAsync(gstConfig);

                TempData["Warning"] = "GST rate saving is not implemented yet.";
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
                // Validate model
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors);
                    _logger.LogWarning($"Invalid company GST details: {string.Join(", ", errors.Select(e => e.ErrorMessage))}");
                    TempData["Error"] = "Please enter valid company details.";
                    return RedirectToAction(nameof(Configuration));
                }

                // Validate GSTIN format (15 characters)
                if (string.IsNullOrWhiteSpace(model.CompanyGstin) || model.CompanyGstin.Length != 15)
                {
                    TempData["Error"] = "GSTIN must be exactly 15 characters long.";
                    return RedirectToAction(nameof(Configuration));
                }

                // Validate company name
                if (string.IsNullOrWhiteSpace(model.CompanyName) || model.CompanyName.Length < 3)
                {
                    TempData["Error"] = "Company name must be at least 3 characters long.";
                    return RedirectToAction(nameof(Configuration));
                }

                // Validate state
                if (string.IsNullOrWhiteSpace(model.RegisteredState) || model.RegisteredState.Length != 2)
                {
                    TempData["Error"] = "Please select a valid state.";
                    return RedirectToAction(nameof(Configuration));
                }

                _logger.LogInformation($"Updating company GST details: GSTIN={model.CompanyGstin}, Company={model.CompanyName}, State={model.RegisteredState}");

                // TODO: Update company profile in database
                // var company = await _companyService.GetCompanyAsync();
                // company.Gstin = model.CompanyGstin;
                // company.CompanyName = model.CompanyName;
                // company.RegisteredState = model.RegisteredState;
                // company.UpdatedOn = DateTime.Now;
                // await _companyService.UpdateAsync(company);

                TempData["Warning"] = "Company GST detail saving is not implemented yet.";
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

        #region Helper Methods

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

        /// <summary>
        /// Get current logged-in user ID
        /// TODO: Implement if needed
        /// </summary>
        private int GetCurrentUserId()
        {
            // Implementation would depend on your user management system
            return 0;
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
        public async Task<IActionResult> LockSnapshot(int month, int year)
        {
            TempData["Error"] = "GST snapshot filing lock is not implemented yet.";
            return RedirectToAction(nameof(Dashboard), new { month, year });
        }

        #endregion
    }
}