using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.ViewModels;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR,Employee")]
    public class AttendanceController : Controller
    {
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly ILeaveApplicationRepository _leaveApplicationRepository;

        public AttendanceController(
        IAttendanceRepository attendanceRepository,
        IEmployeeRepository employeeRepository,
        ILeaveApplicationRepository leaveApplicationRepository,
        UserManager<ApplicationUserModel> userManager)
        {
            _attendanceRepository = attendanceRepository;
            _employeeRepository = employeeRepository;
            _leaveApplicationRepository = leaveApplicationRepository;
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

            bool approvedLeaveExists =
               await _leaveApplicationRepository.LeaveApplications
               .AnyAsync(l =>
               l.EmployeeId == model.EmployeeId &&
               l.Status == "Approved" &&
               l.FromDate.Date <= model.AttendanceDate.Date &&
               l.ToDate.Date >= model.AttendanceDate.Date);

            if (approvedLeaveExists && model.Status != "L")
            {
                ModelState.AddModelError(
                    "",
                    "Approved leave exists for this date. Only Leave attendance is allowed.");
            }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(string locationType)
        {
            if (string.IsNullOrWhiteSpace(locationType))
            {
                TempData["Error"] = "Please select Office or Site.";
                return RedirectToAction(nameof(Index));
            }

            if (locationType != "Office" && locationType != "Site")
            {
                TempData["Error"] = "Invalid attendance location.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);

            var employee = await _employeeRepository.Employees
                .FirstOrDefaultAsync(e => e.ApplicationUserId == user.Id);

            if (employee == null)
            {
                TempData["Error"] = "Employee mapping not found.";
                return RedirectToAction(nameof(Index));
            }

            var approvedLeaveToday =
                await _leaveApplicationRepository.HasApprovedLeaveTodayAsync(employee.Id);

            if (approvedLeaveToday)
            {
                TempData["Error"] = "You are on approved leave today. Attendance is not allowed.";
                return RedirectToAction(nameof(Index));
            }

            var todayAttendance = await _attendanceRepository.Attendances
              .FirstOrDefaultAsync(a =>
              a.EmployeeId == employee.Id &&
              a.AttendanceDate.Date == DateTime.Today);

            if (todayAttendance != null && todayAttendance.Status != "P")
            {
                TempData["Error"] =
                    $"Attendance is already marked as {todayAttendance.Status}. Check-in is not allowed.";

                return RedirectToAction(nameof(Index));
            }

            if (todayAttendance != null)
            {
                if (todayAttendance.Status == "L")
                {
                    TempData["Error"] = "You are on leave today. Attendance is not allowed.";
                    return RedirectToAction(nameof(Index));
                }

                if (todayAttendance.CheckInTime != null)
                {
                    TempData["Error"] = "Check In already completed today.";
                    return RedirectToAction(nameof(Index));
                }

                todayAttendance.Status = "P";
                todayAttendance.CheckInTime = DateTime.Now;
                todayAttendance.LocationType = locationType;

                await _attendanceRepository.SaveAsync();

                TempData["Success"] = $"Check In successful from {locationType}.";

                return RedirectToAction(nameof(Index));
            }

            var attendance = new AttendanceModel
            {
                EmployeeId = employee.Id,
                AttendanceDate = DateTime.Today,
                Status = "P",
                CheckInTime = DateTime.Now,
                LocationType = locationType,
                CreatedOn = DateTime.Now
            };

            await _attendanceRepository.AddAsync(attendance);
            await _attendanceRepository.SaveAsync();

            TempData["Success"] = $"Check In successful from {locationType}.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut()
        {
            var user = await _userManager.GetUserAsync(User);

            var employee = await _employeeRepository.Employees
                .FirstOrDefaultAsync(e => e.ApplicationUserId == user.Id);

            if (employee == null)
            {
                TempData["Error"] = "Employee mapping not found.";
                return RedirectToAction(nameof(Index));
            }

            var approvedLeaveToday =
                await _leaveApplicationRepository.HasApprovedLeaveTodayAsync(employee.Id);

            if (approvedLeaveToday)
            {
                TempData["Error"] = "You are on approved leave today. Check-out is not allowed.";
                return RedirectToAction(nameof(Index));
            }

            var attendance = await _attendanceRepository.Attendances
                .FirstOrDefaultAsync(a =>
                    a.EmployeeId == employee.Id &&
                    a.AttendanceDate.Date == DateTime.Today);

            if (attendance == null)
            {
                TempData["Error"] = "Check In first.";
                return RedirectToAction(nameof(Index));
            }

            if (attendance.Status == "L")
            {
                TempData["Error"] = "You are on leave today. Check-out is not allowed.";
                return RedirectToAction(nameof(Index));
            }

            if (attendance.CheckOutTime != null)
            {
                TempData["Error"] = "Already checked out.";
                return RedirectToAction(nameof(Index));
            }

            attendance.CheckOutTime = DateTime.Now;

            await _attendanceRepository.SaveAsync();

            TempData["Success"] = "Check Out successful.";

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
        public async Task<IActionResult> Edit(AttendanceModel model)
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
            var today = DateTime.Today;

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

            int presentToday = _attendanceRepository.Attendances
                 .Count(a =>
                 a.AttendanceDate.Date == today &&
                 a.Status == "P");

            int onLeaveToday = _attendanceRepository.Attendances
                .Count(a =>
                    a.AttendanceDate.Date == today &&
                    a.Status == "L");

            int markedToday = _attendanceRepository.Attendances
                .Count(a =>
                    a.AttendanceDate.Date == today);

            int notMarkedToday = employees.Count - markedToday;

            decimal attendancePercentage = employees.Count > 0
                ? Math.Round(((decimal)presentToday / employees.Count) * 100, 2)
                : 0;

            vm.PresentToday = presentToday;
            vm.OnLeaveToday = onLeaveToday;
            vm.NotMarkedToday = notMarkedToday;
            vm.AttendancePercentage = attendancePercentage;

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

            var attendanceList = await query
                .Include(a => a.Employee)
                    .ThenInclude(e => e.Department)
                .Include(a => a.Employee)
                    .ThenInclude(e => e.Designation)
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();

            var worksheet =
                workbook.Worksheets.Add("Attendance");

            worksheet.Cell(1, 1).Value = "Employee Code";
            worksheet.Cell(1, 2).Value = "Employee Name";
            worksheet.Cell(1, 3).Value = "Department";
            worksheet.Cell(1, 4).Value = "Designation";
            worksheet.Cell(1, 5).Value = "Date";
            worksheet.Cell(1, 6).Value = "Status";
            worksheet.Cell(1, 7).Value = "Remarks";
            worksheet.Cell(1, 8).Value = "Created On";

            var headerRange = worksheet.Range("A1:H1");

            headerRange.Style.Font.Bold = true;
            headerRange.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;


            int row = 2;

            foreach (var attendance in attendanceList)
            {
                worksheet.Cell(row, 1).Value =
                    attendance.Employee?.EmployeeCode;

                worksheet.Cell(row, 2).Value =
                    $"{attendance.Employee?.FirstName} {attendance.Employee?.LastName}";

                worksheet.Cell(row, 3).Value =
                    attendance.Employee?.Department?.DepartmentName;

                worksheet.Cell(row, 4).Value =
                    attendance.Employee?.Designation?.DesignationName;

                worksheet.Cell(row, 5).Value =
                    attendance.AttendanceDate;

                worksheet.Cell(row, 6).Value =
                    attendance.Status;

                worksheet.Cell(row, 7).Value =
                    attendance.Remarks;

                worksheet.Cell(row, 8).Value =
                    attendance.CreatedOn;

                row++;
            }

            worksheet.Columns().AdjustToContents();
            worksheet.Column(5).Style.DateFormat.Format = "dd-MMM-yyyy";

            worksheet.Column(8).Style.DateFormat.Format ="dd-MMM-yyyy HH:mm";



            using var stream =
                new MemoryStream();

            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Attendance_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Summary(int? month,int? year)
        {
            month ??= DateTime.Today.Month;
            year ??= DateTime.Today.Year;

            var attendanceRecords =
                await _attendanceRepository.Attendances
                .Include(a => a.Employee)
                .Where(a =>
                    a.AttendanceDate.Month == month &&
                    a.AttendanceDate.Year == year)
                .ToListAsync();

            var summary =
                attendanceRecords
                .GroupBy(a => a.EmployeeId)
                .Select(g =>
                {
                    int present =
                        g.Count(x => x.Status == "P");

                    int absent =
                        g.Count(x => x.Status == "A");

                    int leave =
                        g.Count(x => x.Status == "L");

                    int holiday =
                        g.Count(x => x.Status == "H");

                    int weekOff =
                        g.Count(x => x.Status == "WO");

                    int onDuty =
                        g.Count(x => x.Status == "OD");

                    int totalDays =
                        g.Count();

                    int workingDays =
                        totalDays - holiday - weekOff;

                    decimal percentage =
                        workingDays == 0
                        ? 0
                        : Math.Round(
                            ((decimal)(present + onDuty)
                            / workingDays) * 100,
                            2);

                    return new AttendanceSummaryViewModel
                    {
                        EmployeeId =
                            g.First().EmployeeId,

                        EmployeeCode =
                            g.First().Employee.EmployeeCode,

                        EmployeeName =
                            $"{g.First().Employee.FirstName} {g.First().Employee.LastName}",

                        PresentCount = present,

                        AbsentCount = absent,

                        LeaveCount = leave,

                        HolidayCount = holiday,

                        WeekOffCount = weekOff,

                        OnDutyCount = onDuty,

                        TotalDays = totalDays,

                        AttendancePercentage =
                            percentage
                    };
                })
                .OrderBy(x => x.EmployeeName)
                .ToList();

            ViewBag.Month = month;
            ViewBag.Year = year;

            return View(summary);
        }
    }
}