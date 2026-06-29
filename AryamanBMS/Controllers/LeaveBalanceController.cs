using AryamanBMS.Extensions;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public async Task<IActionResult> Index(
              int? year,
              int? employeeId,
              int page = 1)
        {
            const int pageSize = 10;

            int selectedYear =
                year ?? DateTime.Today.Year;

            var query = _leaveBalanceRepository.LeaveBalances
                .AsNoTracking()
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.LeaveYear == selectedYear)
                .AsQueryable();

            if (employeeId.HasValue &&
                employeeId.Value > 0)
            {
                query = query.Where(x =>
                    x.EmployeeId == employeeId.Value);
            }

            query = query
                .OrderBy(x => x.Employee!.EmployeeCode)
                .ThenBy(x => x.LeaveType!.LeaveCode);

            var routeValues =
                new Dictionary<string, string>
                {
                    ["year"] = selectedYear.ToString()
                };

            if (employeeId.HasValue &&
                employeeId.Value > 0)
            {
                routeValues["employeeId"] =
                    employeeId.Value.ToString();
            }

            var model = await query.ToPagedListAsync(
                page,
                pageSize,
                routeValues);

            model.Pagination.ControllerName =
                "LeaveBalance";

            model.Pagination.ActionName =
                nameof(Index);

            ViewBag.Year = selectedYear;

            ViewBag.EmployeeId =
                employeeId.GetValueOrDefault();

            ViewBag.Employees =
                await _employeeRepository.Employees
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.EmployeeCode)
                    .ToListAsync();

            return View(model);
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
                    if (string.Equals(
                     leaveType.LeaveCode,
                     "COMP",
                     StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (string.Equals(
                        leaveType.LeaveCode,
                        "BDL",
                        StringComparison.OrdinalIgnoreCase) &&
                    !employee.DateOfBirth.HasValue)
                    {
                        continue;
                    }

                    bool exists = await _leaveBalanceRepository.LeaveBalances
                        .AnyAsync(x =>
                            x.EmployeeId == employee.Id &&
                            x.LeaveTypeId == leaveType.Id &&
                            x.LeaveYear == year);



                    if (!exists)
                    {
                        decimal currentYearAllocation =
                            CalculateProratedLeave(
                                leaveType.DaysPerYear,
                                employee.JoiningDate,
                                year);

                        if (currentYearAllocation <= 0)
                        {
                            continue;
                        }

                        decimal carryForwardDays = 0;

                        if (leaveType.IsCarryForward)
                        {
                            var previousYearBalance =
                                await _leaveBalanceRepository.LeaveBalances
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(x =>
                                        x.EmployeeId == employee.Id &&
                                        x.LeaveTypeId == leaveType.Id &&
                                        x.LeaveYear == year - 1);

                            if (previousYearBalance != null &&
                                previousYearBalance.BalanceDays > 0)
                            {
                                decimal availablePreviousBalance =
                                 previousYearBalance.BalanceDays;

                                int maximumCarryForwardDays =
                                    leaveType.MaximumCarryForwardDays ?? 0;

                                carryForwardDays =
                                    maximumCarryForwardDays > 0
                                        ? Math.Min(
                                            availablePreviousBalance,
                                            maximumCarryForwardDays)
                                        : 0;
                            }
                        }

                        decimal totalAllocatedDays =
                            currentYearAllocation + carryForwardDays;

                        var balance = new LeaveBalanceModel
                        {
                            EmployeeId = employee.Id,
                            LeaveTypeId = leaveType.Id,
                            LeaveYear = year,

                            CurrentYearAllocation =
                            currentYearAllocation,

                            CarryForwardDays =
                            carryForwardDays,

                            AllocatedDays =
                            totalAllocatedDays,

                            UsedDays = 0,

                            BalanceDays =
                            totalAllocatedDays
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
        public async Task<IActionResult> Export(
        int? year,
        int? employeeId)
        {
            int selectedYear =
                year ?? DateTime.Today.Year;

            var query = _leaveBalanceRepository.LeaveBalances
                .AsNoTracking()
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Where(x => x.LeaveYear == selectedYear)
                .AsQueryable();

            if (employeeId.HasValue &&
                employeeId.Value > 0)
            {
                query = query.Where(x =>
                    x.EmployeeId == employeeId.Value);
            }

            var balances = await query
                .OrderBy(x => x.Employee!.EmployeeCode)
                .ThenBy(x => x.LeaveType!.LeaveCode)
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
                $"LeaveBalance_{selectedYear}_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }

        private decimal CalculateProratedLeave(
          decimal annualLeaveDays,
          DateTime joiningDate,
          int leaveYear)
        {
            var leaveYearStart =
                new DateTime(leaveYear, 1, 1);

            var leaveYearEnd =
                new DateTime(leaveYear, 12, 31);

            if (joiningDate.Date > leaveYearEnd)
            {
                return 0;
            }

            if (joiningDate.Date <= leaveYearStart)
            {
                return annualLeaveDays;
            }

            int eligibleMonths =
                12 - joiningDate.Month + 1;

            decimal proratedLeave =
                annualLeaveDays / 12m * eligibleMonths;

            return Math.Round(
             proratedLeave * 2,
             MidpointRounding.AwayFromZero) / 2;
        }
    }
}