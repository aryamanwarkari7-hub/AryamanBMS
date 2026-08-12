using AryamanBMS.Extensions;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interface;
using AryamanBMS.Services.Interfaces;
using AryamanBMS.ViewModels;
using ClosedXML.Excel;
using AryamanBMS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AryamanBMS.Controllers
{
    [Authorize]
    public class SalaryController : Controller
    {
        #region Actions

        private readonly ISalaryRecordRepository _salaryRecordRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<SalaryController> _logger;

        // Service
        private readonly ISalaryExcelImportService _salaryExcelImportService;
        private readonly ISalaryAttendanceSummaryService _salaryAttendanceSummaryService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public SalaryController(
      ISalaryRecordRepository salaryRecordRepository,
      IEmployeeRepository employeeRepository,
      UserManager<ApplicationUserModel> userManager,
      ISalaryExcelImportService salaryExcelImportService,
      ISalaryAttendanceSummaryService salaryAttendanceSummaryService,
      IWebHostEnvironment webHostEnvironment,
      ApplicationDbContext context,
      INotificationService notificationService,
      ILogger<SalaryController> logger)
        {
            _salaryRecordRepository = salaryRecordRepository;
            _employeeRepository = employeeRepository;
            _userManager = userManager;
            _salaryExcelImportService = salaryExcelImportService;
            _salaryAttendanceSummaryService = salaryAttendanceSummaryService;
            _webHostEnvironment = webHostEnvironment;
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Index(
          int? month,
          int? year,
          int page = 1)
        {
            const int pageSize = 10;

            int selectedMonth =
                month.HasValue &&
                month.Value >= 1 &&
                month.Value <= 12
                    ? month.Value
                    : DateTime.Today.Month;

            int selectedYear =
                year ?? DateTime.Today.Year;
            var selectedPeriod =
                new DateTime(
                    selectedYear,
                    selectedMonth,
                    1);

            var currentPeriod =
                new DateTime(
                    DateTime.Today.Year,
                    DateTime.Today.Month,
                    1);

            if (selectedPeriod > currentPeriod)
            {
                ViewBag.EmptyMessage =
                    $"Salary records are not available for " +
                    $"{selectedPeriod:MMMM yyyy} because this is a future payroll period.";
            }
            else
            {
                ViewBag.EmptyMessage =
                    $"No salary records found for {selectedPeriod:MMMM yyyy}. " +
                    "Please export the salary template, complete it, and import the final salary Excel.";
            }

            var query = _salaryRecordRepository.SalaryRecords
                .AsNoTracking()
                .Include(x => x.Employee)
                .Where(x =>
                    x.Month == selectedMonth &&
                    x.Year == selectedYear)
                .OrderBy(x => x.Employee!.EmployeeCode)
                .ThenBy(x => x.Id);

            var routeValues =
                new Dictionary<string, string>
                {
                    ["month"] = selectedMonth.ToString(),
                    ["year"] = selectedYear.ToString()
                };

            var model = await query.ToPagedListAsync(
                page,
                pageSize,
                routeValues);

            model.Pagination.ControllerName = "Salary";
            model.Pagination.ActionName = nameof(Index);

            ViewBag.Month = selectedMonth;
            ViewBag.Year = selectedYear;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public IActionResult Generate(int month, int year)
        {
            TempData["Error"] =
                "Salary generation is disabled. Please upload the final salary Excel.";

            return RedirectToAction(
                nameof(Index),
                new { month, year });
        }

        [HttpGet]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(int id)
        {
            var salary =
                await _salaryRecordRepository
                .GetByIdAsync(id);

            if (salary == null)
            {
                return NotFound();
            }

            if (salary.PayrollStatus == "Finalized")
            {
                TempData["Error"] =
                    "Finalized salary records cannot be edited. Reopen payroll before making corrections.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            return View(salary);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(SalaryRecordModel salary)
        {
            var existing =
                await _salaryRecordRepository
                    .GetByIdAsync(salary.Id);

            if (existing == null)
            {
                return NotFound();
            }

            if (existing.PayrollStatus == "Finalized")
            {
                TempData["Error"] =
                    "Finalized salary records cannot be edited. Reopen payroll before making corrections.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = salary.Id });
            }

            if (ModelState.IsValid)
            {
                existing.BasicSalary = salary.BasicSalary;
                existing.HRA = salary.HRA;
                existing.DA = salary.DA;
                existing.OtherAllowances = salary.OtherAllowances;
                existing.PfDeduction = salary.PfDeduction;
                existing.EsicDeduction = salary.EsicDeduction;
                existing.TdsDeduction = salary.TdsDeduction;
                existing.OtherDeductions = salary.OtherDeductions;
                existing.GrossSalary = salary.GrossSalary;
                existing.TotalEarnings = salary.GrossSalary;
                existing.TotalDeductions =
                    salary.PfDeduction +
                    salary.EsicDeduction +
                    existing.ProfessionalTax +
                    existing.Advance +
                    salary.TdsDeduction +
                    salary.OtherDeductions;
                existing.NetSalary = salary.NetSalary;
                existing.UpdatedByUserId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);
                existing.UpdatedOn = DateTime.Now;

                await _salaryRecordRepository
                    .UpdateAsync(existing);

                await _salaryRecordRepository
                    .SaveAsync();

                TempData["Success"] =
                    "Salary updated successfully.";

                return RedirectToAction(
                    nameof(Index));
            }

            return View(salary);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> MarkPaid(
                 int id,
                 int month,
                 int year,
                 int page = 1)
        {
            var salary =
                await _salaryRecordRepository.GetByIdAsync(id);

            if (salary == null)
            {
                return NotFound();
            }

            if (salary.PayrollStatus != "Finalized")
            {
                TempData["Error"] =
                    "Salary can be marked paid only after payroll is finalized.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        month,
                        year,
                        page
                    });
            }

            string previousPaymentStatus =
                salary.PaymentStatus;

            salary.PaymentStatus = "Paid";
            salary.PaidOn = DateTime.Now;
            salary.PaidByUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            await _salaryRecordRepository.UpdateAsync(salary);
            await _salaryRecordRepository.SaveAsync();

            if (!string.Equals(
                    previousPaymentStatus,
                    "Paid",
                    StringComparison.OrdinalIgnoreCase))
            {
                await NotifyEmployeeSalaryAsync(
                    salary,
                    notificationType: "SalaryPaid",
                    title: "Salary Paid",
                    message:
                        $"Salary for {GetMonthName(salary.Month)} {salary.Year} " +
                        $"has been paid. Net salary: ₹{salary.NetSalary:N2}.",
                    actionUrl: "/Salary/MySalary");
            }

            TempData["Success"] =
                "Salary marked as paid.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    month,
                    year,
                    page
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Verify(
            int id,
            int month,
            int year,
            int page = 1)
        {
            var salary =
                await _salaryRecordRepository.GetByIdAsync(id);

            if (salary == null)
            {
                return NotFound();
            }

            if (salary.PayrollStatus is "Finalized")
            {
                TempData["Error"] =
                    "Finalized payroll is already locked.";
            }
            else
            {
                salary.PayrollStatus = "Verified";
                salary.VerifiedByUserId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);
                salary.VerifiedOn = DateTime.Now;

                await _salaryRecordRepository.UpdateAsync(salary);
                await _salaryRecordRepository.SaveAsync();

                TempData["Success"] =
                    "Salary record verified.";
            }

            return RedirectToAction(
                nameof(Index),
                new { month, year, page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> FinalizePayroll(
            int id,
            int month,
            int year,
            int page = 1)
        {
            var salary =
                await _salaryRecordRepository.GetByIdAsync(id);

            if (salary == null)
            {
                return NotFound();
            }

            if (salary.PayrollStatus != "Verified")
            {
                TempData["Error"] =
                    "Only verified salary records can be finalized.";

                return RedirectToAction(
                    nameof(Index),
                    new { month, year, page });
            }

            salary.PayrollStatus = "Finalized";
            salary.FinalizedByUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);
            salary.FinalizedOn = DateTime.Now;

            await _salaryRecordRepository.UpdateAsync(salary);
            await _salaryRecordRepository.SaveAsync();

            TempData["Success"] =
                "Salary record finalized.";

            return RedirectToAction(
                nameof(Index),
                new { month, year, page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Reopen(
            int id,
            int month,
            int year,
            string reopenReason,
            int page = 1)
        {
            var salary =
                await _salaryRecordRepository.GetByIdAsync(id);

            if (salary == null)
            {
                return NotFound();
            }

            if (salary.PayrollStatus != "Finalized")
            {
                TempData["Error"] =
                    "Only finalized salary records can be reopened.";
            }
            else if (string.IsNullOrWhiteSpace(reopenReason))
            {
                TempData["Error"] =
                    "Reopen reason is required.";
            }
            else
            {
                salary.PayrollStatus = "Reopened";
                salary.ReopenReason = reopenReason.Trim();
                salary.ReopenedByUserId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);
                salary.ReopenedOn = DateTime.Now;
                salary.IsPayslipReleased = false;

                await _salaryRecordRepository.UpdateAsync(salary);
                await _salaryRecordRepository.SaveAsync();

                TempData["Success"] =
                    "Salary record reopened.";
            }

            return RedirectToAction(
                nameof(Index),
                new { month, year, page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> ReleasePayslip(
            int id,
            int month,
            int year,
            int page = 1)
        {
            var salary =
                await _salaryRecordRepository.GetByIdAsync(id);

            if (salary == null)
            {
                return NotFound();
            }

            if (salary.PayrollStatus != "Finalized")
            {
                TempData["Error"] =
                    "Payslip can be released only after payroll is finalized.";
            }
            else
            {
                bool wasPayslipReleased =
                    salary.IsPayslipReleased;

                salary.IsPayslipReleased = true;
                salary.PayslipReleasedByUserId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);
                salary.PayslipReleasedOn = DateTime.Now;

                await _salaryRecordRepository.UpdateAsync(salary);
                await _salaryRecordRepository.SaveAsync();

                if (!wasPayslipReleased)
                {
                    await NotifyEmployeeSalaryAsync(
                        salary,
                        notificationType: "PayslipReleased",
                        title: "Payslip Released",
                        message:
                            $"Your payslip for {GetMonthName(salary.Month)} {salary.Year} " +
                            "has been released.",
                        actionUrl: $"/Salary/Payslip/{salary.Id}");
                }

                TempData["Success"] =
                    "Payslip released.";
            }

            return RedirectToAction(
                nameof(Index),
                new { month, year, page });
        }

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Dashboard(
    string viewType = "Monthly",
    int? month = null,
    int? year = null)
        {
            int selectedMonth =
                month.HasValue &&
                month.Value >= 1 &&
                month.Value <= 12
                    ? month.Value
                    : DateTime.Today.Month;

            int selectedYear = year ?? DateTime.Today.Year;

            bool isYearly = string.Equals(
                viewType,
                "Yearly",
                StringComparison.OrdinalIgnoreCase);

            viewType = isYearly ? "Yearly" : "Monthly";

            var yearSalaries = await _salaryRecordRepository.SalaryRecords
                .AsNoTracking()
                .Include(x => x.Employee)
                .Where(x => x.Year == selectedYear)
                .ToListAsync();

            var selectedSalaries = isYearly
                ? yearSalaries
                : yearSalaries
                    .Where(x => x.Month == selectedMonth)
                    .ToList();

            int totalRecords = selectedSalaries.Count;

            int paidCount = selectedSalaries.Count(x =>
                string.Equals(x.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase));

            int pendingCount = totalRecords - paidCount;

            int finalizedCount = selectedSalaries.Count(x =>
                string.Equals(x.PayrollStatus, "Finalized", StringComparison.OrdinalIgnoreCase));

            int verifiedCount = selectedSalaries.Count(x =>
                string.Equals(x.PayrollStatus, "Verified", StringComparison.OrdinalIgnoreCase));

            int draftCount = selectedSalaries.Count(x =>
                string.Equals(x.PayrollStatus, "Draft", StringComparison.OrdinalIgnoreCase));

            int payslipReleasedCount = selectedSalaries.Count(x => x.IsPayslipReleased);

            int payslipPendingCount = selectedSalaries.Count(x =>
                string.Equals(x.PayrollStatus, "Finalized", StringComparison.OrdinalIgnoreCase) &&
                !x.IsPayslipReleased);

            var model = new SalaryDashboardViewModel
            {
                ViewType = viewType,
                Month = selectedMonth,
                Year = selectedYear,

                TotalEmployees = selectedSalaries
                    .Select(x => x.EmployeeId)
                    .Distinct()
                    .Count(),

                PaidCount = paidCount,
                PendingCount = pendingCount,

                FinalizedCount = finalizedCount,
                VerifiedCount = verifiedCount,
                DraftCount = draftCount,

                PayslipReleasedCount = payslipReleasedCount,
                PayslipPendingCount = payslipPendingCount,

                TotalGrossSalary = selectedSalaries.Sum(x => x.GrossSalary),
                TotalNetSalary = selectedSalaries.Sum(x => x.NetSalary),
                TotalDeductions = selectedSalaries.Sum(x => x.TotalDeductions),

                PayrollCompletionPercentage = totalRecords > 0
                    ? Math.Round((decimal)paidCount / totalRecords * 100, 2)
                    : 0,

                PayslipReleasePercentage = totalRecords > 0
                    ? Math.Round((decimal)payslipReleasedCount / totalRecords * 100, 2)
                    : 0,

                FinalizationPercentage = totalRecords > 0
                    ? Math.Round((decimal)finalizedCount / totalRecords * 100, 2)
                    : 0,

                TotalBasic = selectedSalaries.Sum(x => x.BasicSalary),
                TotalHRA = selectedSalaries.Sum(x => x.HRA),
                TotalDA = selectedSalaries.Sum(x => x.DA),

                TotalOtherAllowances = selectedSalaries.Sum(x =>
                    x.Conveyance +
                    x.MedicalAllowance +
                    x.EducationAllowance +
                    x.SpecialAllowance +
                    x.OtherAllowances),

                TotalPF = selectedSalaries.Sum(x => x.PfDeduction),
                TotalESIC = selectedSalaries.Sum(x => x.EsicDeduction),
                TotalTDS = selectedSalaries.Sum(x => x.TdsDeduction),

                TotalOtherDeductions = selectedSalaries.Sum(x =>
                    x.ProfessionalTax +
                    x.Advance +
                    x.OtherDeductions)
            };

            model.PaymentBuckets = BuildSalaryBuckets(new List<SalaryDashboardBucket>
    {
        new() { Label = "Paid", Count = paidCount, CssClass = "bucket-success" },
        new() { Label = "Pending", Count = pendingCount, CssClass = "bucket-warning" }
    });

            model.PayrollStatusBuckets = BuildSalaryBuckets(new List<SalaryDashboardBucket>
    {
        new() { Label = "Draft", Count = draftCount, CssClass = "bucket-neutral" },
        new() { Label = "Verified", Count = verifiedCount, CssClass = "bucket-info" },
        new() { Label = "Finalized", Count = finalizedCount, CssClass = "bucket-success" }
    });

            model.PayComponentBuckets = BuildSalaryAmountBuckets(new List<SalaryDashboardBucket>
    {
        new() { Label = "Basic", Amount = model.TotalBasic, CssClass = "bucket-success" },
        new() { Label = "HRA", Amount = model.TotalHRA, CssClass = "bucket-info" },
        new() { Label = "DA", Amount = model.TotalDA, CssClass = "bucket-warning" },
        new() { Label = "Other", Amount = model.TotalOtherAllowances, CssClass = "bucket-neutral" }
    });

            model.DeductionBuckets = BuildSalaryAmountBuckets(new List<SalaryDashboardBucket>
    {
        new() { Label = "PF", Amount = model.TotalPF, CssClass = "bucket-info" },
        new() { Label = "ESIC", Amount = model.TotalESIC, CssClass = "bucket-warning" },
        new() { Label = "TDS", Amount = model.TotalTDS, CssClass = "bucket-danger" },
        new() { Label = "Other", Amount = model.TotalOtherDeductions, CssClass = "bucket-neutral" }
    });

            model.PendingPayments = selectedSalaries
                .Where(x => !string.Equals(x.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.NetSalary)
                .Take(8)
                .Select(ToSalaryDashboardListItem)
                .ToList();

            model.PendingFinalization = selectedSalaries
                .Where(x => !string.Equals(x.PayrollStatus, "Finalized", StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Employee!.FirstName)
                .Take(8)
                .Select(ToSalaryDashboardListItem)
                .ToList();

            model.PendingPayslips = selectedSalaries
                .Where(x =>
                    string.Equals(x.PayrollStatus, "Finalized", StringComparison.OrdinalIgnoreCase) &&
                    !x.IsPayslipReleased)
                .OrderBy(x => x.Employee!.FirstName)
                .Take(8)
                .Select(ToSalaryDashboardListItem)
                .ToList();

            if (isYearly)
            {
                model.MonthlySummaries = yearSalaries
                    .GroupBy(x => x.Month)
                    .OrderBy(x => x.Key)
                    .Select(group =>
                    {
                        int monthlyPaid = group.Count(x =>
                            string.Equals(x.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase));

                        return new SalaryDashboardViewModel.MonthlySalarySummaryViewModel
                        {
                            Month = group.Key,
                            MonthName = new DateTime(selectedYear, group.Key, 1).ToString("MMMM"),
                            EmployeeCount = group.Select(x => x.EmployeeId).Distinct().Count(),
                            PaidCount = monthlyPaid,
                            PendingCount = group.Count() - monthlyPaid,
                            GrossSalary = group.Sum(x => x.GrossSalary),
                            NetSalary = group.Sum(x => x.NetSalary)
                        };
                    })
                    .ToList();
            }

            ViewBag.ViewType = viewType;
            ViewBag.Month = selectedMonth;
            ViewBag.Year = selectedYear;

            return View(model);
        }


        private static List<SalaryDashboardBucket> BuildSalaryBuckets(
    List<SalaryDashboardBucket> buckets)
        {
            int total = buckets.Sum(x => x.Count);

            foreach (var bucket in buckets)
            {
                bucket.Percent = total == 0
                    ? 0
                    : Math.Round((decimal)bucket.Count / total * 100, 2);
            }

            return buckets;
        }

        private static List<SalaryDashboardBucket> BuildSalaryAmountBuckets(
            List<SalaryDashboardBucket> buckets)
        {
            decimal total = buckets.Sum(x => x.Amount);

            foreach (var bucket in buckets)
            {
                bucket.Percent = total == 0
                    ? 0
                    : Math.Round(bucket.Amount / total * 100, 2);
            }

            return buckets;
        }

        private static SalaryDashboardListItem ToSalaryDashboardListItem(
            SalaryRecordModel salary)
        {
            return new SalaryDashboardListItem
            {
                SalaryRecordId = salary.Id,
                EmployeeId = salary.EmployeeId,
                EmployeeName = salary.Employee?.FullName ?? "-",
                EmployeeCode = salary.Employee?.EmployeeCode ?? string.Empty,
                Meta = $"{salary.Month:00}/{salary.Year}",
                Badge = $"{salary.PayrollStatus} / {salary.PaymentStatus}",
                Amount = salary.NetSalary
            };
        }



        [HttpGet]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Details(int id)
        {
            var salary =
                await _salaryRecordRepository
                .GetByIdAsync(id);

            if (salary == null)
            {
                return NotFound();
            }

            return View(salary);
        }

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> ExportExcel(int month, int year)
        {
            if (month == 0)
            {
                month = DateTime.Today.Month;
            }

            if (year == 0)
            {
                year = DateTime.Today.Year;
            }

            var templatePath = Path.Combine(
                _webHostEnvironment.WebRootPath,
                "templates",
                "SalaryTemplate.xlsx");

            if (!System.IO.File.Exists(templatePath))
            {
                TempData["Error"] =
                    "Salary Excel template not found. Please add SalaryTemplate.xlsx in wwwroot/templates.";

                return RedirectToAction(nameof(Index), new { month, year });
            }

            var attendanceSummary = await _salaryAttendanceSummaryService
                .GetMonthlySummaryAsync(month, year);

            var employees = await _employeeRepository.Employees
                .Where(e => e.IsActive)
                .ToListAsync();

            var previousSalaryRecords = await _salaryRecordRepository.SalaryRecords
                .ToListAsync();

            

            var selectedPeriodEnd =
                new DateTime(year, month, DateTime.DaysInMonth(year, month));
            var selectedPeriodStart =
                new DateTime(year, month, 1);

            var salaryStructures =
              await _context.EmployeeSalaryStructures
                  .AsNoTracking()
                  .Where(x => x.IsActive)
                  .ToListAsync();

            using var workbook = new XLWorkbook(templatePath);

            var worksheet = workbook.Worksheet("Salary");

            // Helper cells used by Excel formulas
            worksheet.Cell("AB1").Value = month;
            worksheet.Cell("AB2").Value = year;
            worksheet.Cell("AB3").Value = DateTime.DaysInMonth(year, month);

            int startRow = 4;
            int maxRows = 200;

            // Clear input columns only. Do not clear formula columns.
            for (int clearRow = startRow; clearRow <= maxRows; clearRow++)
            {
                worksheet.Cell(clearRow, 1).Clear(XLClearOptions.Contents);   // S. No.
                worksheet.Cell(clearRow, 2).Clear(XLClearOptions.Contents);   // Emp ID
                worksheet.Cell(clearRow, 3).Clear(XLClearOptions.Contents);   // DB Name
                worksheet.Cell(clearRow, 4).Clear(XLClearOptions.Contents);   // Actual Salary
                worksheet.Cell(clearRow, 5).Clear(XLClearOptions.Contents);   // Pay Days
                worksheet.Cell(clearRow, 18).Clear(XLClearOptions.Contents);  // Advance
                worksheet.Cell(clearRow, 20).Clear(XLClearOptions.Contents);  // Remark
                worksheet.Cell(clearRow, 24).Clear(XLClearOptions.Contents);  // Gender
            }

            int row = startRow;
            int serialNo = 1;

            int[] formulaColumns =
             {
                 6, 7, 8, 9, 10, 11, 12,
                 13, 14, 15, 16, 17,
                 19,
                 21, 22, 23
             };

            foreach (var item in attendanceSummary)
            {
                var employee = employees
                    .FirstOrDefault(e => e.Id == item.EmployeeId);

                if (employee == null)
                {
                    continue;
                }

                var latestSalary =
                  previousSalaryRecords
                      .Where(s =>
                          s.EmployeeId == employee.Id &&
                          (
                              s.Year < year ||
                              (s.Year == year && s.Month <= month)
                          ))
                      .OrderByDescending(s => s.Year)
                      .ThenByDescending(s => s.Month)
                      .ThenByDescending(s => s.ImportedOn)
                      .FirstOrDefault();
                  
                var latestSalaryStructure =
              salaryStructures
                  .Where(x =>
                      x.EmployeeId == employee.Id &&
                      x.EffectiveFrom.Date <= selectedPeriodEnd &&
                      (!x.EffectiveTo.HasValue ||
                       x.EffectiveTo.Value.Date >= selectedPeriodStart) &&
                      x.IsActive)
                  .OrderByDescending(x => x.EffectiveFrom)
                  .ThenByDescending(x => x.Id)
                  .FirstOrDefault()
              ?? salaryStructures
                  .Where(x =>
                      x.EmployeeId == employee.Id &&
                      x.IsActive &&
                      x.EffectiveFrom.Date <= selectedPeriodEnd)
                  .OrderByDescending(x => x.EffectiveFrom)
                  .ThenByDescending(x => x.Id)
                  .FirstOrDefault();

                decimal actualSalary = latestSalaryStructure?.ActualSalary
                 ?? latestSalary?.ActualSalary
                 ?? 0;



                worksheet.Cell(row, 1).Value = serialNo;
                worksheet.Cell(row, 2).Value = employee.EmployeeCode;
                worksheet.Cell(row, 3).Value = employee.FullName;
                worksheet.Cell(row, 4).Value = actualSalary;
                worksheet.Cell(row, 5).Value = item.PayDays;
                worksheet.Cell(row, 18).Value = 0;
                worksheet.Cell(row, 20).Value = "";
                worksheet.Cell(row, 24).Value = employee.Gender ?? "";

                foreach (int formulaColumn in formulaColumns)
                {
                    var templateCell =
                        worksheet.Cell(startRow, formulaColumn);

                    var targetCell =
                        worksheet.Cell(row, formulaColumn);

                    if (templateCell.HasFormula)
                    {
                        targetCell.FormulaR1C1 =
                            templateCell.FormulaR1C1;
                    }
                }

                row++;
                serialNo++;
            }
            int totalRow = row;

            worksheet.Cell(totalRow, 3).Value = "Grand Total";

            // Sum numeric columns
            int[] sumColumns =
            {
               4,  // Actual Salary
               5,  // Pay Days
               6,  // Gross Salary
               7,  // BASIC
               8,  // HRA
               9,  // Conveyance
               10, // Medical Allowance
               11, // Education Allowance
               12, // Special Allowance
               13, // TOTAL
               14, // Gross - Conveyance
               15, // PF Employee
               16, // ESIC Employee
               17, // Professional Tax
               18, // Advance
               19, // Total Payable
               21, // PF Employer
               22, // ESIC Employer
               23  // CTC
             };

            foreach (var col in sumColumns)
            {
                string columnLetter = worksheet.Column(col).ColumnLetter();

                worksheet.Cell(totalRow, col).FormulaA1 =
                    $"=SUM({columnLetter}{startRow}:{columnLetter}{totalRow - 1})";
            }

            // Style Grand Total row
            var totalRange = worksheet.Range(totalRow, 1, totalRow, 24);

            totalRange.Style.Font.Bold = true;
            totalRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            totalRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            string fileName = $"Salary_Template_{month}_{year}.xlsx";


            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Payslip(int id)
        {
            var salary = await _salaryRecordRepository.GetByIdAsync(id);

            if (salary == null)
            {
                return NotFound();
            }

            bool isAdminOrHr = User.IsInRole("Admin") || User.IsInRole("HR");

            bool isEmployeeOnly =
                User.IsInRole("Employee") &&
                !isAdminOrHr;

            if (!isAdminOrHr && !isEmployeeOnly)
            {
                return Forbid();
            }

            if (isEmployeeOnly)
            {
                if (!salary.IsPayslipReleased)
                {
                    TempData["Error"] =
                        "Payslip has not been released yet.";

                    return RedirectToAction(
                        nameof(MySalary));
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                var employee = await _employeeRepository.Employees
                    .FirstOrDefaultAsync(x => x.ApplicationUserId == userId);

                if (employee == null || salary.EmployeeId != employee.Id)
                {
                    return Forbid();
                }

                if (!salary.EmployeeViewedPayslipOn.HasValue)
                {
                    salary.EmployeeViewedPayslipOn = DateTime.Now;
                    await _salaryRecordRepository.UpdateAsync(salary);
                    await _salaryRecordRepository.SaveAsync();
                }
            }

            return View(salary);
        }

        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> MyPayslip()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = _employeeRepository.Employees
                .FirstOrDefault(x => x.ApplicationUserId == user.Id);

            if (employee == null)
            {
                return NotFound();
            }

            var salary = _salaryRecordRepository.SalaryRecords
               .Where(x =>
                   x.EmployeeId == employee.Id &&
                   x.IsPayslipReleased)
               .OrderByDescending(x => x.Year)
               .ThenByDescending(x => x.Month)
               .FirstOrDefault();

            if (salary == null)
            {
                TempData["Error"] = "No released payslips found.";
                return RedirectToAction(nameof(MySalary));
            }

            return RedirectToAction(
                nameof(Payslip),
                new { id = salary.Id });
        }

        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> MySalary()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = _employeeRepository.Employees
                .FirstOrDefault(x => x.ApplicationUserId == user.Id);

            if (employee == null)
            {
                TempData["Error"] =
                    "Employee profile not found.";

                return RedirectToAction("MyDashboard", "Employee");
            }

            var salaries = _salaryRecordRepository.SalaryRecords
    .AsNoTracking()
    .Where(x =>
        x.EmployeeId == employee.Id &&
        x.IsPayslipReleased)
    .OrderByDescending(x => x.Year)
    .ThenByDescending(x => x.Month)
    .ToList();

            return View(salaries);
        }

        private async Task NotifyEmployeeSalaryAsync(
            SalaryRecordModel salary,
            string notificationType,
            string title,
            string message,
            string actionUrl)
        {
            try
            {
                string? recipientUserId =
                    salary.Employee?.ApplicationUserId;

                if (string.IsNullOrWhiteSpace(recipientUserId))
                {
                    return;
                }

                var recipient =
                    await _userManager.FindByIdAsync(recipientUserId);

                if (recipient == null || !recipient.IsActive)
                {
                    return;
                }

                bool exists =
                    await _notificationService.ExistsAsync(
                        recipient.Id,
                        notificationType,
                        "SalaryRecord",
                        salary.Id);

                if (exists)
                {
                    return;
                }

                await _notificationService.CreateAsync(
                    userId: recipient.Id,
                    title: title,
                    message: message,
                    notificationType: notificationType,
                    referenceType: "SalaryRecord",
                    referenceId: salary.Id,
                    actionUrl: actionUrl);
            }
            catch (Exception ex)
            {
                // Salary actions must remain successful even if notification creation fails.
                _logger.LogWarning(
                    ex,
                    "Salary notification failed. Type: {NotificationType}, SalaryRecordId: {SalaryRecordId}",
                    notificationType,
                    salary.Id);
            }
        }

        private static string GetMonthName(int month)
        {
            return new System.Globalization.CultureInfo("en-US")
                .DateTimeFormat.GetMonthName(month);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> ImportExcel(IFormFile file,
            int month, int year)
        {
            var result = await _salaryExcelImportService
                .ImportAsync(file, month, year);

            if (result.HasErrors)
            {
                TempData["Error"] = string.Join(Environment.NewLine, result.Errors);
            }

            TempData["Success"] = result.Message;

            await NotifySalaryImportUsersAsync(
                result.HasErrors
                    ? "Salary Import Failed"
                    : "Salary Import Completed",
                result.HasErrors
                    ? string.Join(Environment.NewLine, result.Errors.Take(3))
                    : result.Message,
                result.HasErrors
                    ? "SalaryImportFailed"
                    : "SalaryImportSucceeded",
                month,
                year);

            return RedirectToAction(
                nameof(Index),
                new { month, year });
        }

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> AttendanceSummary(int month, int year)
        {
            if (month == 0)
            {
                month = DateTime.Today.Month;
            }

            if (year == 0)
            {
                year = DateTime.Today.Year;
            }

            var summary = await _salaryAttendanceSummaryService
                .GetMonthlySummaryAsync(month, year);

            ViewBag.Month = month;
            ViewBag.Year = year;

            return View(summary);
        }

        private async Task NotifySalaryImportUsersAsync(
            string title,
            string message,
            string notificationType,
            int month,
            int year)
        {
            var recipients = new Dictionary<string, ApplicationUserModel>();

            foreach (var role in new[] { "Admin", "HR" })
            {
                foreach (var user in await _userManager.GetUsersInRoleAsync(role))
                {
                    recipients[user.Id] = user;
                }
            }

            foreach (var user in recipients.Values.Where(x => x.IsActive))
            {
                await _notificationService.CreateAsync(
                    user.Id,
                    title,
                    message,
                    notificationType,
                    "SalaryImport",
                    year * 100 + month,
                    $"/Salary?month={month}&year={year}");
            }
        }
        #endregion
    }
}
