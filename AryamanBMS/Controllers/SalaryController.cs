using AryamanBMS.Models;
using AryamanBMS.Repositories;
using AryamanBMS.Repositories.Interfaces;
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

        public SalaryController(
           ISalaryRecordRepository salaryRecordRepository,
           IEmployeeRepository employeeRepository,UserManager<ApplicationUserModel> userManager)
        {
            _salaryRecordRepository =  salaryRecordRepository;

            _employeeRepository = employeeRepository;
             _userManager = userManager;
        }

        
        public IActionResult Index(int? month, int? year)
        {
            month ??= DateTime.Today.Month;
            year ??= DateTime.Today.Year;

            var salaries =
                _salaryRecordRepository.SalaryRecords
                .Where(x =>
                    x.Month == month &&
                    x.Year == year)
                .OrderBy(x => x.Employee!.EmployeeCode)
                .ToList();

            ViewBag.Month = month;
            ViewBag.Year = year;

            return View(salaries);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Generate(int month, int year)
        {
            var employees =
                _employeeRepository.Employees
                .Where(x => x.IsActive)
                .ToList();

            int generatedCount = 0;

            foreach (var employee in employees)
            {
                bool alreadyExists =
                    _salaryRecordRepository.SalaryRecords
                    .Any(x =>
                        x.EmployeeId == employee.Id &&
                        x.Month == month &&
                        x.Year == year);

                if (alreadyExists)
                {
                    continue;
                }

                decimal basicSalary = 25000m;
                decimal hra = 10000m;
                decimal da = 5000m;
                decimal otherAllowances = 2000m;

                decimal grossSalary =
                    basicSalary +
                    hra +
                    da +
                    otherAllowances;

                decimal pfDeduction =
                    Math.Round(basicSalary * 0.12m, 2);

                decimal esicDeduction = 0;

                if (grossSalary <= 21000)
                {
                    esicDeduction =
                        Math.Round(grossSalary * 0.0075m, 2);
                }

                decimal tdsDeduction = 0;
                decimal otherDeductions = 0;

                decimal netSalary =
                    grossSalary -
                    pfDeduction -
                    esicDeduction -
                    tdsDeduction -
                    otherDeductions;

                var salaryRecord =
                    new SalaryRecordModel
                    {
                        EmployeeId = employee.Id,

                        Month = month,
                        Year = year,

                        BasicSalary = basicSalary,
                        HRA = hra,
                        DA = da,
                        OtherAllowances = otherAllowances,

                        GrossSalary = grossSalary,

                        PfDeduction = pfDeduction,
                        EsicDeduction = esicDeduction,
                        TdsDeduction = tdsDeduction,
                        OtherDeductions = otherDeductions,

                        NetSalary = netSalary,

                        PaymentStatus = "Pending"
                    };

                await _salaryRecordRepository
                    .AddAsync(salaryRecord);

                generatedCount++;
            }

            await _salaryRecordRepository.SaveAsync();

            TempData["Success"] =
                $"{generatedCount} salary records generated successfully.";

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
        public async Task<IActionResult> MarkPaid(int id)
        {
            var salary =
                await _salaryRecordRepository
                .GetByIdAsync(id);

            if (salary == null)
            {
                return NotFound();
            }

            salary.PaymentStatus = "Paid";
            salary.PaidOn = DateTime.Now;

            await _salaryRecordRepository
                .UpdateAsync(salary);

            await _salaryRecordRepository
                .SaveAsync();

            TempData["Success"] =
                "Salary marked as paid.";

            return RedirectToAction(
                nameof(Index));
        }

        [Authorize(Roles = "Admin,HR")]
        public IActionResult Dashboard(int? month,int? year)
        {
            month ??= DateTime.Today.Month;
            year ??= DateTime.Today.Year;

            var salaries =
                _salaryRecordRepository.SalaryRecords
                .Where(x =>
                    x.Month == month &&
                    x.Year == year);

            var model =
                new SalaryDashboardViewModel
                {
                    TotalEmployees =
                        salaries.Count(),

                    PaidCount =
                        salaries.Count(x =>
                            x.PaymentStatus == "Paid"),

                    PendingCount =
                        salaries.Count(x =>
                            x.PaymentStatus != "Paid"),

                    TotalGrossSalary =
                        salaries.Sum(x => x.GrossSalary),

                    TotalNetSalary =
                        salaries.Sum(x => x.NetSalary)
                };

            ViewBag.Month = month;
            ViewBag.Year = year;

            return View(model);
        }

        [HttpGet]
        [Authorize]
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
        public async Task<IActionResult> ExportExcel(int? month, int? year)
        {
            month ??= DateTime.Today.Month;
            year ??= DateTime.Today.Year;

            var salaries = await _salaryRecordRepository.SalaryRecords
                .Include(x => x.Employee)
                .Where(x => x.Month == month && x.Year == year)
                .OrderBy(x => x.Employee!.EmployeeCode)
                .ToListAsync();

            using var workbook = new XLWorkbook();

            var worksheet = workbook.Worksheets.Add("Salary Register");

            worksheet.Cell("A1").Value = "Employee Code";
            worksheet.Cell("B1").Value = "Employee Name";
            worksheet.Cell("C1").Value = "Month";
            worksheet.Cell("D1").Value = "Year";
            worksheet.Cell("E1").Value = "Basic Salary";
            worksheet.Cell("F1").Value = "HRA";
            worksheet.Cell("G1").Value = "DA";
            worksheet.Cell("H1").Value = "Other Allowances";
            worksheet.Cell("I1").Value = "Gross Salary";
            worksheet.Cell("J1").Value = "PF";
            worksheet.Cell("K1").Value = "ESIC";
            worksheet.Cell("L1").Value = "TDS";
            worksheet.Cell("M1").Value = "Other Deductions";
            worksheet.Cell("N1").Value = "Total Deductions";
            worksheet.Cell("O1").Value = "Net Salary";
            worksheet.Cell("P1").Value = "Payment Status";
            worksheet.Cell("Q1").Value = "Paid On";

            int row = 2;

            foreach (var salary in salaries)
            {
                decimal totalDeductions =
                    salary.PfDeduction +
                    salary.EsicDeduction +
                    salary.TdsDeduction +
                    salary.OtherDeductions;

                worksheet.Cell(row, 1).Value = salary.Employee?.EmployeeCode;
                worksheet.Cell(row, 2).Value = salary.Employee?.FullName;
                worksheet.Cell(row, 3).Value = salary.Month;
                worksheet.Cell(row, 4).Value = salary.Year;
                worksheet.Cell(row, 5).Value = salary.BasicSalary;
                worksheet.Cell(row, 6).Value = salary.HRA;
                worksheet.Cell(row, 7).Value = salary.DA;
                worksheet.Cell(row, 8).Value = salary.OtherAllowances;
                worksheet.Cell(row, 9).Value = salary.GrossSalary;
                worksheet.Cell(row, 10).Value = salary.PfDeduction;
                worksheet.Cell(row, 11).Value = salary.EsicDeduction;
                worksheet.Cell(row, 12).Value = salary.TdsDeduction;
                worksheet.Cell(row, 13).Value = salary.OtherDeductions;
                worksheet.Cell(row, 14).Value = totalDeductions;
                worksheet.Cell(row, 15).Value = salary.NetSalary;
                worksheet.Cell(row, 16).Value = salary.PaymentStatus;
                worksheet.Cell(row, 17).Value = salary.PaidOn;

                row++;
            }

            var headerRange = worksheet.Range("A1:Q1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

            worksheet.Column(17).Style.DateFormat.Format = "dd-MMM-yyyy HH:mm";

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"SalaryRegister_{month}_{year}_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
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
    }
}