using AryamanBMS.Extensions;
using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interface;
using AryamanBMS.Services.Interfaces;
using AryamanBMS.ViewModels;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR,Employee,Master")]
    public class AttendanceController : Controller
    {
        #region Actions

        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly ILeaveApplicationRepository _leaveApplicationRepository;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly INotificationService _notificationService;
        private readonly IWorkingDayService _workingDayService;

        private readonly ISalaryAttendanceSummaryService _salaryAttendanceSummaryService;

        public AttendanceController(
    IAttendanceRepository attendanceRepository,
    IEmployeeRepository employeeRepository,
    ILeaveApplicationRepository leaveApplicationRepository,
    UserManager<ApplicationUserModel> userManager,
    ISalaryAttendanceSummaryService salaryAttendanceSummaryService,
    ApplicationDbContext context,
    IConfiguration configuration,
    INotificationService notificationService,
    IWorkingDayService workingDayService)
        {
            _attendanceRepository = attendanceRepository;
            _employeeRepository = employeeRepository;
            _leaveApplicationRepository = leaveApplicationRepository;
            _userManager = userManager;
            _salaryAttendanceSummaryService = salaryAttendanceSummaryService;
            _context = context;
            _configuration = configuration;
            _notificationService = notificationService;
            _workingDayService = workingDayService;
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
                    ? await GetCalendarStatusAsync(DateTime.Today)
                    : null;

            return View(todayAttendance);
        }

        [Authorize(Roles = "Employee,Admin,HR,Master")]
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

            int totalDays = DateTime.DaysInMonth(selectedYear, selectedMonth);
            var monthStart = new DateTime(selectedYear, selectedMonth, 1);
            var monthEnd = new DateTime(selectedYear, selectedMonth, totalDays);
            var today = DateTime.Today;

            DateTime eligibleStart =
                employee.JoiningDate.Date > monthStart
                    ? employee.JoiningDate.Date
                    : monthStart;

            DateTime eligibleEnd =
                employee.LastWorkingDate.HasValue &&
                employee.LastWorkingDate.Value.Date < monthEnd
                    ? employee.LastWorkingDate.Value.Date
                    : monthEnd;

            if (selectedYear == today.Year &&
                selectedMonth == today.Month &&
                today < eligibleEnd)
            {
                eligibleEnd = today;
            }

            var markedDates = attendanceRecords
                .Select(a => a.AttendanceDate.Date)
                .ToHashSet();

            var visibleAttendanceRecords =
                attendanceRecords.ToList();

            int expectedAttendanceDays = 0;
            int missingDays = 0;

            if (eligibleStart <= eligibleEnd)
            {
                for (var date = eligibleStart.Date;
                     date <= eligibleEnd.Date;
                     date = date.AddDays(1))
                {
                    if (await _workingDayService.IsWorkingDayAsync(date))
                    {
                        expectedAttendanceDays++;

                        if (!markedDates.Contains(date))
                        {
                            missingDays++;
                            visibleAttendanceRecords.Add(new AttendanceModel
                            {
                                EmployeeId = employee.Id,
                                AttendanceDate = date,
                                Status = "M",
                                AttendanceValue = 0m,
                                Remarks = "Missing attendance"
                            });
                        }
                    }
                    else if (!markedDates.Contains(date))
                    {
                        var dayStatus =
                            await _workingDayService.GetDayStatusAsync(date);

                        visibleAttendanceRecords.Add(new AttendanceModel
                        {
                            EmployeeId = employee.Id,
                            AttendanceDate = date,
                            Status = dayStatus == "Holiday" ? "H" : "WO",
                            AttendanceValue = 0m,
                            Remarks = dayStatus == "Holiday"
                                ? "Holiday"
                                : "Weekly off"
                        });
                    }
                }
            }

            ViewBag.Employee = employee;
            ViewBag.Month = selectedMonth;
            ViewBag.Year = selectedYear;
            ViewBag.ExpectedAttendanceDays = expectedAttendanceDays;
            ViewBag.MissingDays = missingDays;

            DateTime fyStart =
                DateTime.Today.Month >= 4
                    ? new DateTime(DateTime.Today.Year, 4, 1)
                    : new DateTime(DateTime.Today.Year - 1, 4, 1);

            var paidLeaveSnapshot =
                await _context.LeaveApplications
                    .AsNoTracking()
                    .Include(x => x.LeaveType)
                    .Where(x =>
                        x.EmployeeId == employee.Id &&
                        x.FromDate.Date >= fyStart.Date &&
                        x.FromDate.Date <= fyStart.AddYears(1).AddDays(-1).Date &&
                        (
                            x.LeaveType == null ||
                            x.LeaveType.LeaveCode == null ||
                            (
                                x.LeaveType.LeaveCode != "COMP" &&
                                x.LeaveType.LeaveCode != "BDL"
                            )
                        ))
                    .ToListAsync();

            decimal paidUsed =
                paidLeaveSnapshot
                    .Where(x => x.Status == "Approved")
                    .Sum(x => x.PaidDays);

            int monthsLate = 0;

            if (employee.JoiningDate.Date > fyStart.Date)
            {
                monthsLate =
                    ((employee.JoiningDate.Year - fyStart.Year) * 12) +
                    employee.JoiningDate.Month -
                    fyStart.Month;

                if (employee.JoiningDate.Day > 1)
                {
                    monthsLate++;
                }

                monthsLate = Math.Clamp(monthsLate, 0, 12);
            }

            decimal entitlement =
                Math.Max(0, 18m - (monthsLate * 1.5m));

            decimal balanceBeforePending =
                Math.Max(0, entitlement - paidUsed);

            decimal pendingReserved =
                Math.Min(
                    paidLeaveSnapshot
                        .Where(x => x.Status == "Pending")
                        .Sum(x => x.NumberOfDays),
                    balanceBeforePending);

            ViewBag.PaidLeaveBalance =
                Math.Max(0, balanceBeforePending - pendingReserved);

            return View(
                visibleAttendanceRecords
                    .OrderBy(x => x.AttendanceDate)
                    .ThenBy(x => x.Id)
                    .ToList());
        }

        [Authorize(Roles = "Admin,HR,Master")]
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
        [Authorize(Roles = "Admin,HR,Master")]
        public async Task<IActionResult> Create(
            AttendanceModel model)
        {
            model.AttendanceValue =
                NormalizeAttendanceValue(model.AttendanceValue);

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

                await NotifyAttendanceChangedAsync(
                    model.EmployeeId,
                    model.AttendanceDate,
                    model.Status,
                    model.AttendanceValue,
                    "Attendance Marked",
                    "ManualAttendanceCreated");

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

            var calendarStatus = await GetCalendarStatusAsync(DateTime.Today);

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
                todayAttendance.AttendanceValue = 1m;
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
                AttendanceValue = 1m,
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

            var calendarStatus = await GetCalendarStatusAsync(DateTime.Today);

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


        [Authorize(Roles = "Admin,HR,Master")]
        public async Task<IActionResult> Register(
    string? searchText,
    DateTime? fromDate,
    DateTime? toDate,
    string sortBy = "AttendanceDate",
    string sortOrder = "desc",
    int page = 1)
        {
            const int pageSize = 10;
            var today = DateTime.Today;

            sortBy = sortBy switch
            {
                "Employee" => "Employee",
                "Status" => "Status",
                "AttendanceValue" => "AttendanceValue",
                _ => "AttendanceDate"
            };
            sortOrder = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase)
                ? "asc"
                : "desc";

            var effectiveFromDate =
                fromDate?.Date ??
                new DateTime(today.Year, today.Month, 1);

            var effectiveToDate =
                toDate?.Date ?? today.Date;

            if (effectiveToDate < effectiveFromDate)
            {
                effectiveToDate = effectiveFromDate;
            }

            var employeeQuery = _employeeRepository.Employees
                .AsNoTracking()
                .Where(e => e.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                searchText = searchText.Trim();

                employeeQuery = employeeQuery.Where(e =>
                    (
                        (e.EmployeeCode != null &&
                         e.EmployeeCode.Contains(searchText)) ||

                        (e.FirstName != null &&
                           e.FirstName.Contains(searchText)) ||

                        (e.LastName != null &&
                           e.LastName.Contains(searchText)) ||

                        (e.MobileNumber != null &&
                         e.MobileNumber.Contains(searchText)) ||

                        (e.OfficialEmail != null &&
                         e.OfficialEmail.Contains(searchText))
                    ));
            }

            var employees = await employeeQuery
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToListAsync();

            var employeeIds = employees
                .Select(e => e.Id)
                .ToHashSet();

            var attendanceRecords = await _attendanceRepository.Attendances
                .AsNoTracking()
                .Include(a => a.Employee)
                .Where(a =>
                    employeeIds.Contains(a.EmployeeId) &&
                    a.AttendanceDate.Date >= effectiveFromDate.Date &&
                    a.AttendanceDate.Date <= effectiveToDate.Date)
                .ToListAsync();

            var displayRecords = await BuildDisplayAttendanceRecordsAsync(
                employees,
                attendanceRecords,
                effectiveFromDate,
                effectiveToDate,
                today);

            var sortedRecords = sortBy switch
            {
                "Employee" => sortOrder == "asc"
                    ? displayRecords
                        .OrderBy(a => a.Employee?.FirstName)
                        .ThenBy(a => a.Employee?.LastName)
                        .ThenBy(a => a.AttendanceDate)
                        .ThenBy(a => a.Id)
                    : displayRecords
                        .OrderByDescending(a => a.Employee?.FirstName)
                        .ThenByDescending(a => a.Employee?.LastName)
                        .ThenByDescending(a => a.AttendanceDate)
                        .ThenByDescending(a => a.Id),

                "Status" => sortOrder == "asc"
                    ? displayRecords
                        .OrderBy(a => a.Status)
                        .ThenByDescending(a => a.AttendanceDate)
                        .ThenByDescending(a => a.Id)
                    : displayRecords
                        .OrderByDescending(a => a.Status)
                        .ThenByDescending(a => a.AttendanceDate)
                        .ThenByDescending(a => a.Id),

                "AttendanceValue" => sortOrder == "asc"
                    ? displayRecords
                        .OrderBy(a => a.AttendanceValue)
                        .ThenByDescending(a => a.AttendanceDate)
                        .ThenByDescending(a => a.Id)
                    : displayRecords
                        .OrderByDescending(a => a.AttendanceValue)
                        .ThenByDescending(a => a.AttendanceDate)
                        .ThenByDescending(a => a.Id),

                _ => sortOrder == "asc"
                    ? displayRecords
                        .OrderBy(a => a.AttendanceDate)
                        .ThenBy(a => a.Employee?.FirstName)
                        .ThenBy(a => a.Employee?.LastName)
                        .ThenBy(a => a.Id)
                    : displayRecords
                        .OrderByDescending(a => a.AttendanceDate)
                        .ThenBy(a => a.Employee?.FirstName)
                        .ThenBy(a => a.Employee?.LastName)
                        .ThenByDescending(a => a.Id)
            };

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

            routeValues["sortBy"] = sortBy;
            routeValues["sortOrder"] = sortOrder;

            page = page < 1 ? 1 : page;

            int totalRecords = displayRecords.Count;
            int totalPages = pageSize > 0
                ? (int)Math.Ceiling(totalRecords / (double)pageSize)
                : 0;

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var model = new PagedListViewModel<AttendanceModel>
            {
                Items = sortedRecords
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList(),
                Pagination = new PaginationViewModel
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    RouteValues = routeValues
                }
            };

            model.Pagination.ControllerName = "Attendance";
            model.Pagination.ActionName = nameof(Register);

            ViewBag.SearchText = searchText;
            ViewBag.FromDate = effectiveFromDate.ToString("yyyy-MM-dd");
            ViewBag.ToDate = effectiveToDate.ToString("yyyy-MM-dd");
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

            return View(model);
        }

        [Authorize(Roles = "Admin,HR,Master")]
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
        [Authorize(Roles = "Admin,HR,Master")]
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

            attendance.AttendanceValue =
                NormalizeAttendanceValue(model.AttendanceValue);

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

            await NotifyAttendanceChangedAsync(
                attendance.EmployeeId,
                attendance.AttendanceDate,
                attendance.Status,
                attendance.AttendanceValue,
                "Attendance Updated",
                "ManualAttendanceUpdated");

            TempData["Success"] =
                "Attendance updated successfully.";

            return RedirectToAction(nameof(Register));
        }

        [Authorize(Roles = "Admin,HR,Master")]
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

        [Authorize(Roles = "Admin,HR,Master")]
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
        [Authorize(Roles = "Admin,HR,Master")]

        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var attendance =
                await _attendanceRepository
                .GetByIdAsync(id);

            if (attendance != null)
            {
                int employeeId = attendance.EmployeeId;
                DateTime attendanceDate = attendance.AttendanceDate;

                await _attendanceRepository
                    .DeleteAsync(attendance);

                await _attendanceRepository
                    .SaveAsync();

                await NotifyAttendanceDeletedAsync(
                    employeeId,
                    attendanceDate);
            }

            TempData["Success"] =
                "Attendance deleted successfully.";

            return RedirectToAction(nameof(Register));
        }

        [Authorize(Roles = "Admin,HR,Master")]
        public async Task<IActionResult> Dashboard(int? day, int? month, int? year)
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

            var attendanceByEmployeeDate = attendanceRecords
                .GroupBy(a => (a.EmployeeId, Date: a.AttendanceDate.Date))
                .ToDictionary(
                    g => g.Key,
                    g => g
                        .OrderByDescending(a => a.CheckInTime.HasValue)
                        .ThenByDescending(a => a.Id)
                        .First());

            var displayAttendanceRecords = new List<AttendanceModel>();

            foreach (var employee in employees)
            {
                for (int calendarDay = 1; calendarDay <= totalDays; calendarDay++)
                {
                    var date = new DateTime(selectedYear, selectedMonth, calendarDay);

                    if (attendanceByEmployeeDate.TryGetValue((employee.Id, date.Date), out var record))
                    {
                        displayAttendanceRecords.Add(record);
                        continue;
                    }

                    if (!IsEligibleDashboardAttendanceDate(employee, date, today))
                    {
                        continue;
                    }

                    var dayStatus = await _workingDayService.GetDayStatusAsync(date);
                    string? status = dayStatus switch
                    {
                        "Holiday" => "H",
                        "WeeklyOff" => "WO",
                        "Working" => "M",
                        _ => null
                    };

                    if (status == null)
                    {
                        continue;
                    }

                    displayAttendanceRecords.Add(new AttendanceModel
                    {
                        EmployeeId = employee.Id,
                        Employee = employee,
                        AttendanceDate = date,
                        Status = status,
                        AttendanceValue = 0,
                        Remarks = status switch
                        {
                            "H" => "Holiday",
                            "WO" => "Weekly off",
                            _ => "Missing attendance"
                        }
                    });
                }
            }

            var monthStart = new DateTime(selectedYear, selectedMonth, 1);
            var monthEnd = new DateTime(selectedYear, selectedMonth, totalDays);
            var defaultSummaryDate = monthStart > today
                ? monthStart
                : monthEnd > today
                    ? today
                    : monthEnd;

            DateTime summaryDate = selectedDate ?? defaultSummaryDate;

            var summaryRecords = displayAttendanceRecords
                .Where(a => a.AttendanceDate.Date == summaryDate.Date)
                .ToList();

            var missingEmployeeIds = summaryRecords
                .Where(a => a.Status == "M")
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
                    var record = displayAttendanceRecords.FirstOrDefault(a =>
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

                        case "M":
                            employeeAttendance.MissingCount++;
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
            vm.NotMarkedToday = missingEmployeeIds.Count;

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
        new() { Label = "Missing", Count = vm.NotMarkedToday, CssClass = "bucket-danger" }
    });

            vm.MonthlyStatusBuckets = BuildAttendanceBuckets(new List<AttendanceDashboardBucket>
    {
        new() { Label = "Present", Count = vm.Employees.Sum(x => x.PresentCount), CssClass = "bucket-success" },
        new() { Label = "Absent", Count = vm.Employees.Sum(x => x.AbsentCount), CssClass = "bucket-danger" },
        new() { Label = "Missing", Count = vm.Employees.Sum(x => x.MissingCount), CssClass = "bucket-danger" },
        new() { Label = "Leave", Count = vm.Employees.Sum(x => x.LeaveCount), CssClass = "bucket-warning" },
        new() { Label = "Holiday", Count = vm.Employees.Sum(x => x.HolidayCount), CssClass = "bucket-info" },
        new() { Label = "Week Off", Count = vm.Employees.Sum(x => x.WeekOffCount), CssClass = "bucket-neutral" },
        new() { Label = "On Duty", Count = vm.Employees.Sum(x => x.OnDutyCount), CssClass = "bucket-info" }
    });

            for (int calendarDay = 1; calendarDay <= totalDays; calendarDay++)
            {
                var dayRecords = displayAttendanceRecords
                    .Where(a => a.AttendanceDate.Day == calendarDay)
                    .ToList();

                int dayPresent = dayRecords.Count(a => a.Status == "P");

                vm.DayTrends.Add(new AttendanceDashboardDayTrend
                {
                    Day = calendarDay,
                    PresentCount = dayPresent,
                    LeaveCount = dayRecords.Count(a => a.Status == "L"),
                    AbsentCount = dayRecords.Count(a => a.Status == "A"),
                    NotMarkedCount = dayRecords.Count(a => a.Status == "M"),
                    PresentPercent = employees.Count > 0
                        ? Math.Round((decimal)dayPresent / employees.Count * 100, 2)
                        : 0
                });
            }

            vm.NotMarkedEmployees = summaryRecords
                .Where(a => a.Status == "M" && a.Employee != null)
                .OrderBy(a => a.Employee!.FirstName)
                .Take(8)
                .Select(a => ToAttendanceDashboardListItem(
                    a.Employee!,
                    "Missing",
                    summaryDate.ToString("dd-MMM-yyyy")))
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

        private static bool IsEligibleDashboardAttendanceDate(
            EmployeeModel employee,
            DateTime date,
            DateTime today)
        {
            if (date.Date > today.Date)
            {
                return false;
            }

            if (employee.JoiningDate.Date > date.Date)
            {
                return false;
            }

            if (employee.LastWorkingDate.HasValue &&
                employee.LastWorkingDate.Value.Date < date.Date)
            {
                return false;
            }

            return true;
        }

        private async Task<List<AttendanceModel>> BuildDisplayAttendanceRecordsAsync(
            IReadOnlyCollection<EmployeeModel> employees,
            IReadOnlyCollection<AttendanceModel> attendanceRecords,
            DateTime fromDate,
            DateTime toDate,
            DateTime today)
        {
            var attendanceByEmployeeDate = attendanceRecords
                .GroupBy(a => (a.EmployeeId, Date: a.AttendanceDate.Date))
                .ToDictionary(
                    g => g.Key,
                    g => g
                        .OrderByDescending(a => a.CheckInTime.HasValue)
                        .ThenByDescending(a => a.Id)
                        .First());

            var displayRecords = new List<AttendanceModel>();

            foreach (var employee in employees)
            {
                var employeeStart =
                    employee.JoiningDate.Date > fromDate.Date
                        ? employee.JoiningDate.Date
                        : fromDate.Date;

                var employeeEnd =
                    employee.LastWorkingDate.HasValue &&
                    employee.LastWorkingDate.Value.Date < toDate.Date
                        ? employee.LastWorkingDate.Value.Date
                        : toDate.Date;

                for (var date = employeeStart;
                     date <= employeeEnd;
                     date = date.AddDays(1))
                {
                    if (attendanceByEmployeeDate.TryGetValue((employee.Id, date.Date), out var record))
                    {
                        displayRecords.Add(record);
                        continue;
                    }

                    if (!IsEligibleDashboardAttendanceDate(employee, date, today))
                    {
                        continue;
                    }

                    var dayStatus = await _workingDayService.GetDayStatusAsync(date);
                    string? status = dayStatus switch
                    {
                        "Holiday" => "H",
                        "WeeklyOff" => "WO",
                        "Working" => "M",
                        _ => null
                    };

                    if (status == null)
                    {
                        continue;
                    }

                    displayRecords.Add(new AttendanceModel
                    {
                        EmployeeId = employee.Id,
                        Employee = employee,
                        AttendanceDate = date,
                        Status = status,
                        AttendanceValue = 0m,
                        Remarks = status switch
                        {
                            "H" => "Holiday",
                            "WO" => "Weekly off",
                            _ => "Missing attendance"
                        }
                    });
                }
            }

            return displayRecords;
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

        private static string GetAttendanceStatusText(string? status)
        {
            return status switch
            {
                "P" or "Present" => "Present",
                "A" or "Absent" => "Absent",
                "M" or "Missing" => "Missing",
                "L" or "Leave" => "Leave",
                "H" or "Holiday" => "Holiday",
                "WO" or "WeekOff" or "Weekly Off" => "Week Off",
                "OD" or "On Duty" => "On Duty",
                _ => string.IsNullOrWhiteSpace(status) ? "-" : status
            };
        }

        private async Task<string?> GetCalendarStatusAsync(DateTime date)
        {
            if (await IsOfficeHolidayAsync(date))
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
                    .Get<string[]>()
                ?? Array.Empty<string>();

            if (configuredDays.Any(day =>
                Enum.TryParse(day, true, out DayOfWeek weeklyOffDay) &&
                date.DayOfWeek == weeklyOffDay))
            {
                return true;
            }

            // Apply the configured alternate-Saturday working pattern.
            if (date.DayOfWeek == DayOfWeek.Saturday)
            {
                var saturdayNumber = ((date.Day - 1) / 7) + 1;

                var workingSaturdays =
                    _configuration
                        .GetSection("Attendance:WorkingSaturdayNumbers")
                        .Get<int[]>()
                    ?? new[] { 1, 3, 5 };

                return !workingSaturdays.Contains(saturdayNumber);
            }

            return false;
        }

        private async Task<bool> IsOfficeHolidayAsync(DateTime date)
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

            return await _context.Holidays
                .AsNoTracking()
                .AnyAsync(x =>
                    x.IsActive &&
                    x.HolidayDate.Date == date.Date);
        }

        private static decimal NormalizeAttendanceValue(decimal value)
        {
            return value == 0.5m
                ? 0.5m
                : 1m;
        }

        private async Task NotifyAttendanceChangedAsync(
            int employeeId,
            DateTime attendanceDate,
            string status,
            decimal attendanceValue,
            string title,
            string notificationType)
        {
            var employee = await _employeeRepository.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == employeeId);

            if (string.IsNullOrWhiteSpace(employee?.ApplicationUserId))
            {
                return;
            }

            await _notificationService.CreateAsync(
                employee.ApplicationUserId,
                title,
                $"Your attendance for {attendanceDate:dd-MMM-yyyy} is marked as {status} ({attendanceValue:0.##} day).",
                notificationType,
                "Attendance",
                employeeId,
                "/Attendance/MyMonthly");
        }

        private async Task NotifyAttendanceDeletedAsync(
            int employeeId,
            DateTime attendanceDate)
        {
            var employee = await _employeeRepository.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == employeeId);

            if (string.IsNullOrWhiteSpace(employee?.ApplicationUserId))
            {
                return;
            }

            await _notificationService.CreateAsync(
                employee.ApplicationUserId,
                "Attendance Deleted",
                $"Your attendance record for {attendanceDate:dd-MMM-yyyy} was deleted.",
                "ManualAttendanceDeleted",
                "Attendance",
                employeeId,
                "/Attendance/MyMonthly");
        }

        [Authorize(Roles = "Admin,HR,Master")]
        public async Task<IActionResult> ExportExcel(
               string? searchText,
               DateTime? fromDate,
               DateTime? toDate)
        {
            var today = DateTime.Today;
            var effectiveFromDate =
                fromDate?.Date ??
                new DateTime(today.Year, today.Month, 1);

            var effectiveToDate =
                toDate?.Date ?? today.Date;

            if (effectiveToDate < effectiveFromDate)
            {
                effectiveToDate = effectiveFromDate;
            }

            var employeeQuery = _employeeRepository.Employees
                .AsNoTracking()
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .Where(e => e.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                searchText = searchText.Trim();

                employeeQuery = employeeQuery.Where(e =>
                    (
                        (e.EmployeeCode != null &&
                         e.EmployeeCode.Contains(searchText)) ||
                        (e.FirstName != null &&
                         e.FirstName.Contains(searchText)) ||
                        (e.LastName != null &&
                         e.LastName.Contains(searchText))
                    ));
            }

            var employees = await employeeQuery
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToListAsync();

            var employeeIds = employees
                .Select(e => e.Id)
                .ToHashSet();

            var attendanceRecords = await _attendanceRepository.Attendances
                .AsNoTracking()
                .Include(a => a.Employee)
                .Where(a =>
                    employeeIds.Contains(a.EmployeeId) &&
                    a.AttendanceDate.Date >= effectiveFromDate.Date &&
                    a.AttendanceDate.Date <= effectiveToDate.Date)
                .ToListAsync();

            var attendanceList =
                (await BuildDisplayAttendanceRecordsAsync(
                    employees,
                    attendanceRecords,
                    effectiveFromDate,
                    effectiveToDate,
                    today))
                .OrderByDescending(a => a.AttendanceDate)
                .ThenBy(a => a.Employee?.FirstName)
                .ThenBy(a => a.Employee?.LastName)
                .ToList();

            using var workbook = new XLWorkbook();

            var worksheet =
                workbook.Worksheets.Add("Attendance");

            worksheet.Cell(1, 1).Value = "Employee Code";
            worksheet.Cell(1, 2).Value = "Employee Name";
            worksheet.Cell(1, 3).Value = "Department";
            worksheet.Cell(1, 4).Value = "Designation";
            worksheet.Cell(1, 5).Value = "Date";
            worksheet.Cell(1, 6).Value = "Status";
            worksheet.Cell(1, 7).Value = "Day Value";
            worksheet.Cell(1, 8).Value = "Remarks";
            worksheet.Cell(1, 9).Value = "Created On";

            var headerRange = worksheet.Range("A1:I1");

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
                    GetAttendanceStatusText(attendance.Status);

                worksheet.Cell(row, 7).Value =
                    attendance.AttendanceValue;

                worksheet.Cell(row, 8).Value =
                    attendance.Remarks;

                if (attendance.Id > 0)
                {
                    worksheet.Cell(row, 9).Value =
                        attendance.CreatedOn;
                }
                else
                {
                    worksheet.Cell(row, 9).Value =
                        string.Empty;
                }

                row++;
            }

            worksheet.Columns().AdjustToContents();
            worksheet.Column(5).Style.DateFormat.Format = "dd-MMM-yyyy";

            worksheet.Column(9).Style.DateFormat.Format = "dd-MMM-yyyy HH:mm";



            using var stream =
                new MemoryStream();

            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Attendance_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        [Authorize(Roles = "Admin,HR,Master")]
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
        #endregion
    }
}
