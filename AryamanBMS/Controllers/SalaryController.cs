using AryamanBMS.Extensions;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interface;
using AryamanBMS.ViewModels;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize]
    public class SalaryController : Controller
    {
        private readonly ISalaryRecordRepository _salaryRecordRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly UserManager<ApplicationUserModel> _userManager;

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
              IWebHostEnvironment webHostEnvironment)
        {
            _salaryRecordRepository = salaryRecordRepository;
            _employeeRepository = employeeRepository;
            _userManager = userManager;
            _salaryExcelImportService = salaryExcelImportService;
            _salaryAttendanceSummaryService = salaryAttendanceSummaryService;
            _webHostEnvironment = webHostEnvironment;
        }

        [Authorize(Roles = "Admin,HR")]

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

            return View(salary);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(SalaryRecordModel salary)
        {
            if (ModelState.IsValid)
            {
                await _salaryRecordRepository
                    .UpdateAsync(salary);

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
        [Authorize(Roles = "Admin,HR")]
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

            salary.PaymentStatus = "Paid";
            salary.PaidOn = DateTime.Now;

            await _salaryRecordRepository.UpdateAsync(salary);
            await _salaryRecordRepository.SaveAsync();

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

            int selectedYear =
                year ?? DateTime.Today.Year;

            bool isYearly =
                string.Equals(
                    viewType,
                    "Yearly",
                    StringComparison.OrdinalIgnoreCase);

            viewType = isYearly
                ? "Yearly"
                : "Monthly";

            var yearSalaries =
                await _salaryRecordRepository.SalaryRecords
                    .AsNoTracking()
                    .Where(x => x.Year == selectedYear)
                    .ToListAsync();

            var selectedSalaries =
                isYearly
                    ? yearSalaries
                    : yearSalaries
                        .Where(x => x.Month == selectedMonth)
                        .ToList();

            int paidCount =
                selectedSalaries.Count(x =>
                    string.Equals(
                        x.PaymentStatus,
                        "Paid",
                        StringComparison.OrdinalIgnoreCase));

            int pendingCount =
                selectedSalaries.Count - paidCount;

            decimal totalGrossSalary =
                selectedSalaries.Sum(x => x.GrossSalary);

            decimal totalNetSalary =
                selectedSalaries.Sum(x => x.NetSalary);

            var model = new SalaryDashboardViewModel
            {
                ViewType = viewType,
                Month = selectedMonth,
                Year = selectedYear,

                TotalEmployees =
             selectedSalaries
                 .Select(x => x.EmployeeId)
                 .Distinct()
                 .Count(),

                PaidCount = paidCount,

                PendingCount = pendingCount,

                TotalGrossSalary =
             selectedSalaries.Sum(x => x.GrossSalary),

                TotalNetSalary =
             selectedSalaries.Sum(x => x.NetSalary),

                TotalDeductions =
             selectedSalaries.Sum(x => x.TotalDeductions),

                PayrollCompletionPercentage =
             selectedSalaries.Count > 0
                 ? Math.Round(
                     (decimal)paidCount /
                     selectedSalaries.Count * 100,
                     2)
                 : 0,

                TotalBasic =
             selectedSalaries.Sum(x => x.BasicSalary),

                TotalHRA =
             selectedSalaries.Sum(x => x.HRA),

                TotalDA =
             selectedSalaries.Sum(x => x.DA),

                TotalOtherAllowances =
             selectedSalaries.Sum(x =>
                 x.Conveyance +
                 x.MedicalAllowance +
                 x.EducationAllowance +
                 x.SpecialAllowance +
                 x.OtherAllowances),

                TotalPF =
             selectedSalaries.Sum(x => x.PfDeduction),

                TotalESIC =
             selectedSalaries.Sum(x => x.EsicDeduction),

                TotalTDS =
             selectedSalaries.Sum(x => x.TdsDeduction),

                TotalOtherDeductions =
             selectedSalaries.Sum(x =>
                 x.ProfessionalTax +
                 x.Advance +
                 x.OtherDeductions)
            };

            if (isYearly)
            {
                model.MonthlySummaries =
                    yearSalaries
                        .GroupBy(x => x.Month)
                        .OrderBy(x => x.Key)
                        .Select(group =>
                        {
                            int monthlyPaid =
                                group.Count(x =>
                                    string.Equals(
                                        x.PaymentStatus,
                                        "Paid",
                                        StringComparison.OrdinalIgnoreCase));

                            return new SalaryDashboardViewModel
                                .MonthlySalarySummaryViewModel
                            {
                                Month = group.Key,

                                MonthName =
                                    new DateTime(
                                        selectedYear,
                                        group.Key,
                                        1)
                                    .ToString("MMMM"),

                                EmployeeCount =
                                    group.Select(x => x.EmployeeId)
                                        .Distinct()
                                        .Count(),

                                PaidCount = monthlyPaid,

                                PendingCount =
                                    group.Count() - monthlyPaid,

                                GrossSalary =
                                    group.Sum(x => x.GrossSalary),

                                NetSalary =
                                    group.Sum(x => x.NetSalary)
                            };
                        })
                        .ToList();
            }

            ViewBag.ViewType = viewType;
            ViewBag.Month = selectedMonth;
            ViewBag.Year = selectedYear;

            return View(model);
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

                var latestSalary = previousSalaryRecords
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

                decimal actualSalary = latestSalary?.ActualSalary ?? 0;

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

            if (User.IsInRole("Employee") &&
                !User.IsInRole("Admin") &&
                !User.IsInRole("HR"))
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                var employee = await _employeeRepository.Employees
                    .FirstOrDefaultAsync(x => x.ApplicationUserId == userId);

                if (employee == null || salary.EmployeeId != employee.Id)
                {
                    return Forbid();
                }
            }

            return View(salary);
        }

        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> MyPayslip()
        {
            var user = await _userManager.GetUserAsync(User);

            var employee = _employeeRepository.Employees
                .FirstOrDefault(x => x.ApplicationUserId == user.Id);

            if (employee == null)
            {
                return NotFound();
            }

            var salary = _salaryRecordRepository.SalaryRecords
                .Where(x => x.EmployeeId == employee.Id)
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .FirstOrDefault();

            if (salary == null)
            {
                TempData["Error"] = "No salary records found.";
                return RedirectToAction("Index", "Dashboard");
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

                return RedirectToAction("Index", "Dashboard");
            }

            var salaries = _salaryRecordRepository.SalaryRecords
                .Where(x => x.EmployeeId == employee.Id)
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToList();

            return View(salaries);
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
                TempData["Error"] = string.Join("<br/>", result.Errors);
            }

            TempData["Success"] = result.Message;

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
    }
}