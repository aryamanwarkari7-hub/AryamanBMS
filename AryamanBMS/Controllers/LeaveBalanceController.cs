using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using ClosedXML.Excel;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class LeaveBalanceController : Controller
    {
        private readonly ILeaveBalanceRepository _leaveBalanceRepository;
        private readonly ILeaveTypeRepository _leaveTypeRepository;
        private readonly IEmployeeRepository _employeeRepository;

        public LeaveBalanceController(
            ILeaveBalanceRepository leaveBalanceRepository,
            ILeaveTypeRepository leaveTypeRepository,
            IEmployeeRepository employeeRepository)
        {
            _leaveBalanceRepository = leaveBalanceRepository;
            _leaveTypeRepository = leaveTypeRepository;
            _employeeRepository = employeeRepository;
        }

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Index(int? year, int? employeeId)
        {
            year ??= DateTime.Today.Year;

            var query = _leaveBalanceRepository.LeaveBalances
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.LeaveYear == year)
                .AsQueryable();

            if (employeeId.HasValue && employeeId.Value > 0)
            {
                query = query.Where(x => x.EmployeeId == employeeId.Value);
            }

            var leaveBalances = await query
                .OrderBy(x => x.Employee!.EmployeeCode)
                .ThenBy(x => x.LeaveType!.LeaveCode)
                .ToListAsync();

            ViewBag.Year = year;
            ViewBag.EmployeeId = employeeId ?? 0;

            ViewBag.Employees = await _employeeRepository.Employees
                .Where(x => x.IsActive)
                .OrderBy(x => x.EmployeeCode)
                .ToListAsync();

            return View(leaveBalances);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(int year)
        {
            var employees = await _employeeRepository.Employees
                .Where(x => x.IsActive)
                .ToListAsync();

            var leaveTypes = await _leaveTypeRepository.LeaveTypes
                .Where(x => x.IsActive && x.IsPaidLeave)
                .ToListAsync();

            foreach (var employee in employees)
            {
                foreach (var leaveType in leaveTypes)
                {
                    bool exists = await _leaveBalanceRepository.LeaveBalances
                        .AnyAsync(x =>
                            x.EmployeeId == employee.Id &&
                            x.LeaveTypeId == leaveType.Id &&
                            x.LeaveYear == year);

                    if (!exists)
                    {
                        var balance = new LeaveBalanceModel
                        {
                            EmployeeId = employee.Id,
                            LeaveTypeId = leaveType.Id,
                            LeaveYear = year,
                            AllocatedDays = leaveType.DaysPerYear,
                            UsedDays = 0,
                            BalanceDays = leaveType.DaysPerYear
                        };

                        await _leaveBalanceRepository.AddAsync(balance);
                    }
                }
            }

            await _leaveBalanceRepository.SaveAsync();

            TempData["Success"] =
                "Leave balances generated successfully.";

            return RedirectToAction(nameof(Index), new { year });
        }


        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Export(int? year)
        {
            year ??= DateTime.Today.Year;

            var balances = await _leaveBalanceRepository.LeaveBalances
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.LeaveYear == year.Value)
                .OrderBy(x => x.Employee.EmployeeCode)
                .ThenBy(x => x.LeaveType.LeaveCode)
                .ToListAsync();

            using var workbook = new XLWorkbook();

            var worksheet =
                workbook.Worksheets.Add("Leave Balance");

            worksheet.Cell("A1").Value = "Employee Code";
            worksheet.Cell("B1").Value = "Employee Name";
            worksheet.Cell("C1").Value = "Leave Code";
            worksheet.Cell("D1").Value = "Leave Type";
            worksheet.Cell("E1").Value = "Allocated";
            worksheet.Cell("F1").Value = "Used";
            worksheet.Cell("G1").Value = "Balance";
            worksheet.Cell("H1").Value = "Year";

            int row = 2;

            foreach (var item in balances)
            {
                worksheet.Cell(row, 1).Value = item.Employee?.EmployeeCode;
                worksheet.Cell(row, 2).Value = item.Employee?.FullName;
                worksheet.Cell(row, 3).Value = item.LeaveType?.LeaveCode;
                worksheet.Cell(row, 4).Value = item.LeaveType?.LeaveName;
                worksheet.Cell(row, 5).Value = item.AllocatedDays;
                worksheet.Cell(row, 6).Value = item.UsedDays;
                worksheet.Cell(row, 7).Value = item.BalanceDays;
                worksheet.Cell(row, 8).Value = item.LeaveYear;

                row++;
            }

            var headerRange = worksheet.Range("A1:H1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"LeaveBalance_{year}_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }

        private decimal CalculateProratedLeave(
    decimal annualLeaveDays,
    DateTime joiningDate,
    int financialYear)
        {
            var fyStart = new DateTime(financialYear, 4, 1);
            var fyEnd = new DateTime(financialYear + 1, 3, 31);

            if (joiningDate > fyEnd)
            {
                return 0;
            }

            var effectiveStartDate =
                joiningDate > fyStart
                    ? joiningDate
                    : fyStart;

            int eligibleMonths =
                ((fyEnd.Year - effectiveStartDate.Year) * 12)
                + fyEnd.Month
                - effectiveStartDate.Month
                + 1;

            if (eligibleMonths < 0)
            {
                eligibleMonths = 0;
            }

            decimal proratedLeave =
                annualLeaveDays / 12m * eligibleMonths;

            return Math.Round(proratedLeave, 2);
        }
    }
}