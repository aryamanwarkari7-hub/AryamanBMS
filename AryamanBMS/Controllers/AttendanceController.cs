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
    [Authorize(Roles = "Admin,HR,Employee")]
    public class AttendanceController : Controller
    {
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly ILeaveApplicationRepository _leaveApplicationRepository;
        private readonly IConfiguration _configuration;

        private readonly ISalaryAttendanceSummaryService _salaryAttendanceSummaryService;

        public AttendanceController(
    IAttendanceRepository attendanceRepository,
    IEmployeeRepository employeeRepository,
    ILeaveApplicationRepository leaveApplicationRepository,
    UserManager<ApplicationUserModel> userManager,
    ISalaryAttendanceSummaryService salaryAttendanceSummaryService,
    IConfiguration configuration)
        {
            _attendanceRepository = attendanceRepository;
            _employeeRepository = employeeRepository;
            _leaveApplicationRepository = leaveApplicationRepository;
            _userManager = userManager;
            _salaryAttendanceSummaryService = salaryAttendanceSummaryService;
            _configuration = configuration;
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
            ViewBag.TodayCalendarStatus =
                todayAttendance == null
                    ? GetCalendarStatus(DateTime.Today)
                    : null;

            return View(todayAttendance);
        }

        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> MyMonthly(int? month, int? year)
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
                TempData["Error"] =
                    "No employee record mapped to this user.";

                return RedirectToAction(nameof(Index));
            }

            int selectedMonth =
                month.HasValue &&
                month.Value >= 1 &&
                month.Value <= 12
                    ? month.Value
                    : DateTime.Today.Month;

            int selectedYear =
                year ?? DateTime.Today.Year;

            var attendanceRecords =
                await _attendanceRepository.Attendances
                    .Where(a =>
                        a.EmployeeId == employee.Id &&
                        a.AttendanceDate.Month == selectedMonth &&
                        a.AttendanceDate.Year == selectedYear)
                    .OrderBy(a => a.AttendanceDate)
                    .ToListAsync();

            ViewBag.Employee = employee;
            ViewBag.Month = selectedMonth;
            ViewBag.Year = selectedYear;

            return View(attendanceRecords);
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

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _employeeRepository.Employees
                .FirstOrDefaultAsync(e => e.ApplicationUserId == user.Id);

            if (employee == null)
            {
                TempData["Error"] = "Employee mapping not found.";
                return RedirectToAction(nameof(Index));
            }

            var calendarStatus = GetCalendarStatus(DateTime.Today);

            if (calendarStatus == "H")
            {
                TempData["Error"] = "Today is configured as an office holiday. Attendance is not required.";
                return RedirectToAction(nameof(Index));
            }

            if (calendarStatus == "WO")
            {
                TempData["Error"] = "Today is configured as a weekly off. Attendance is not required.";
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
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _employeeRepository.Employees
                .FirstOrDefaultAsync(e => e.ApplicationUserId == user.Id);

            if (employee == null)
            {
                TempData["Error"] = "Employee mapping not found.";
                return RedirectToAction(nameof(Index));
            }

            var calendarStatus = GetCalendarStatus(DateTime.Today);

            if (calendarStatus == "H")
            {
                TempData["Error"] = "Today is configured as an office holiday. Check-out is not required.";
                return RedirectToAction(nameof(Index));
            }

            if (calendarStatus == "WO")
            {
                TempData["Error"] = "Today is configured as a weekly off. Check-out is not required.";
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
            const int pageSize = 10;

            var query = _attendanceRepository.Attendances
                .AsNoTracking()
                .Include(a => a.Employee)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                searchText = searchText.Trim();

                query = query.Where(a =>
                    a.Employee != null &&
                    (
                        (a.Employee.EmployeeCode != null &&
                         a.Employee.EmployeeCode.Contains(searchText)) ||

                        (a.Employee.FirstName != null &&
                           a.Employee.FirstName.Contains(searchText)) ||

                        (a.Employee.LastName != null &&
                           a.Employee.LastName.Contains(searchText)) ||

                        (a.Employee.MobileNumber != null &&
                         a.Employee.MobileNumber.Contains(searchText)) ||

                        (a.Employee.OfficialEmail != null &&
                         a.Employee.OfficialEmail.Contains(searchText))
                    ));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(a =>
                    a.AttendanceDate.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(a =>
                    a.AttendanceDate.Date <= toDate.Value.Date);
            }

            query = query
                .OrderByDescending(a => a.AttendanceDate)
                .ThenByDescending(a => a.Id);

            var routeValues = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                routeValues["searchText"] = searchText;
            }

            if (fromDate.HasValue)
            {
                routeValues["fromDate"] =
                    fromDate.Value.ToString("yyyy-MM-dd");
            }

            if (toDate.HasValue)
            {
                routeValues["toDate"] =
                    toDate.Value.ToString("yyyy-MM-dd");
            }

            var model = await query.ToPagedListAsync(
                page,
                pageSize,
                routeValues);

            model.Pagination.ControllerName = "Attendance";
            model.Pagination.ActionName = nameof(Register);

            ViewBag.SearchText = searchText;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(model);
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
        public IActionResult Dashboard(int? day, int? month, int? year)
        {
            var today = DateTime.Today;

            int selectedMonth = month ?? today.Month;
            int selectedYear = year ?? today.Year;

            int totalDays = DateTime.DaysInMonth(selectedYear, selectedMonth);

            DateTime? selectedDate = null;

            if (day.HasValue &&
                day.Value >= 1 &&
                day.Value <= totalDays)
            {
                selectedDate = new DateTime(selectedYear, selectedMonth, day.Value);
            }

            var employees = _employeeRepository.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToList();

            var attendanceRecords = _attendanceRepository.Attendances
                .Include(a => a.Employee)
                .Where(a =>
                    a.AttendanceDate.Month == selectedMonth &&
                    a.AttendanceDate.Year == selectedYear)
                .ToList();

            DateTime summaryDate = selectedDate ?? today;

            var summaryRecords = attendanceRecords
                .Where(a => a.AttendanceDate.Date == summaryDate.Date)
                .ToList();

            var markedEmployeeIds = summaryRecords
                .Select(a => a.EmployeeId)
                .Distinct()
                .ToHashSet();

            var vm = new AttendanceDashboardViewModel
            {
                Month = selectedMonth,
                Year = selectedYear,
                TotalDays = totalDays,
                SummaryDate = summaryDate,
                TotalActiveEmployees = employees.Count
            };

            foreach (var employee in employees)
            {
                var employeeAttendance = new EmployeeAttendanceViewModel
                {
                    EmployeeId = employee.Id,
                    EmployeeCode = employee.EmployeeCode ?? string.Empty,
                    EmployeeName = employee.FullName
                };

                for (int calendarDay = 1; calendarDay <= totalDays; calendarDay++)
                {
                    var record = attendanceRecords.FirstOrDefault(a =>
                        a.EmployeeId == employee.Id &&
                        a.AttendanceDate.Day == calendarDay);

                    string status = record?.Status ?? "";

                    employeeAttendance.DailyStatus[calendarDay] = status;

                    switch (status)
                    {
                        case "P":
                            employeeAttendance.PresentCount++;
                            break;

                        case "A":
                            employeeAttendance.AbsentCount++;
                            break;

                        case "L":
                            employeeAttendance.LeaveCount++;
                            break;

                        case "H":
                            employeeAttendance.HolidayCount++;
                            break;

                        case "WO":
                            employeeAttendance.WeekOffCount++;
                            break;

                        case "OD":
                            employeeAttendance.OnDutyCount++;
                            break;
                    }
                }

                vm.Employees.Add(employeeAttendance);
            }

            vm.PresentToday = summaryRecords.Count(a => a.Status == "P");
            vm.AbsentToday = summaryRecords.Count(a => a.Status == "A");
            vm.OnLeaveToday = summaryRecords.Count(a => a.Status == "L");
            vm.OnDutyToday = summaryRecords.Count(a => a.Status == "OD");
            vm.NotMarkedToday = Math.Max(0, employees.Count - markedEmployeeIds.Count);

            vm.MissingCheckoutCount = summaryRecords.Count(a =>
                a.CheckInTime.HasValue &&
                !a.CheckOutTime.HasValue);

            vm.AttendancePercentage = employees.Count > 0
                ? Math.Round((decimal)vm.PresentToday / employees.Count * 100, 2)
                : 0;

            vm.StatusBuckets = BuildAttendanceBuckets(new List<AttendanceDashboardBucket>
    {
        new() { Label = "Present", Count = vm.PresentToday, CssClass = "bucket-success" },
        new() { Label = "Absent", Count = vm.AbsentToday, CssClass = "bucket-danger" },
        new() { Label = "Leave", Count = vm.OnLeaveToday, CssClass = "bucket-warning" },
        new() { Label = "On Duty", Count = vm.OnDutyToday, CssClass = "bucket-info" },
        new() { Label = "Not Marked", Count = vm.NotMarkedToday, CssClass = "bucket-neutral" }
    });

            vm.MonthlyStatusBuckets = BuildAttendanceBuckets(new List<AttendanceDashboardBucket>
    {
        new() { Label = "Present", Count = vm.Employees.Sum(x => x.PresentCount), CssClass = "bucket-success" },
        new() { Label = "Absent", Count = vm.Employees.Sum(x => x.AbsentCount), CssClass = "bucket-danger" },
        new() { Label = "Leave", Count = vm.Employees.Sum(x => x.LeaveCount), CssClass = "bucket-warning" },
        new() { Label = "Holiday", Count = vm.Employees.Sum(x => x.HolidayCount), CssClass = "bucket-info" },
        new() { Label = "Week Off", Count = vm.Employees.Sum(x => x.WeekOffCount), CssClass = "bucket-neutral" },
        new() { Label = "On Duty", Count = vm.Employees.Sum(x => x.OnDutyCount), CssClass = "bucket-info" }
    });

            for (int calendarDay = 1; calendarDay <= totalDays; calendarDay++)
            {
                var dayRecords = attendanceRecords
                    .Where(a => a.AttendanceDate.Day == calendarDay)
                    .ToList();

                int dayMarked = dayRecords
                    .Select(a => a.EmployeeId)
                    .Distinct()
                    .Count();

                int dayPresent = dayRecords.Count(a => a.Status == "P");

                vm.DayTrends.Add(new AttendanceDashboardDayTrend
                {
                    Day = calendarDay,
                    PresentCount = dayPresent,
                    LeaveCount = dayRecords.Count(a => a.Status == "L"),
                    AbsentCount = dayRecords.Count(a => a.Status == "A"),
                    NotMarkedCount = Math.Max(0, employees.Count - dayMarked),
                    PresentPercent = employees.Count > 0
                        ? Math.Round((decimal)dayPresent / employees.Count * 100, 2)
                        : 0
                });
            }

            vm.NotMarkedEmployees = employees
                .Where(e => !markedEmployeeIds.Contains(e.Id))
                .Take(8)
                .Select(e => ToAttendanceDashboardListItem(e, "Not marked", summaryDate.ToString("dd-MMM-yyyy")))
                .ToList();

            vm.OnLeaveEmployees = summaryRecords
                .Where(a => a.Status == "L")
                .OrderBy(a => a.Employee!.FirstName)
                .Take(8)
                .Select(a => ToAttendanceDashboardListItem(a.Employee!, "Leave", a.Remarks))
                .ToList();

            vm.MissingCheckoutEmployees = summaryRecords
                .Where(a => a.CheckInTime.HasValue && !a.CheckOutTime.HasValue)
                .OrderBy(a => a.Employee!.FirstName)
                .Take(8)
                .Select(a => ToAttendanceDashboardListItem(
                    a.Employee!,
                    "Missing checkout",
                    a.CheckInTime?.ToString("hh:mm tt")))
                .ToList();

            ViewBag.SelectedDay = day;
            ViewBag.SelectedMonth = selectedMonth;
            ViewBag.SelectedYear = selectedYear;
            ViewBag.SelectedDate = selectedDate;
            ViewBag.IsDailyView = selectedDate.HasValue;

            ViewBag.DailyAttendance = selectedDate.HasValue
                ? summaryRecords
                    .OrderBy(a => a.Employee!.FirstName)
                    .ThenBy(a => a.Employee!.LastName)
                    .ToList()
                : new List<AttendanceModel>();

            return View(vm);
        }

        private static List<AttendanceDashboardBucket> BuildAttendanceBuckets(
    List<AttendanceDashboardBucket> buckets)
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

        private static AttendanceDashboardListItem ToAttendanceDashboardListItem(
            EmployeeModel employee,
            string badge,
            string? meta)
        {
            return new AttendanceDashboardListItem
            {
                EmployeeId = employee.Id,
                EmployeeName = employee.FullName,
                EmployeeCode = employee.EmployeeCode ?? string.Empty,
                Badge = badge,
                Meta = meta
            };
        }

        private string? GetCalendarStatus(DateTime date)
        {
            if (IsOfficeHoliday(date))
            {
                return "H";
            }

            if (IsWeeklyOff(date))
            {
                return "WO";
            }

            return null;
        }

        private bool IsWeeklyOff(DateTime date)
        {
            var configuredDays =
                _configuration
                    .GetSection("Attendance:WeeklyOffDays")
                    .Get<string[]>();

            if (configuredDays == null || configuredDays.Length == 0)
            {
                return date.DayOfWeek == DayOfWeek.Sunday;
            }

            foreach (var configuredDay in configuredDays)
            {
                if (Enum.TryParse(
                        configuredDay,
                        ignoreCase: true,
                        out DayOfWeek dayOfWeek) &&
                    date.DayOfWeek == dayOfWeek)
                {
                    return true;
                }
            }

            return date.DayOfWeek == DayOfWeek.Sunday;
        }

        private bool IsOfficeHoliday(DateTime date)
        {
            var configuredHolidays =
                _configuration
                    .GetSection("Attendance:OfficeHolidays")
                    .Get<string[]>();

            if (configuredHolidays == null)
            {
                return false;
            }

            foreach (var configuredHoliday in configuredHolidays)
            {
                if (DateTime.TryParse(configuredHoliday, out var holiday) &&
                    holiday.Date == date.Date)
                {
                    return true;
                }
            }

            return false;
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
                searchText = searchText.Trim();

                query = query.Where(a =>
                    a.Employee != null &&
                    (
                        (a.Employee.EmployeeCode != null &&
                         a.Employee.EmployeeCode.Contains(searchText)) ||
                        (a.Employee.FirstName != null &&
                         a.Employee.FirstName.Contains(searchText)) ||
                        (a.Employee.LastName != null &&
                         a.Employee.LastName.Contains(searchText))
                    ));
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
                    .ThenInclude(e => e!.Department)
                .Include(a => a.Employee)
                    .ThenInclude(e => e!.Designation)
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

            worksheet.Column(8).Style.DateFormat.Format = "dd-MMM-yyyy HH:mm";



            using var stream =
                new MemoryStream();

            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Attendance_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Summary(int? month, int? year)
        {
            int selectedMonth =
                month.HasValue &&
                month.Value >= 1 &&
                month.Value <= 12
                    ? month.Value
                    : DateTime.Today.Month;

            int selectedYear =
                year ?? DateTime.Today.Year;

            var summary =
                await _salaryAttendanceSummaryService
                    .GetMonthlySummaryAsync(
                        selectedMonth,
                        selectedYear);

            ViewBag.Month = selectedMonth;
            ViewBag.Year = selectedYear;

            return View(summary);
        }
    }
}
