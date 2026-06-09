using AryamanBMS.Models;
using AryamanBMS.ViewModels;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using ClosedXML.Excel;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR,Employee")]
    public class AttendanceController : Controller
    {
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly UserManager<ApplicationUserModel> _userManager;

        public AttendanceController(
            IAttendanceRepository attendanceRepository,
            IEmployeeRepository employeeRepository,
            UserManager<ApplicationUserModel> userManager)
        {
            _attendanceRepository = attendanceRepository;
            _employeeRepository = employeeRepository;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _employeeRepository.Employees
                .FirstOrDefaultAsync(e =>
                    e.ApplicationUserId == user.Id);

            if (employee == null)
            {
                if (User.IsInRole("Admin") ||
                    User.IsInRole("HR"))
                {
                    return View();
                }

                TempData["Error"] =
                    "No employee record mapped to this user.";

                return View();
            }

            var todayAttendance =
                await _attendanceRepository.Attendances
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a =>
                    a.EmployeeId == employee.Id &&
                    a.AttendanceDate.Date == DateTime.Today);

            ViewBag.Employee = employee;
            ViewBag.TodayAttendance = todayAttendance;

            return View(todayAttendance);
        }

        [Authorize(Roles = "Admin,HR")]
        public IActionResult Create()
        {
            ViewBag.Employees = _employeeRepository.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.FirstName)
                .ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create(
            AttendanceModel model)
        {
            bool alreadyExists =
                await _attendanceRepository.Attendances
                .AnyAsync(a =>
                    a.EmployeeId == model.EmployeeId &&
                    a.AttendanceDate.Date ==
                    model.AttendanceDate.Date);

            if (alreadyExists)
            {
                ModelState.AddModelError(
                    "",
                    "Attendance already exists for selected employee and date.");
            }

            if (ModelState.IsValid)
            {
                model.CreatedOn = DateTime.Now;

                await _attendanceRepository.AddAsync(model);
                await _attendanceRepository.SaveAsync();

                TempData["Success"] =
                    "Attendance created successfully.";

                return RedirectToAction(nameof(Register));
            }

            ViewBag.Employees = _employeeRepository.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.FirstName)
                .ToList();

            return View(model);
        }

        public async Task<IActionResult> CheckIn()
        {
            var user = await _userManager.GetUserAsync(User);

            var employee = await _employeeRepository.Employees
                .FirstOrDefaultAsync(e =>
                    e.ApplicationUserId == user.Id);

            if (employee == null)
            {
                TempData["Error"] =
                    "Employee mapping not found.";

                return RedirectToAction(nameof(Index));
            }

            bool alreadyMarked =
                await _attendanceRepository.Attendances
                .AnyAsync(a =>
                    a.EmployeeId == employee.Id &&
                    a.AttendanceDate.Date == DateTime.Today);

            if (alreadyMarked)
            {
                TempData["Error"] =
                    "Attendance already marked today.";

                return RedirectToAction(nameof(Index));
            }

            var attendance = new AttendanceModel
            {
                EmployeeId = employee.Id,
                AttendanceDate = DateTime.Today,
                Status = "P",
                CheckInTime = DateTime.Now,
                CreatedOn = DateTime.Now
            };

            await _attendanceRepository.AddAsync(attendance);
            await _attendanceRepository.SaveAsync();

            TempData["Success"] =
                "Check In successful.";

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> CheckOut()
        {
            var user = await _userManager.GetUserAsync(User);

            var employee = await _employeeRepository.Employees
                .FirstOrDefaultAsync(e =>
                    e.ApplicationUserId == user.Id);

            if (employee == null)
            {
                TempData["Error"] =
                    "Employee mapping not found.";

                return RedirectToAction(nameof(Index));
            }

            var attendance =
                await _attendanceRepository.Attendances
                .FirstOrDefaultAsync(a =>
                    a.EmployeeId == employee.Id &&
                    a.AttendanceDate.Date == DateTime.Today);

            if (attendance == null)
            {
                TempData["Error"] =
                    "Check In first.";

                return RedirectToAction(nameof(Index));
            }

            if (attendance.CheckOutTime != null)
            {
                TempData["Error"] =
                    "Already checked out.";

                return RedirectToAction(nameof(Index));
            }

            attendance.CheckOutTime = DateTime.Now;

            await _attendanceRepository.SaveAsync();

            TempData["Success"] =
                "Check Out successful.";

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Register(
            string? searchText,
            DateTime? fromDate,
            DateTime? toDate,
            int page = 1)
        {
            int pageSize = 10;

            var query =
                _attendanceRepository.Attendances
                .Include(a => a.Employee)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(a =>
                    a.Employee.EmployeeCode.Contains(searchText) ||
                    a.Employee.FirstName.Contains(searchText) ||
                    a.Employee.LastName.Contains(searchText) ||
                    (a.Employee.MobileNumber != null &&
                     a.Employee.MobileNumber.Contains(searchText)) ||
                    (a.Employee.OfficialEmail != null &&
                     a.Employee.OfficialEmail.Contains(searchText)));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a =>
                    a.AttendanceDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(a =>
                    a.AttendanceDate <= toDate.Value.Date);
            }

            int totalRecords =
                await query.CountAsync();

            var attendance =
                await query
                .OrderByDescending(a => a.AttendanceDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.SearchText = searchText;
            ViewBag.FromDate =
                fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate =
                toDate?.ToString("yyyy-MM-dd");

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages =
                (int)Math.Ceiling(
                    (double)totalRecords / pageSize);

            return View(attendance);
        }

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(int id)
        {
            var attendance =
                await _attendanceRepository.GetByIdAsync(id);

            if (attendance == null)
            {
                return NotFound();
            }

            return View(attendance);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(
            AttendanceModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var attendance =
                await _attendanceRepository
                .GetByIdAsync(model.Id);

            if (attendance == null)
            {
                return NotFound();
            }

            attendance.AttendanceDate =
                model.AttendanceDate;

            attendance.Status =
                model.Status;

            attendance.Remarks =
                model.Remarks;

            bool duplicateExists =
              await _attendanceRepository.Attendances
              .AnyAsync(a =>
              a.EmployeeId == attendance.EmployeeId &&
              a.AttendanceDate.Date == model.AttendanceDate.Date &&
              a.Id != model.Id);

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    "",
                    "Attendance already exists for this employee and date.");

                return View(model);
            }

            await _attendanceRepository
                .UpdateAsync(attendance);

            await _attendanceRepository
                .SaveAsync();

            TempData["Success"] =
                "Attendance updated successfully.";

            return RedirectToAction(nameof(Register));
        }

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Details(int id)
        {
            var attendance =
                await _attendanceRepository.Attendances
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a =>
                    a.Id == id);

            if (attendance == null)
            {
                return NotFound();
            }

            return View(attendance);
        }

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Delete(int id)
        {
            var attendance =
                await _attendanceRepository.Attendances
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a =>
                    a.Id == id);

            if (attendance == null)
            {
                return NotFound();
            }

            return View(attendance);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]

        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var attendance =
                await _attendanceRepository
                .GetByIdAsync(id);

            if (attendance != null)
            {
                await _attendanceRepository
                    .DeleteAsync(attendance);

                await _attendanceRepository
                    .SaveAsync();
            }

            TempData["Success"] =
                "Attendance deleted successfully.";

            return RedirectToAction(nameof(Register));
        }

        [Authorize(Roles = "Admin,HR")]
        public IActionResult Dashboard(int? month,int? year)
        {
            month ??= DateTime.Today.Month;
            year ??= DateTime.Today.Year;

            int totalDays =
                DateTime.DaysInMonth(
                    year.Value,
                    month.Value);

            var employees =
                _employeeRepository.Employees
                .Where(e => e.IsActive)
                .ToList();

            var attendanceRecords =
                _attendanceRepository.Attendances
                .Where(a =>
                    a.AttendanceDate.Month == month &&
                    a.AttendanceDate.Year == year)
                .ToList();

            var vm = new AttendanceDashboardViewModel
                {
                    Month = month.Value,
                    Year = year.Value,
                    TotalDays = totalDays
                };

            foreach (var employee in employees)
            {
                var employeeAttendanceViewModel =
                  new EmployeeAttendanceViewModel
                  {
                      EmployeeId = employee.Id,
                      EmployeeCode = employee.EmployeeCode,
                      EmployeeName =
                          $"{employee.FirstName} {employee.LastName}"
                  };

                for (int day = 1; day <= totalDays; day++)
                {
                    var record = attendanceRecords.FirstOrDefault(a =>
                            a.EmployeeId == employee.Id &&
                            a.AttendanceDate.Day == day);

                    string status =
                        record?.Status ?? "";

                    employeeAttendanceViewModel.DailyStatus[day] =
                        status;

                    switch (status)
                    {
                        case "P":
                            employeeAttendanceViewModel.PresentCount++;
                            break;

                        case "A":
                            employeeAttendanceViewModel.AbsentCount++;
                            break;

                        case "L":
                            employeeAttendanceViewModel.LeaveCount++;
                            break;

                        case "H":
                            employeeAttendanceViewModel.HolidayCount++;
                            break;

                        case "WO":
                            employeeAttendanceViewModel.WeekOffCount++;
                            break;

                        case "OD":
                            employeeAttendanceViewModel.OnDutyCount++;
                            break;
                    }
                }

                vm.Employees.Add(employeeAttendanceViewModel);
            }

            return View(vm);
        }

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> ExportExcel(
    string? searchText,
    DateTime? fromDate,
    DateTime? toDate)
        {
            var query = _attendanceRepository.Attendances
                .Include(a => a.Employee)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(a =>
                    a.Employee.EmployeeCode.Contains(searchText) ||
                    a.Employee.FirstName.Contains(searchText) ||
                    a.Employee.LastName.Contains(searchText));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a =>
                    a.AttendanceDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(a =>
                    a.AttendanceDate <= toDate.Value.Date);
            }

            var records = await query
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();

            var worksheet =
                workbook.Worksheets.Add("Attendance");

            worksheet.Cell(1, 1).Value = "Employee Code";
            worksheet.Cell(1, 2).Value = "Employee Name";
            worksheet.Cell(1, 3).Value = "Date";
            worksheet.Cell(1, 4).Value = "Status";
            worksheet.Cell(1, 5).Value = "Check In";
            worksheet.Cell(1, 6).Value = "Check Out";
            worksheet.Cell(1, 7).Value = "Remarks";

            int row = 2;

            foreach (var item in records)
            {
                worksheet.Cell(row, 1).Value =
                    item.Employee?.EmployeeCode;

                worksheet.Cell(row, 2).Value =
                    $"{item.Employee?.FirstName} {item.Employee?.LastName}";

                worksheet.Cell(row, 3).Value =
                    item.AttendanceDate;

                worksheet.Cell(row, 4).Value =
                    item.Status;

                worksheet.Cell(row, 5).Value =
                    item.CheckInTime?.ToString("hh:mm tt");

                worksheet.Cell(row, 6).Value =
                    item.CheckOutTime?.ToString("hh:mm tt");

                worksheet.Cell(row, 7).Value =
                    item.Remarks;

                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream =
                new MemoryStream();

            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Attendance_{DateTime.Now:yyyyMMdd}.xlsx");
        }
    }
}