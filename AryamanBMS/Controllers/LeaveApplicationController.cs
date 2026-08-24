using AryamanBMS.Extensions;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interface;
using AryamanBMS.Services.Interfaces;
using AryamanBMS.ViewModels;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using AryamanBMS.Business.Interfaces;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR,Employee,Master")]
    public class LeaveApplicationController : Controller
    {
        #region Actions

        private readonly ILeaveApplicationRepository _leaveApplicationRepository;
        private readonly ILeaveTypeRepository _leaveTypeRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly ILeaveBalanceRepository _leaveBalanceRepository;
        private readonly ILeaveApplicationDayRepository _leaveApplicationDayRepository;

        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly ICompOffCreditRepository _compOffCreditRepository;
        private readonly ICompOffUsageRepository _compOffUsageRepository;
        private readonly INotificationService _notificationService;
        private readonly IWorkingDayService _workingDayService;
        private readonly ILogger<LeaveApplicationController> _logger;
        private const decimal AnnualPaidLeaveEntitlement = 18m;
        private const decimal MonthlyPaidLeaveAccrual = 1.5m;
        private const int FinancialYearStartMonth = 4;
        private const int FinancialYearStartDay = 1;

        public LeaveApplicationController(
              ILeaveApplicationRepository leaveApplicationRepository,
              ILeaveTypeRepository leaveTypeRepository,
              IEmployeeRepository employeeRepository,
              IAttendanceRepository attendanceRepository,
              ILeaveApplicationDayRepository leaveApplicationDayRepository,
              UserManager<ApplicationUserModel> userManager,
              ICompOffCreditRepository compOffCreditRepository,
              ICompOffUsageRepository compOffUsageRepository,
              ILeaveBalanceRepository leaveBalanceRepository,
              INotificationService notificationService,
              IWorkingDayService workingDayService,
              ILogger<LeaveApplicationController> logger)
        {
            _leaveApplicationRepository = leaveApplicationRepository;
            _leaveTypeRepository = leaveTypeRepository;
            _employeeRepository = employeeRepository;
            _attendanceRepository = attendanceRepository;
            _leaveBalanceRepository = leaveBalanceRepository;
            _leaveApplicationDayRepository = leaveApplicationDayRepository;
            _userManager = userManager;
            _compOffCreditRepository = compOffCreditRepository;
            _compOffUsageRepository = compOffUsageRepository;
            _notificationService = notificationService;
            _workingDayService = workingDayService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(
           string? searchText,
           string status = "All",
           string sortBy = "AppliedOn",
           string sortOrder = "desc",
           int page = 1,
           bool mine = false)
        {
            const int pageSize = 10;

            sortBy = sortBy switch
            {
                "ApplicationNumber" => "ApplicationNumber",
                "Employee" => "Employee",
                "LeaveType" => "LeaveType",
                "FromDate" => "FromDate",
                "Status" => "Status",
                _ => "AppliedOn"
            };
            sortOrder = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase)
                ? "asc"
                : "desc";

            var query = _leaveApplicationRepository.LeaveApplications
                .AsNoTracking()
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Include(x => x.LeaveDays)
                .AsQueryable();

            bool isEmployeeOnly =
                User.IsInRole("Employee") &&
                !User.IsInRole("Admin") &&
                !User.IsInRole("HR") ||
                mine;

            if (isEmployeeOnly)
            {
                var user = await _userManager.GetUserAsync(User);

                var employee = user == null
                    ? null
                    : await _employeeRepository.Employees
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.ApplicationUserId == user.Id);

                if (employee == null)
                {
                    TempData["Error"] =
                        "No employee record mapped to this user.";

                    query = query.Where(x => false);
                }
                else
                {
                    query = query.Where(x =>
                        x.EmployeeId == employee.Id);
                }
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                searchText = searchText.Trim();

                query = query.Where(x =>
                    x.ApplicationNumber.Contains(searchText) ||

                    (x.Employee != null &&
                     x.Employee.EmployeeCode != null &&
                     x.Employee.EmployeeCode.Contains(searchText)) ||

                    (x.Employee != null &&
                     x.Employee.FirstName != null &&
                     x.Employee.FirstName.Contains(searchText)) ||

                    (x.Employee != null &&
                     x.Employee.LastName != null &&
                     x.Employee.LastName.Contains(searchText)) ||

                    (x.LeaveType != null &&
                     x.LeaveType.LeaveName.Contains(searchText)));
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                status != "All")
            {
                query = query.Where(x =>
                    x.Status == status);
            }

            query = sortBy switch
            {
                "ApplicationNumber" => sortOrder == "asc"
                    ? query.OrderBy(x => x.ApplicationNumber).ThenBy(x => x.Id)
                    : query.OrderByDescending(x => x.ApplicationNumber).ThenByDescending(x => x.Id),
                "Employee" => sortOrder == "asc"
                    ? query.OrderBy(x => x.Employee!.FirstName).ThenBy(x => x.Employee!.LastName).ThenBy(x => x.Id)
                    : query.OrderByDescending(x => x.Employee!.FirstName).ThenByDescending(x => x.Employee!.LastName).ThenByDescending(x => x.Id),
                "LeaveType" => sortOrder == "asc"
                    ? query.OrderBy(x => x.LeaveType!.LeaveName).ThenBy(x => x.Id)
                    : query.OrderByDescending(x => x.LeaveType!.LeaveName).ThenByDescending(x => x.Id),
                "FromDate" => sortOrder == "asc"
                    ? query.OrderBy(x => x.FromDate).ThenBy(x => x.Id)
                    : query.OrderByDescending(x => x.FromDate).ThenByDescending(x => x.Id),
                "Status" => sortOrder == "asc"
                    ? query.OrderBy(x => x.Status).ThenByDescending(x => x.AppliedOn).ThenByDescending(x => x.Id)
                    : query.OrderByDescending(x => x.Status).ThenByDescending(x => x.AppliedOn).ThenByDescending(x => x.Id),
                _ => sortOrder == "asc"
                    ? query.OrderBy(x => x.AppliedOn).ThenBy(x => x.Id)
                    : query.OrderByDescending(x => x.AppliedOn).ThenByDescending(x => x.Id)
            };

            var routeValues = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                routeValues["searchText"] = searchText;
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                routeValues["status"] = status;
            }

            if (mine)
            {
                routeValues["mine"] = "true";
            }

            routeValues["sortBy"] = sortBy;
            routeValues["sortOrder"] = sortOrder;

            var model = await query.ToPagedListAsync(
                page,
                pageSize,
                routeValues);

            model.Pagination.ControllerName =
                "LeaveApplication";

            model.Pagination.ActionName =
                nameof(Index);

            ViewBag.SearchText = searchText;
            ViewBag.Status = status;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();

            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                var employee =
                    await _employeeRepository.Employees
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.ApplicationUserId == user.Id);

                if (employee != null)
                {
                    ViewBag.PaidLeaveSnapshot =
                        await GetPaidLeaveBalanceSnapshotAsync(
                            employee,
                            DateTime.Today,
                            0m);

                    var availableCompOffDays =
                        await _compOffCreditRepository.CompOffCredits
                            .AsNoTracking()
                            .Where(x =>
                                x.EmployeeId == employee.Id &&
                                x.Status == "Available" &&
                                x.ExpiryDate.Date >= DateTime.Today &&
                                x.CreditDays > x.UsedDays)
                            .SumAsync(x => (decimal?)(x.CreditDays - x.UsedDays))
                        ?? 0m;

                    ViewBag.AvailableCompOffDays =
                        availableCompOffDays;
                }
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeaveApplicationModel leaveApplication)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _employeeRepository.Employees
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == user.Id);

            if (employee == null)
            {
                ModelState.AddModelError(
                    "",
                    "No employee record mapped to this user.");
            }
            else
            {
                leaveApplication.EmployeeId = employee.Id;
            }

            if (leaveApplication.IsHalfDay)
            {
                leaveApplication.ToDate = leaveApplication.FromDate;
                leaveApplication.NumberOfDays =
                    await CalculateLeaveDaysAsync(leaveApplication);

                if (!IsValidHalfDaySession(leaveApplication.HalfDaySession))
                {
                    ModelState.AddModelError(
                        "HalfDaySession",
                        "Please select first half or second half.");
                }
            }
            else
            {
                leaveApplication.HalfDaySession = null;

                leaveApplication.NumberOfDays =
                    await CalculateLeaveDaysAsync(leaveApplication);
            }

            if (leaveApplication.FromDate > leaveApplication.ToDate)
            {
                ModelState.AddModelError(
                    "ToDate",
                    "To Date cannot be earlier than From Date.");
            }

            if (leaveApplication.NumberOfDays <= 0)
            {
                ModelState.AddModelError(
                    "",
                    "Selected leave range does not contain any working leave day.");

                var leaveType = await _leaveTypeRepository.LeaveTypes
                      .FirstOrDefaultAsync(x =>
                      x.Id == leaveApplication.LeaveTypeId);
            }

            var overlappingLeaves =
               await _leaveApplicationRepository.LeaveApplications
                    .AsNoTracking()
                    .Where(x =>
                        x.EmployeeId == leaveApplication.EmployeeId &&
                        x.Status != "Rejected" &&
                        x.Status != "Cancelled" &&
                        leaveApplication.FromDate <= x.ToDate &&
                        leaveApplication.ToDate >= x.FromDate)
                    .ToListAsync();

            bool overlappingLeaveExists =
                overlappingLeaves.Any(x =>
                    IsLeaveOverlap(x, leaveApplication));

            if (overlappingLeaveExists)
            {
                ModelState.AddModelError(
                    "",
                    "Leave already exists for the selected date range.");
            }

            var selectedLeaveType =
              await _leaveTypeRepository.LeaveTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == leaveApplication.LeaveTypeId &&
                    x.IsActive);

            if (selectedLeaveType == null)
            {
                ModelState.AddModelError(
                    "LeaveTypeId",
                    "Selected leave type is inactive or unavailable.");
            }
            else if (employee != null && IsBirthdayLeave(selectedLeaveType))
            {
                if (!employee.DateOfBirth.HasValue)
                {
                    ModelState.AddModelError(
                        "LeaveTypeId",
                        "Birthday Leave cannot be applied because your date of birth is not saved in Employee Details.");
                }
                else if (leaveApplication.NumberOfDays != 1)
                {
                    ModelState.AddModelError(
                        "ToDate",
                        "Birthday Leave can be applied for one day only.");
                }
                else if (GetFinancialYearStart(leaveApplication.FromDate) !=
                         GetFinancialYearStart(leaveApplication.ToDate))
                {
                    ModelState.AddModelError(
                        "ToDate",
                        "Birthday Leave must be taken within one financial year.");
                }
                else
                {
                    var financialYearStart =
                        GetFinancialYearStart(leaveApplication.FromDate);

                    var financialYearEnd =
                        GetFinancialYearEnd(leaveApplication.FromDate);

                    var birthdayLeaveAlreadyUsed =
                        await _leaveApplicationRepository.LeaveApplications
                            .AsNoTracking()
                            .AnyAsync(x =>
                                x.Id != leaveApplication.Id &&
                                x.EmployeeId == employee.Id &&
                                x.LeaveTypeId == selectedLeaveType.Id &&
                                (x.Status == "Pending" ||
                                 x.Status == "Approved") &&
                                x.FromDate.Date >= financialYearStart.Date &&
                                x.FromDate.Date <= financialYearEnd.Date);

                    if (birthdayLeaveAlreadyUsed)
                    {
                        ModelState.AddModelError(
                            "LeaveTypeId",
                            "Birthday Leave has already been applied for this financial year.");
                    }
                }
            }
            else if (string.Equals(
                         selectedLeaveType.LeaveCode,
                         "COMP",
                         StringComparison.OrdinalIgnoreCase) &&
                     employee != null &&
                     leaveApplication.NumberOfDays > 0)
            {
                decimal availableCreditDays =
                    await _compOffCreditRepository.CompOffCredits
                        .AsNoTracking()
                        .Where(x =>
                            x.EmployeeId == employee.Id &&
                            x.Status == "Available" &&
                            x.ExpiryDate.Date >=
                                leaveApplication.ToDate.Date)
                        .SumAsync(x => (decimal?)(x.CreditDays - x.UsedDays))
                    ?? 0m;

                decimal reservedCreditDays =
                     await _leaveApplicationRepository
                         .LeaveApplications
                         .AsNoTracking()
                         .Where(x =>
                             x.EmployeeId == employee.Id &&
                             x.LeaveTypeId == selectedLeaveType.Id &&
                            x.Status == "Pending")
                         .SumAsync(x => (decimal?)x.NumberOfDays)
                     ?? 0m;

                decimal usableCreditDays =
                    availableCreditDays - reservedCreditDays;

                if (usableCreditDays <
                    leaveApplication.NumberOfDays)
                {
                    ModelState.AddModelError(
                        "LeaveTypeId",
                        $"Insufficient Comp Off credit. " +
                        $"Available: {Math.Max(usableCreditDays, 0):0.##} day(s), " +
                        $"Required: {leaveApplication.NumberOfDays:0.##} day(s).");
                }
            }

            ModelState.Remove("Employee");
            ModelState.Remove("LeaveType");
            ModelState.Remove("ApplicationNumber");
            ModelState.Remove("Status");
            ModelState.Remove("PaidDays");
            ModelState.Remove("UnpaidDays");
            ModelState.Remove("ApprovedBy");
            ModelState.Remove("ApprovedOn");
            ModelState.Remove("ApprovalRemarks");

            if (ModelState.IsValid)
            {
                leaveApplication.PaidDays = 0;
                leaveApplication.UnpaidDays = 0;

                leaveApplication.ApplicationNumber = GenerateApplicationNumber();

                leaveApplication.AppliedOn = DateTime.Now;

                leaveApplication.Status = "Pending";

                await _leaveApplicationRepository.AddAsync(leaveApplication);

                await _leaveApplicationRepository.SaveAsync();

                await NotifyHrUsersAsync(
                    notificationType: "LeaveRequest",
                    title: "Leave Request Submitted",
                    message:
                        $"{employee?.FullName ?? "Employee"} submitted leave request " +
                        $"{leaveApplication.ApplicationNumber} for " +
                        $"{leaveApplication.FromDate:dd-MMM-yyyy} to {leaveApplication.ToDate:dd-MMM-yyyy}.",
                    referenceType: "LeaveApplication",
                    referenceId: leaveApplication.Id,
                    actionUrl: $"/LeaveApplication/Details/{leaveApplication.Id}",
                    actionUserId: user.Id);

                TempData["Success"] = "Leave application submitted successfully.";

                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();

            return View(leaveApplication);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var leaveApplication =
                await _leaveApplicationRepository.GetByIdAsync(id);

            if (leaveApplication == null)
            {
                return NotFound();
            }

            if (User.IsInRole("Employee") &&
                !User.IsInRole("Admin") &&
                !User.IsInRole("HR") &&
                !User.IsInRole("Master"))
            {
                var user = await _userManager.GetUserAsync(User);

                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var employee = await _employeeRepository.Employees
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == user.Id);

                if (employee == null ||
                    leaveApplication.EmployeeId != employee.Id)
                {
                    return Forbid();
                }
            }

            var employeeForSnapshot =
                await _employeeRepository.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.Id == leaveApplication.EmployeeId);

            if (employeeForSnapshot != null)
            {
                ViewBag.PaidLeaveSnapshot =
                    await GetPaidLeaveBalanceSnapshotAsync(
                        employeeForSnapshot,
                        leaveApplication.FromDate,
                        leaveApplication.NumberOfDays,
                        leaveApplication.Id);
            }

            return View(leaveApplication);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Approve(int id)
        {
            var leaveApplication =
                await _leaveApplicationRepository.GetByIdAsync(id);

            if (leaveApplication == null)
            {
                return NotFound();
            }

            if (leaveApplication.Status != "Pending")
            {
                TempData["Error"] =
                    "Only pending leave applications can be approved.";

                return RedirectToAction(nameof(Index));
            }

            var leaveType =
                await _leaveTypeRepository.LeaveTypes
                    .FirstOrDefaultAsync(x => x.Id == leaveApplication.LeaveTypeId);

            if (leaveType == null)
            {
                TempData["Error"] = "Leave type not found.";
                return RedirectToAction(nameof(Index));
            }

            bool isCompOff =
                string.Equals(
                    leaveType.LeaveCode,
                    "COMP",
                    StringComparison.OrdinalIgnoreCase);

            bool isBirthdayLeave = IsBirthdayLeave(leaveType);

            leaveApplication.NumberOfDays =
                await CalculateLeaveDaysAsync(leaveApplication);

            if (leaveApplication.NumberOfDays <= 0)
            {
                TempData["Error"] =
                    "Cannot approve leave because the selected range does not contain any working leave day.";

                return RedirectToAction(nameof(Index));
            }

            var compOffAllocations =
                new List<(CompOffCreditModel Credit, decimal DaysToUse)>();

            if (isCompOff)
            {
                var availableCredits =
                    await _compOffCreditRepository.CompOffCredits
                        .Where(x =>
                            x.EmployeeId == leaveApplication.EmployeeId &&
                            x.Status == "Available" &&
                            x.ExpiryDate.Date >= leaveApplication.ToDate.Date &&
                            x.UsedDays < x.CreditDays)
                        .OrderBy(x => x.ExpiryDate)
                        .ThenBy(x => x.WorkedDate)
                        .ThenBy(x => x.Id)
                        .ToListAsync();

                decimal remainingRequiredDays =
                    leaveApplication.NumberOfDays;

                foreach (var credit in availableCredits)
                {
                    if (remainingRequiredDays <= 0)
                    {
                        break;
                    }

                    decimal remainingCreditDays =
                        credit.CreditDays - credit.UsedDays;

                    if (remainingCreditDays <= 0)
                    {
                        continue;
                    }

                    decimal daysToUse =
                        Math.Min(remainingCreditDays, remainingRequiredDays);

                    compOffAllocations.Add((credit, daysToUse));

                    remainingRequiredDays -= daysToUse;
                }

                if (remainingRequiredDays > 0)
                {
                    TempData["Error"] =
                        "Insufficient valid Comp Off credit to approve this leave.";

                    return RedirectToAction(nameof(Index));
                }
            }

            var leaveDates =
                await GetLeaveWorkingDatesAsync(leaveApplication);

            bool attendanceConflict =
                await _attendanceRepository.Attendances
                    .AnyAsync(a =>
                        a.EmployeeId == leaveApplication.EmployeeId &&
                        leaveDates.Contains(a.AttendanceDate.Date) &&
                        !leaveApplication.IsHalfDay &&
                        (
                            a.Status == "P" ||
                            a.Status == "Present" ||
                            a.Status == "OD" ||
                            a.Status == "On Duty" ||
                            a.Status == "OnDuty"
                        ));

            if (attendanceConflict)
            {
                TempData["Error"] =
                    "Cannot approve leave because Present or On Duty attendance already exists in this date range.";

                return RedirectToAction(nameof(Index));
            }

            var employee =
                await _employeeRepository.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == leaveApplication.EmployeeId);

            if (employee == null)
            {
                TempData["Error"] = "Employee not found.";
                return RedirectToAction(nameof(Index));
            }

            if (isBirthdayLeave)
            {
                if (!employee.DateOfBirth.HasValue)
                {
                    TempData["Error"] =
                        "Birthday Leave cannot be approved because the employee date of birth is missing.";

                    return RedirectToAction(nameof(Index));
                }

                if (leaveApplication.NumberOfDays != 1)
                {
                    TempData["Error"] =
                        "Birthday Leave can only be approved for one day.";

                    return RedirectToAction(nameof(Index));
                }

                if (GetFinancialYearStart(leaveApplication.FromDate) !=
                    GetFinancialYearStart(leaveApplication.ToDate))
                {
                    TempData["Error"] =
                        "Birthday Leave must be within one financial year.";

                    return RedirectToAction(nameof(Index));
                }

                var financialYearStart =
                    GetFinancialYearStart(leaveApplication.FromDate);

                var financialYearEnd =
                    GetFinancialYearEnd(leaveApplication.FromDate);

                var birthdayLeaveAlreadyUsed =
                    await _leaveApplicationRepository.LeaveApplications
                        .AsNoTracking()
                        .AnyAsync(x =>
                            x.Id != leaveApplication.Id &&
                            x.EmployeeId == leaveApplication.EmployeeId &&
                            x.LeaveTypeId == leaveApplication.LeaveTypeId &&
                            (x.Status == "Pending" ||
                             x.Status == "Approved") &&
                            x.FromDate.Date >= financialYearStart.Date &&
                            x.FromDate.Date <= financialYearEnd.Date);

                if (birthdayLeaveAlreadyUsed)
                {
                    TempData["Error"] =
                        "Birthday Leave has already been used for this financial year.";

                    return RedirectToAction(nameof(Index));
                }
            }

            if (isCompOff)
            {
                leaveApplication.PaidDays = leaveApplication.NumberOfDays;
                leaveApplication.UnpaidDays = 0;

                TempData["LeaveApprovalSplit"] =
                    $"Comp Off | Paid: {leaveApplication.PaidDays:0.##}, Unpaid: 0";
            }
            else if (isBirthdayLeave)
            {
                leaveApplication.PaidDays =
                    leaveApplication.NumberOfDays;

                leaveApplication.UnpaidDays = 0;

                TempData["LeaveApprovalSplit"] =
                    $"Birthday Leave | Paid: {leaveApplication.PaidDays:0.##}, Unpaid: 0";
            }
            else
            {
                var paidLeaveSnapshot =
                    await GetPaidLeaveBalanceSnapshotAsync(
                        employee,
                        leaveApplication.FromDate,
                        leaveApplication.NumberOfDays,
                        leaveApplication.Id);

                leaveApplication.PaidDays =
                    paidLeaveSnapshot.PaidDaysForRequest;

                leaveApplication.UnpaidDays =
                    paidLeaveSnapshot.UnpaidDaysForRequest;

                TempData["LeaveApprovalSplit"] =
                    $"FY {paidLeaveSnapshot.FinancialYearLabel} | " +
                    $"Entitlement: {paidLeaveSnapshot.ProratedEntitlement:0.##}, " +
                    $"Used: {paidLeaveSnapshot.PaidLeaveUsed:0.##}, " +
                    $"Balance: {paidLeaveSnapshot.PaidLeaveBalance:0.##}, " +
                    $"This Request Paid: {paidLeaveSnapshot.PaidDaysForRequest:0.##}, " +
                    $"Unpaid: {paidLeaveSnapshot.UnpaidDaysForRequest:0.##}";
            }

            leaveApplication.Status = "Approved";
            leaveApplication.ApprovedOn = DateTime.Now;
            leaveApplication.ApprovedBy = User.Identity?.Name;

            if (isCompOff)
            {
                foreach (var allocation in compOffAllocations)
                {
                    var credit = allocation.Credit;

                    credit.UsedDays += allocation.DaysToUse;
                    credit.UpdatedOn = DateTime.Now;

                    if (credit.UsedDays >= credit.CreditDays)
                    {
                        credit.UsedDays = credit.CreditDays;
                        credit.Status = "Used";
                    }
                    else
                    {
                        credit.Status = "Available";
                    }

                    await _compOffCreditRepository.UpdateAsync(credit);

                    var usage = new CompOffUsageModel
                    {
                        CompOffCreditId = credit.Id,
                        LeaveApplicationId = leaveApplication.Id,
                        UsedDays = allocation.DaysToUse,
                        UsedOn = DateTime.Now,
                        IsReversed = false
                    };

                    await _compOffUsageRepository.AddAsync(usage);
                }
            }

            foreach (var date in leaveDates)
            {
                decimal leaveDayValue =
                    GetLeaveDayValue(leaveApplication, date);

                var existingAttendance =
                    await _attendanceRepository.Attendances
                        .FirstOrDefaultAsync(a =>
                            a.EmployeeId == leaveApplication.EmployeeId &&
                            a.AttendanceDate.Date == date);

                if (existingAttendance == null)
                {
                    var attendance = new AttendanceModel
                    {
                        EmployeeId = leaveApplication.EmployeeId,
                        AttendanceDate = date,
                        Status = "L",
                        AttendanceValue = leaveDayValue,
                        Remarks = BuildLeaveAttendanceRemarks(leaveApplication),
                        CreatedOn = DateTime.Now
                    };

                    await _attendanceRepository.AddAsync(attendance);
                }
                else if (IsAttendanceStatus(existingAttendance.Status, "A", "Absent"))
                {
                    existingAttendance.Status = "L";
                    existingAttendance.AttendanceValue = leaveDayValue;
                    existingAttendance.Remarks =
                        $"Absent converted to leave: {BuildLeaveAttendanceRemarks(leaveApplication)}";

                    await _attendanceRepository.UpdateAsync(existingAttendance);
                }
                else if (IsAttendanceStatus(existingAttendance.Status, "L", "Leave"))
                {
                    existingAttendance.AttendanceValue =
                        Math.Max(existingAttendance.AttendanceValue, leaveDayValue);

                    existingAttendance.Remarks =
                        BuildLeaveAttendanceRemarks(leaveApplication);

                    await _attendanceRepository.UpdateAsync(existingAttendance);
                }
            }

            await _leaveApplicationRepository.UpdateAsync(leaveApplication);
            await _leaveApplicationRepository.SaveAsync();

            await RebuildLeaveApplicationDaysAsync(leaveApplication);

            if (isCompOff)
            {
                await _compOffCreditRepository.SaveAsync();
                await _compOffUsageRepository.SaveAsync();
            }

            await _attendanceRepository.SaveAsync();

            await NotifyEmployeeLeaveAsync(
                leaveApplication,
                notificationType: "LeaveApproved",
                title: "Leave Approved",
                message:
                      $"Your leave request {leaveApplication.ApplicationNumber} " +
                      $"has been approved for {leaveApplication.FromDate:dd-MMM-yyyy} " +
                      $"to {leaveApplication.ToDate:dd-MMM-yyyy}. " +
                      $"Paid: {leaveApplication.PaidDays:0.##} day(s), " +
                      $"Unpaid: {leaveApplication.UnpaidDays:0.##} day(s).",
                actionUrl: $"/LeaveApplication/Details/{leaveApplication.Id}");

            TempData["Success"] =
                isCompOff
                    ? "Comp Off leave approved, credits consumed and attendance marked."
                    : "Leave application approved and attendance marked.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,HR")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var leaveApplication =
                await _leaveApplicationRepository.GetByIdAsync(id);

            if (leaveApplication == null)
            {
                return NotFound();
            }

            if (leaveApplication.Status != "Pending")
            {
                TempData["Error"] =
                    "Only pending leave applications can be rejected.";

                return RedirectToAction(nameof(Index));
            }

            leaveApplication.PaidDays = 0;
            leaveApplication.UnpaidDays = 0;
            leaveApplication.Status = "Rejected";
            leaveApplication.ApprovedOn = DateTime.Now;
            leaveApplication.ApprovedBy = User.Identity?.Name;

            await _leaveApplicationRepository.UpdateAsync(leaveApplication);
            await _leaveApplicationRepository.SaveAsync();

            await NotifyEmployeeLeaveAsync(
                leaveApplication,
                notificationType: "LeaveRejected",
                title: "Leave Rejected",
                message:
                    $"Your leave request {leaveApplication.ApplicationNumber} " +
                    $"has been rejected. No paid or unpaid leave days were consumed.",
                actionUrl: $"/LeaveApplication/Details/{leaveApplication.Id}");

            TempData["Success"] =
                "Leave application rejected.";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdowns()
        {
            ViewBag.LeaveTypes =
                await _leaveTypeRepository.LeaveTypes
                .Where(x => x.IsActive)
                .OrderBy(x => x.LeaveName)
                .ToListAsync();
        }

        private string GenerateApplicationNumber()
        {
            int nextId =
                _leaveApplicationRepository.LeaveApplications
                .Count() + 1;

            return $"LA{nextId:00000}";
        }

        [HttpPost]
        [Authorize(Roles = "Admin,HR,Employee,Master")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var leaveApplication =
                await _leaveApplicationRepository.GetByIdAsync(id);

            if (leaveApplication == null)
            {
                return NotFound();
            }

            if (leaveApplication.Status != "Pending")
            {
                TempData["Error"] =
                    "Only pending leave applications can be cancelled.";

                return RedirectToAction(nameof(Index));
            }

            if (!User.IsInRole("Admin") && !User.IsInRole("HR"))
            {
                var user = await _userManager.GetUserAsync(User);

                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var employee = await _employeeRepository.Employees
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == user.Id);

                if (employee == null ||
                    leaveApplication.EmployeeId != employee.Id)
                {
                    return Forbid();
                }
            }

            leaveApplication.PaidDays = 0;
            leaveApplication.UnpaidDays = 0;
            leaveApplication.Status = "Cancelled";
            leaveApplication.ApprovedOn = DateTime.Now;
            leaveApplication.ApprovedBy = User.Identity?.Name;
            leaveApplication.ApprovalRemarks = "Cancelled by user.";

            await _leaveApplicationRepository.UpdateAsync(leaveApplication);
            await _leaveApplicationRepository.SaveAsync();

            TempData["Success"] =
                "Leave application cancelled successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR,Employee,Master")]
        public async Task<IActionResult> RequestCancellation(
           int id,
           int[]? leaveDayIds,
           string cancellationReason)
        {
            var leaveApplication =
                await _leaveApplicationRepository.GetByIdAsync(id);

            if (leaveApplication == null)
            {
                return NotFound();
            }

            // Employee can request cancellation only for their own leave
            if (!User.IsInRole("Admin") && !User.IsInRole("HR"))
            {
                var user = await _userManager.GetUserAsync(User);

                if (user == null)
                {
                    return Forbid();
                }

                var employee =
                    await _employeeRepository.Employees
                        .FirstOrDefaultAsync(x =>
                            x.ApplicationUserId == user.Id);

                if (employee == null ||
                    leaveApplication.EmployeeId != employee.Id)
                {
                    return Forbid();
                }
            }

            if (leaveApplication.Status != "Approved")
            {
                TempData["Error"] =
                    "Only approved leave applications can be requested for cancellation.";

                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(cancellationReason))
            {
                TempData["Error"] =
                    "Cancellation reason is required.";

                return RedirectToAction(nameof(Index));
            }

            var leaveDays =
                await EnsureLeaveApplicationDaysAsync(leaveApplication);

            if (!leaveDays.Any(x => x.Status == "Active"))
            {
                TempData["Error"] =
                    "No active leave days are available for cancellation.";

                return RedirectToAction(nameof(Index));
            }

            if (leaveDays.Any(x => x.Status == "CancellationRequested"))
            {
                TempData["Error"] =
                    "A cancellation request is already pending for this leave application.";

                return RedirectToAction(nameof(Index));
            }

            var selectedLeaveDays =
                leaveDayIds != null && leaveDayIds.Any()
                    ? leaveDays
                        .Where(x =>
                            leaveDayIds.Contains(x.Id) &&
                            x.Status == "Active")
                        .ToList()
                    : leaveDays
                        .Where(x => x.Status == "Active")
                        .ToList();

            if (!selectedLeaveDays.Any())
            {
                TempData["Error"] =
                    "Please select at least one active leave day to cancel.";

                return RedirectToAction(nameof(Index));
            }

            leaveApplication.CancellationStatus = "Pending";
            leaveApplication.CancellationReason =
                cancellationReason.Trim();

            leaveApplication.CancellationRequestedOn =
                DateTime.Now;

            leaveApplication.CancellationRequestedBy =
                User.Identity?.Name;

            leaveApplication.CancellationReviewedOn = null;
            leaveApplication.CancellationReviewedBy = null;
            leaveApplication.CancellationRemarks = null;

            foreach (var leaveDay in selectedLeaveDays)
            {
                leaveDay.Status = "CancellationRequested";
                leaveDay.CancellationReason = cancellationReason.Trim();
                leaveDay.CancellationRequestedOn = DateTime.Now;
                leaveDay.CancellationRequestedBy = User.Identity?.Name;
                leaveDay.CancellationReviewedOn = null;
                leaveDay.CancellationReviewedBy = null;
                leaveDay.CancellationRemarks = null;

                await _leaveApplicationDayRepository.UpdateAsync(leaveDay);
            }

            await _leaveApplicationRepository
                .UpdateAsync(leaveApplication);

            await _leaveApplicationDayRepository.SaveAsync();
            await _leaveApplicationRepository.SaveAsync();

            await NotifyHrUsersAsync(
                notificationType: "LeaveCancellationRequested",
                title: "Leave Cancellation Requested",
                message:
                    $"{leaveApplication.Employee?.FullName ?? "Employee"} requested cancellation for " +
                    $"{selectedLeaveDays.Sum(x => x.DayValue):0.##} day(s) from leave {leaveApplication.ApplicationNumber}.",
                referenceType: "LeaveApplication",
                referenceId: leaveApplication.Id,
                actionUrl: $"/LeaveApplication/Details/{leaveApplication.Id}",
                actionUserId: _userManager.GetUserId(User));

            TempData["Success"] =
                "Leave cancellation request submitted successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> ApproveCancellation(
    int id,
    string? cancellationRemarks)
        {
            var leaveApplication =
                await _leaveApplicationRepository.GetByIdAsync(id);

            if (leaveApplication == null)
            {
                return NotFound();
            }

            if (leaveApplication.Status != "Approved")
            {
                TempData["Error"] =
                    "Only approved leave applications can be cancelled.";

                return RedirectToAction(nameof(Index));
            }

            if (leaveApplication.CancellationStatus != "Pending")
            {
                TempData["Error"] =
                    "Only pending cancellation requests can be approved.";

                return RedirectToAction(nameof(Index));
            }

            var leaveType =
                await _leaveTypeRepository.LeaveTypes
                    .FirstOrDefaultAsync(x =>
                        x.Id == leaveApplication.LeaveTypeId);

            if (leaveType == null)
            {
                TempData["Error"] = "Leave type not found.";

                return RedirectToAction(nameof(Index));
            }

            bool isCompOff =
                string.Equals(
                    leaveType.LeaveCode,
                    "COMP",
                    StringComparison.OrdinalIgnoreCase);

            var leaveDays =
                await EnsureLeaveApplicationDaysAsync(leaveApplication);

            var cancellationRequestedDays =
                leaveDays
                    .Where(x => x.Status == "CancellationRequested")
                    .ToList();

            if (!cancellationRequestedDays.Any())
            {
                TempData["Error"] =
                    "No leave days are pending cancellation for this application.";

                return RedirectToAction(nameof(Index));
            }

            decimal cancelledDayValue =
                cancellationRequestedDays.Sum(x => x.DayValue);

            if (isCompOff)
            {
                bool compOffReversed =
                    await ReverseCompOffUsageAsync(
                        leaveApplication.Id,
                        cancelledDayValue);

                if (!compOffReversed)
                {
                    TempData["Error"] =
                        "Comp Off usage records were not found.";

                    return RedirectToAction(nameof(Index));
                }
            }

            var cancellationDates =
                cancellationRequestedDays
                    .Select(x => x.LeaveDate.Date)
                    .ToList();

            var attendanceRecords =
                await _attendanceRepository.Attendances
                    .Where(x =>
                        x.EmployeeId ==
                            leaveApplication.EmployeeId &&
                        cancellationDates.Contains(x.AttendanceDate.Date) &&
                        x.Remarks != null &&
                        x.Remarks.Contains(
                            leaveApplication.ApplicationNumber))
                    .ToListAsync();

            attendanceRecords = attendanceRecords
                .Where(x =>
                    IsAttendanceStatus(x.Status, "L", "Leave"))
                .ToList();

            foreach (var attendance in attendanceRecords)
            {
                await _attendanceRepository.DeleteAsync(attendance);
            }

            foreach (var leaveDay in cancellationRequestedDays)
            {
                leaveDay.Status = "Cancelled";
                leaveDay.CancellationReviewedOn = DateTime.Now;
                leaveDay.CancellationReviewedBy = User.Identity?.Name;
                leaveDay.CancellationRemarks =
                    string.IsNullOrWhiteSpace(cancellationRemarks)
                        ? null
                        : cancellationRemarks.Trim();

                await _leaveApplicationDayRepository.UpdateAsync(leaveDay);
            }

            var activeLeaveDays =
                leaveDays
                    .Where(x => x.Status == "Active")
                    .ToList();

            leaveApplication.NumberOfDays =
                activeLeaveDays.Sum(x => x.DayValue);

            leaveApplication.PaidDays =
                activeLeaveDays.Sum(x => x.PaidDays);

            leaveApplication.UnpaidDays =
                activeLeaveDays.Sum(x => x.UnpaidDays);

            bool hasActiveLeaveDays =
                activeLeaveDays.Any();

            leaveApplication.Status =
                hasActiveLeaveDays
                    ? "Approved"
                    : "Cancelled";

            leaveApplication.CancellationStatus =
                hasActiveLeaveDays
                    ? null
                    : "Approved";

            leaveApplication.CancellationReviewedOn = DateTime.Now;
            leaveApplication.CancellationReviewedBy = User.Identity?.Name;
            leaveApplication.CancellationRemarks =
                string.IsNullOrWhiteSpace(cancellationRemarks)
                    ? null
                    : cancellationRemarks.Trim();

            await _leaveApplicationRepository
                .UpdateAsync(leaveApplication);

            if (isCompOff)
            {
                await _compOffCreditRepository.SaveAsync();
                await _compOffUsageRepository.SaveAsync();
            }

            await _leaveApplicationDayRepository.SaveAsync();
            await _leaveApplicationRepository.SaveAsync();

            await _attendanceRepository.SaveAsync();

            await NotifyEmployeeLeaveAsync(
                leaveApplication,
                notificationType: "LeaveCancellationApproved",
                title: "Leave Cancellation Approved",
                message:
                    $"Cancellation for {cancelledDayValue:0.##} day(s) from leave request {leaveApplication.ApplicationNumber} " +
                    "has been approved.",
                actionUrl: $"/LeaveApplication/Details/{leaveApplication.Id}");

            TempData["Success"] =
    isCompOff
        ? "Comp Off cancellation approved. Credit restored and attendance updated."
        : "Leave cancellation approved. Balance and attendance updated.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> RejectCancellation(
    int id,
    string? cancellationRemarks)
        {
            var leaveApplication =
                await _leaveApplicationRepository.GetByIdAsync(id);

            if (leaveApplication == null)
            {
                return NotFound();
            }

            if (leaveApplication.Status != "Approved")
            {
                TempData["Error"] =
                    "Only approved leave applications can have cancellation requests.";

                return RedirectToAction(nameof(Index));
            }

            if (leaveApplication.CancellationStatus != "Pending")
            {
                TempData["Error"] =
                    "Only pending cancellation requests can be rejected.";

                return RedirectToAction(nameof(Index));
            }

            leaveApplication.CancellationStatus = "Rejected";
            leaveApplication.CancellationReviewedOn = DateTime.Now;
            leaveApplication.CancellationReviewedBy =
                User.Identity?.Name;

            leaveApplication.CancellationRemarks =
                string.IsNullOrWhiteSpace(cancellationRemarks)
                    ? null
                    : cancellationRemarks.Trim();

            var leaveDays =
                await _leaveApplicationDayRepository.LeaveApplicationDays
                    .Where(x =>
                        x.LeaveApplicationId == leaveApplication.Id &&
                        x.Status == "CancellationRequested")
                    .ToListAsync();

            foreach (var leaveDay in leaveDays)
            {
                leaveDay.Status = "Active";
                leaveDay.CancellationReviewedOn = DateTime.Now;
                leaveDay.CancellationReviewedBy = User.Identity?.Name;
                leaveDay.CancellationRemarks =
                    string.IsNullOrWhiteSpace(cancellationRemarks)
                        ? null
                        : cancellationRemarks.Trim();

                await _leaveApplicationDayRepository.UpdateAsync(leaveDay);
            }

            await _leaveApplicationRepository
                .UpdateAsync(leaveApplication);

            await _leaveApplicationDayRepository.SaveAsync();
            await _leaveApplicationRepository.SaveAsync();

            await NotifyEmployeeLeaveAsync(
                leaveApplication,
                notificationType: "LeaveCancellationRejected",
                title: "Leave Cancellation Rejected",
                message:
                    $"Cancellation for leave request {leaveApplication.ApplicationNumber} " +
                    "has been rejected.",
                actionUrl: $"/LeaveApplication/Details/{leaveApplication.Id}");

            TempData["Success"] =
                "Leave cancellation request rejected.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = "Admin,HR,Master")]
        public async Task<IActionResult> PaidLeaveBalanceRegister(
            int? year,
            string? searchText,
            string sortBy = "EmployeeCode",
            string sortOrder = "asc",
            int page = 1)
        {
            const int pageSize = 10;
            DateTime today = DateTime.Today;

            int selectedYear =
                year ?? (today.Month >= 4 ? today.Year : today.Year - 1);

            DateTime fyStart = new DateTime(selectedYear, 4, 1);
            DateTime fyEnd = fyStart.AddYears(1).AddDays(-1);

            var employeesQuery =
                _employeeRepository.Employees
                    .AsNoTracking()
                    .Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string search = searchText.Trim();

                employeesQuery =
                    employeesQuery.Where(x =>
                        x.EmployeeCode!.Contains(search) ||
                        x.FirstName!.Contains(search) ||
                        x.LastName!.Contains(search));
            }

            var employees =
                await employeesQuery
                    .OrderBy(x => x.EmployeeCode)
                    .ToListAsync();

            var employeeIds =
                employees.Select(x => x.Id).ToList();

            var approvedPaidLeaves =
                await _leaveApplicationRepository.LeaveApplications
                    .AsNoTracking()
                    .Include(x => x.LeaveType)
                    .Where(x =>
                        employeeIds.Contains(x.EmployeeId) &&
                        x.Status == "Approved" &&
                        x.FromDate.Date >= fyStart.Date &&
                        x.FromDate.Date <= fyEnd.Date &&
                        (
                            x.LeaveType == null ||
                            x.LeaveType.LeaveCode == null ||
                            (
                                x.LeaveType.LeaveCode != "COMP" &&
                                x.LeaveType.LeaveCode != "BDL"
                            )
                        ))
                    .GroupBy(x => x.EmployeeId)
                    .Select(x => new
                    {
                        EmployeeId = x.Key,
                        PaidUsed = x.Sum(l => l.PaidDays)
                    })
                    .ToListAsync();

            var usedMap =
                approvedPaidLeaves.ToDictionary(
                    x => x.EmployeeId,
                    x => x.PaidUsed);

            var pendingPaidLeaves =
                await _leaveApplicationRepository.LeaveApplications
                    .AsNoTracking()
                    .Include(x => x.LeaveType)
                    .Where(x =>
                        employeeIds.Contains(x.EmployeeId) &&
                        x.Status == "Pending" &&
                        x.FromDate.Date >= fyStart.Date &&
                        x.FromDate.Date <= fyEnd.Date &&
                        (
                            x.LeaveType == null ||
                            x.LeaveType.LeaveCode == null ||
                            (
                                x.LeaveType.LeaveCode != "COMP" &&
                                x.LeaveType.LeaveCode != "BDL"
                            )
                        ))
                    .GroupBy(x => x.EmployeeId)
                    .Select(x => new
                    {
                        EmployeeId = x.Key,
                        PendingRequested = x.Sum(l => l.NumberOfDays)
                    })
                    .ToListAsync();

            var pendingMap =
                pendingPaidLeaves.ToDictionary(
                    x => x.EmployeeId,
                    x => x.PendingRequested);

            var carryForwardEntries = await _leaveBalanceRepository.LeaveBalances
        .AsNoTracking()
        .Include(x => x.LeaveType)
        .Where(x =>
            employeeIds.Contains(x.EmployeeId) &&
            x.LeaveYear == selectedYear &&
            x.LeaveType.IsPaidLeave &&
            x.LeaveType.LeaveCode != "COMP" &&
            x.LeaveType.LeaveCode != "BDL")
        .GroupBy(x => x.EmployeeId)
        .Select(x => new
        {
            EmployeeId = x.Key,
            CarryForwardDays = x.Sum(b => b.CarryForwardDays)
        })
        .ToListAsync();

            var carryForwardMap =
                carryForwardEntries.ToDictionary(
                    x => x.EmployeeId,
                    x => x.CarryForwardDays);

            var approvedBirthdayLeaves =
            await _leaveApplicationRepository.LeaveApplications
                .AsNoTracking()
                .Where(x =>
                    employeeIds.Contains(x.EmployeeId) &&
                    x.Status == "Approved" &&
                    x.LeaveType != null &&
                    x.LeaveType.LeaveCode == "BDL" &&
                    x.FromDate.Date >= fyStart.Date &&
                    x.FromDate.Date <= fyEnd.Date)
                .GroupBy(x => x.EmployeeId)
                .Select(x => new
                {
                    EmployeeId = x.Key,
                    BirthdayUsed = x.Sum(l => l.PaidDays)
                })
                .ToListAsync();

            var birthdayUsedMap =
                approvedBirthdayLeaves.ToDictionary(
                    x => x.EmployeeId,
                    x => x.BirthdayUsed);

            var model =
                employees.Select(employee =>
                {
                    decimal entitlement =
                        GetProratedPaidLeaveEntitlement(
                            employee.JoiningDate,
                            fyStart);

                    decimal carryForwardDays =
                        carryForwardMap.TryGetValue(employee.Id, out var carryForwardValue)
                            ? carryForwardValue
                            : 0m;

                    decimal used =
                        usedMap.TryGetValue(employee.Id, out var value)
                            ? value
                            : 0m;

                    decimal balanceBeforePending =
                        Math.Max(0, carryForwardDays + entitlement - used);

                    decimal pendingRequested =
                        pendingMap.TryGetValue(employee.Id, out var pendingValue)
                            ? pendingValue
                            : 0m;

                    decimal pendingReserved =
                        Math.Min(pendingRequested, balanceBeforePending);

                    decimal birthdayUsed = birthdayUsedMap.TryGetValue(employee.Id, out var birthdayValue)
                                          ? birthdayValue
                                          : 0m;

                    return new EmployeePaidLeaveBalanceViewModel
                    {
                        EmployeeId = employee.Id,
                        EmployeeCode = employee.EmployeeCode ?? "-",
                        EmployeeName = employee.FullName,
                        JoiningDate = employee.JoiningDate,
                        FinancialYearStart = fyStart,
                        FinancialYearEnd = fyEnd,
                        AnnualEntitlement = AnnualPaidLeaveEntitlement,
                        MonthlyAccrual = MonthlyPaidLeaveAccrual,
                        ProratedEntitlement = entitlement,
                        CarryForwardDays = carryForwardDays,
                        PaidUsed = used,
                        PendingPaidLeaveReserved = pendingReserved,
                        PaidBalance = Math.Max(0, balanceBeforePending - pendingReserved),
                        BirthdayLeave = new BirthdayLeaveBalanceViewModel
                        {
                            Entitlement = 1m,
                            Used = Math.Min(birthdayUsed, 1m)
                        }
                    };
                })
                .ToList();

            sortBy = sortBy switch
            {
                "EmployeeName" => "EmployeeName",
                "JoiningDate" => "JoiningDate",
                "PaidBalance" => "PaidBalance",
                "BirthdayLeave" => "BirthdayLeave",
                _ => "EmployeeCode"
            };
            sortOrder = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase)
                ? "desc"
                : "asc";

            model = sortBy switch
            {
                "EmployeeName" => sortOrder == "desc"
                    ? model.OrderByDescending(x => x.EmployeeName).ToList()
                    : model.OrderBy(x => x.EmployeeName).ToList(),
                "JoiningDate" => sortOrder == "desc"
                    ? model.OrderByDescending(x => x.JoiningDate).ToList()
                    : model.OrderBy(x => x.JoiningDate).ToList(),
                "PaidBalance" => sortOrder == "desc"
                    ? model.OrderByDescending(x => x.PaidBalance).ToList()
                    : model.OrderBy(x => x.PaidBalance).ToList(),
                "BirthdayLeave" => sortOrder == "desc"
                    ? model.OrderByDescending(x => x.BirthdayLeave.Available).ToList()
                    : model.OrderBy(x => x.BirthdayLeave.Available).ToList(),
                _ => sortOrder == "desc"
                    ? model.OrderByDescending(x => x.EmployeeCode).ToList()
                    : model.OrderBy(x => x.EmployeeCode).ToList()
            };

            int totalRecords = model.Count;
            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            page = Math.Clamp(page, 1, Math.Max(totalPages, 1));
            var pageItems = model
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.SelectedYear = selectedYear;
            ViewBag.SearchText = searchText;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRecords = totalRecords;
            ViewBag.TotalPages = totalPages;

            return View(pageItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> UpdatePaidLeaveCarryForward(
    int employeeId,
    int year,
    decimal carryForwardDays)
        {
            carryForwardDays = Math.Max(0, carryForwardDays);

            var employee =
                await _employeeRepository.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == employeeId);

            if (employee == null)
            {
                TempData["Error"] = "Employee not found.";
                return RedirectToAction(nameof(PaidLeaveBalanceRegister), new { year });
            }

            var leaveType =
                await _leaveTypeRepository.LeaveTypes
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive &&
                        x.IsPaidLeave &&
                        x.LeaveCode != "COMP" &&
                        x.LeaveCode != "BDL")
                    .OrderBy(x => x.Id)
                    .FirstOrDefaultAsync();

            if (leaveType == null)
            {
                TempData["Error"] = "Regular paid leave type not found.";
                return RedirectToAction(nameof(PaidLeaveBalanceRegister), new { year });
            }

            DateTime fyStart = new DateTime(year, 4, 1);

            var balance =
                await _leaveBalanceRepository.LeaveBalances
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == employeeId &&
                        x.LeaveTypeId == leaveType.Id &&
                        x.LeaveYear == year);

            decimal currentYearAllocation =
                GetProratedPaidLeaveEntitlement(
                    employee.JoiningDate,
                    fyStart);

            decimal approvedPaidUsed =
                await _leaveApplicationRepository.LeaveApplications
                    .AsNoTracking()
                    .Include(x => x.LeaveType)
                    .Where(x =>
                        x.EmployeeId == employeeId &&
                        x.Status == "Approved" &&
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
                    .SumAsync(x => (decimal?)x.PaidDays) ?? 0m;

            if (balance == null)
            {
                balance = new LeaveBalanceModel
                {
                    EmployeeId = employeeId,
                    LeaveTypeId = leaveType.Id,
                    LeaveYear = year
                };

                await _leaveBalanceRepository.AddAsync(balance);
            }

            balance.CurrentYearAllocation = currentYearAllocation;
            balance.CarryForwardDays = carryForwardDays;
            balance.AllocatedDays = currentYearAllocation + carryForwardDays;
            balance.UsedDays = approvedPaidUsed;
            balance.BalanceDays = Math.Max(0, balance.AllocatedDays - approvedPaidUsed);

            await _leaveBalanceRepository.SaveAsync();

            TempData["Success"] = "Paid leave carry forward updated.";

            return RedirectToAction(nameof(PaidLeaveBalanceRegister), new { year });
        }

        [HttpGet]
        [Authorize(Roles = "Employee,Admin,HR,Master")]
        public async Task<IActionResult> MyPaidLeaveBalance(int? year)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee =
                await _employeeRepository.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);

            if (employee == null)
            {
                TempData["Error"] =
                    "No employee record is mapped to your user account.";

                return RedirectToAction(nameof(Index));
            }

            DateTime today = DateTime.Today;

            int selectedYear =
                year ?? (today.Month >= 4 ? today.Year : today.Year - 1);

            DateTime fyStart = new DateTime(selectedYear, 4, 1);
            DateTime fyEnd = fyStart.AddYears(1).AddDays(-1);

            var snapshot =
                await GetPaidLeaveBalanceSnapshotAsync(
                    employee,
                    fyStart,
                    0m);

            ViewBag.SelectedYear = selectedYear;
            int firstYear =
                employee.JoiningDate.Month >= FinancialYearStartMonth
                    ? employee.JoiningDate.Year
                    : employee.JoiningDate.Year - 1;

            int currentYear =
                today.Month >= FinancialYearStartMonth
                    ? today.Year
                    : today.Year - 1;

            ViewBag.YearOptions =
                Enumerable.Range(firstYear, Math.Max(1, currentYear - firstYear + 1))
                    .Reverse()
                    .ToList();

            return View(snapshot);
        }

        private async Task NotifyEmployeeLeaveAsync(
            LeaveApplicationModel leaveApplication,
            string notificationType,
            string title,
            string message,
            string actionUrl)
        {
            try
            {
                string? recipientUserId =
                    leaveApplication.Employee?.ApplicationUserId;

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
                        "LeaveApplication",
                        leaveApplication.Id);

                if (exists)
                {
                    return;
                }

                await _notificationService.CreateAsync(
                    userId: recipient.Id,
                    title: title,
                    message: message,
                    notificationType: notificationType,
                    referenceType: "LeaveApplication",
                    referenceId: leaveApplication.Id,
                    actionUrl: actionUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Leave notification failed. Type: {NotificationType}, LeaveApplicationId: {LeaveApplicationId}",
                    notificationType,
                    leaveApplication.Id);
            }
        }

        private async Task NotifyHrUsersAsync(
            string notificationType,
            string title,
            string message,
            string referenceType,
            int referenceId,
            string actionUrl,
            string? actionUserId)
        {
            try
            {
                var admins =
                    await _userManager.GetUsersInRoleAsync("Admin");

                var hrUsers =
                    await _userManager.GetUsersInRoleAsync("HR");

                var recipients = admins
                    .Concat(hrUsers)
                    .Where(x =>
                        x.IsActive &&
                        x.Id != actionUserId)
                    .GroupBy(x => x.Id)
                    .Select(x => x.First())
                    .ToList();

                foreach (var recipient in recipients)
                {
                    bool exists =
                        await _notificationService.ExistsAsync(
                            recipient.Id,
                            notificationType,
                            referenceType,
                            referenceId);

                    if (exists)
                    {
                        continue;
                    }

                    await _notificationService.CreateAsync(
                        userId: recipient.Id,
                        title: title,
                        message: message,
                        notificationType: notificationType,
                        referenceType: referenceType,
                        referenceId: referenceId,
                        actionUrl: actionUrl);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Leave broadcast notification failed. Type: {NotificationType}, Reference: {ReferenceType}/{ReferenceId}",
                    notificationType,
                    referenceType,
                    referenceId);
            }
        }

        [Authorize(Roles = "Admin,HR,Master")]
        public async Task<IActionResult> Dashboard()
        {
            DateTime today = DateTime.Today;
            DateTime monthStart = new(today.Year, today.Month, 1);
            DateTime nextMonth = monthStart.AddMonths(1);
            DateTime upcomingEnd = today.AddDays(30);
            DateTime compOffExpiryEnd = today.AddDays(15);

            int financialYearStartYear =
                today.Month >= 4 ? today.Year : today.Year - 1;

            var applications = await _leaveApplicationRepository.LeaveApplications
                .AsNoTracking()
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .ToListAsync();

            var compOffCredits = await _compOffCreditRepository.CompOffCredits
                .AsNoTracking()
                .Include(x => x.Employee)
                .ToListAsync();

            var compOffUsages = await _compOffUsageRepository.CompOffUsages
                .AsNoTracking()
                .ToListAsync();

            var model = new LeaveDashboardViewModel
            {
                Today = today,
                FinancialYear = $"{financialYearStartYear}-{(financialYearStartYear + 1).ToString()[2..]}"
            };

            model.Summary.TotalThisMonth = applications.Count(x =>
                x.AppliedOn >= monthStart &&
                x.AppliedOn < nextMonth);

            model.Summary.PendingApplications = applications.Count(x =>
                x.Status == "Pending");

            model.Summary.ApprovedThisMonth = applications.Count(x =>
                x.Status == "Approved" &&
                x.ApprovedOn.HasValue &&
                x.ApprovedOn.Value >= monthStart &&
                x.ApprovedOn.Value < nextMonth);

            model.Summary.RejectedThisMonth = applications.Count(x =>
                x.Status == "Rejected" &&
                x.ApprovedOn.HasValue &&
                x.ApprovedOn.Value >= monthStart &&
                x.ApprovedOn.Value < nextMonth);

            model.Summary.CancelledThisMonth = applications.Count(x =>
                x.Status == "Cancelled" &&
                x.CancellationReviewedOn.HasValue &&
                x.CancellationReviewedOn.Value >= monthStart &&
                x.CancellationReviewedOn.Value < nextMonth);

            model.Summary.CancellationRequests = applications.Count(x =>
    x.CancellationStatus == "Pending");

            model.Summary.OnLeaveToday = applications.Count(x =>
                x.Status == "Approved" &&
                x.FromDate <= today &&
                x.ToDate >= today);

            model.Summary.UpcomingLeaves = applications.Count(x =>
                x.Status == "Approved" &&
                x.FromDate > today &&
                x.FromDate <= upcomingEnd);

            model.CompOff.PendingRequests = compOffCredits.Count(x =>
                x.Status == "Pending");

            model.CompOff.ApprovedAvailableCredits = compOffCredits.Count(x =>
                x.Status == "Available" &&
                x.ExpiryDate >= today &&
                x.CreditDays > x.UsedDays);

            model.CompOff.ExpiringSoon = compOffCredits.Count(x =>
                x.Status == "Available" &&
                x.ExpiryDate >= today &&
                x.ExpiryDate <= compOffExpiryEnd &&
                x.CreditDays > x.UsedDays);

            model.CompOff.UsedThisMonth = compOffUsages
                .Where(x =>
                    !x.IsReversed &&
                    x.UsedOn >= monthStart &&
                    x.UsedOn < nextMonth)
                .Sum(x => x.UsedDays);

            model.StatusBuckets = BuildLeaveBuckets(
                applications
                    .GroupBy(x => x.Status)
                    .Select(x => new LeaveDashboardBucket
                    {
                        Label = x.Key,
                        Count = x.Count(),
                        CssClass = GetLeaveStatusClass(x.Key)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList());

            model.LeaveTypeBuckets = BuildLeaveBuckets(
                applications
                    .Where(x => x.Status == "Approved")
                    .GroupBy(x => x.LeaveType != null ? x.LeaveType.LeaveName : "Unassigned")
                    .Select(x => new LeaveDashboardBucket
                    {
                        Label = x.Key,
                        Count = x.Count(),
                        Days = x.Sum(y => y.NumberOfDays),
                        CssClass = "bucket-info"
                    })
                    .OrderByDescending(x => x.Days)
                    .Take(6)
                    .ToList());

            model.PendingApplications = applications
                .Where(x => x.Status == "Pending")
                .OrderBy(x => x.AppliedOn)
                .Take(5)
                .Select(ToLeaveDashboardListItem)
                .ToList();

            model.OnLeaveToday = applications
                .Where(x =>
                    x.Status == "Approved" &&
                    x.FromDate <= today &&
                    x.ToDate >= today)
                .OrderBy(x => x.ToDate)
                .Take(5)
                .Select(x =>
                {
                    var item = ToLeaveDashboardListItem(x);
                    item.Meta = $"Until {x.ToDate:dd-MMM-yyyy}";
                    item.Badge = "On Leave";
                    return item;
                })
                .ToList();

            model.UpcomingLeaves = applications
                .Where(x =>
                    x.Status == "Approved" &&
                    x.FromDate > today &&
                    x.FromDate <= upcomingEnd)
                .OrderBy(x => x.FromDate)
                .Take(5)
                .Select(x =>
                {
                    var item = ToLeaveDashboardListItem(x);
                    item.Meta = $"{x.FromDate:dd-MMM} to {x.ToDate:dd-MMM}";
                    item.Badge = "Upcoming";
                    return item;
                })
                .ToList();

            model.CancellationRequests = applications
    .Where(x => x.CancellationStatus == "Pending")
                .OrderBy(x => x.CancellationRequestedOn)
                .Take(5)
                .Select(x =>
                {
                    var item = ToLeaveDashboardListItem(x);
                    item.Meta = x.CancellationRequestedOn?.ToString("dd-MMM-yyyy");
                    item.Badge = "Cancel";
                    return item;
                })
                .ToList();

            model.ExpiringCompOffCredits = compOffCredits
                .Where(x =>
                    x.Status == "Available" &&
                    x.ExpiryDate >= today &&
                    x.ExpiryDate <= compOffExpiryEnd &&
                    x.CreditDays > x.UsedDays)
                .OrderBy(x => x.ExpiryDate)
                .Take(5)
                .Select(x => new LeaveDashboardListItem
                {
                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee?.FullName ?? "-",
                    Subtitle = $"{x.CreditDays - x.UsedDays:0.##} day(s) available",
                    Meta = x.ExpiryDate.ToString("dd-MMM-yyyy"),
                    Badge = "Expiring"
                })
                .ToList();

            return View(model);
        }

        private static List<LeaveDashboardBucket> BuildLeaveBuckets(
    List<LeaveDashboardBucket> buckets)
        {
            decimal total = buckets.Sum(x => x.Days > 0 ? x.Days : x.Count);

            foreach (var bucket in buckets)
            {
                decimal value = bucket.Days > 0 ? bucket.Days : bucket.Count;

                bucket.Percent = total == 0
                    ? 0
                    : Math.Round(value * 100 / total, 2);
            }

            return buckets;
        }

        private static LeaveDashboardListItem ToLeaveDashboardListItem(
            LeaveApplicationModel application)
        {
            return new LeaveDashboardListItem
            {
                LeaveApplicationId = application.Id,
                EmployeeId = application.EmployeeId,
                EmployeeName = application.Employee?.FullName ?? "-",
                Subtitle = application.LeaveType?.LeaveName ?? "Leave",
                Meta = $"{application.FromDate:dd-MMM} to {application.ToDate:dd-MMM}",
                Badge = application.Status
            };
        }

        private static string GetLeaveStatusClass(string status)
        {
            return status switch
            {
                "Approved" => "bucket-success",
                "Pending" => "bucket-warning",
                "Rejected" => "bucket-danger",
                "Cancelled" => "bucket-neutral",
                _ => "bucket-info"
            };
        }

        private static bool IsLeaveOverlap(
            LeaveApplicationModel existingLeave,
            LeaveApplicationModel newLeave)
        {
            bool dateRangesOverlap =
                newLeave.FromDate.Date <= existingLeave.ToDate.Date &&
                newLeave.ToDate.Date >= existingLeave.FromDate.Date;

            if (!dateRangesOverlap)
            {
                return false;
            }

            if (!existingLeave.IsHalfDay || !newLeave.IsHalfDay)
            {
                return true;
            }

            bool sameDate =
                existingLeave.FromDate.Date == newLeave.FromDate.Date;

            if (!sameDate)
            {
                return true;
            }

            return string.Equals(
                existingLeave.HalfDaySession,
                newLeave.HalfDaySession,
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsValidHalfDaySession(string? halfDaySession)
        {
            return string.Equals(
                    halfDaySession,
                    "FirstHalf",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    halfDaySession,
                    "SecondHalf",
                    StringComparison.OrdinalIgnoreCase);
        }

        private async Task<decimal> CalculateLeaveDaysAsync(
            LeaveApplicationModel leaveApplication)
        {
            if (leaveApplication.FromDate.Date > leaveApplication.ToDate.Date)
            {
                return 0m;
            }

            if (leaveApplication.IsHalfDay)
            {
                return await _workingDayService.IsWorkingDayAsync(
                    leaveApplication.FromDate.Date)
                    ? 0.5m
                    : 0m;
            }

            return (await GetLeaveWorkingDatesAsync(leaveApplication)).Count;
        }

        private async Task<List<DateTime>> GetLeaveWorkingDatesAsync(
            LeaveApplicationModel leaveApplication)
        {
            var dates = new List<DateTime>();

            if (leaveApplication.FromDate.Date > leaveApplication.ToDate.Date)
            {
                return dates;
            }

            for (var date = leaveApplication.FromDate.Date;
                 date <= leaveApplication.ToDate.Date;
                 date = date.AddDays(1))
            {
                if (await _workingDayService.IsWorkingDayAsync(date))
                {
                    dates.Add(date);
                }
            }

            return dates;
        }

        private static decimal GetLeaveDayValue(
            LeaveApplicationModel leaveApplication,
            DateTime date)
        {
            if (leaveApplication.IsHalfDay &&
                leaveApplication.FromDate.Date == date.Date)
            {
                return 0.5m;
            }

            return 1m;
        }

        private static string BuildLeaveAttendanceRemarks(
            LeaveApplicationModel leaveApplication)
        {
            if (!leaveApplication.IsHalfDay)
            {
                return $"Leave approved: {leaveApplication.ApplicationNumber}";
            }

            string sessionText =
                string.Equals(
                    leaveApplication.HalfDaySession,
                    "FirstHalf",
                    StringComparison.OrdinalIgnoreCase)
                    ? "first half"
                    : "second half";

            return $"Half-day leave approved ({sessionText}): {leaveApplication.ApplicationNumber}";
        }

        [Authorize(Roles = "Admin,HR,Master")]
        public async Task<IActionResult> ExportApplications()
        {
            var applications = await _leaveApplicationRepository.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .OrderByDescending(x => x.AppliedOn)
                .ToListAsync();

            using var workbook = new XLWorkbook();

            var worksheet =
                workbook.Worksheets.Add("Leave Applications");

            worksheet.Cell("A1").Value = "Application No";
            worksheet.Cell("B1").Value = "Employee Code";
            worksheet.Cell("C1").Value = "Employee Name";
            worksheet.Cell("D1").Value = "Leave Type";
            worksheet.Cell("E1").Value = "From Date";
            worksheet.Cell("F1").Value = "To Date";
            worksheet.Cell("G1").Value = "Days";
            worksheet.Cell("H1").Value = "Status";
            worksheet.Cell("I1").Value = "Reason";
            worksheet.Cell("J1").Value = "Applied On";
            worksheet.Cell("K1").Value = "Approved By";
            worksheet.Cell("L1").Value = "Approved On";

            int row = 2;

            foreach (var item in applications)
            {
                worksheet.Cell(row, 1).Value = item.ApplicationNumber;
                worksheet.Cell(row, 2).Value = item.Employee?.EmployeeCode;
                worksheet.Cell(row, 3).Value = item.Employee?.FullName;
                worksheet.Cell(row, 4).Value = item.LeaveType?.LeaveName;
                worksheet.Cell(row, 5).Value = item.FromDate;
                worksheet.Cell(row, 6).Value = item.ToDate;
                worksheet.Cell(row, 7).Value = item.NumberOfDays;
                worksheet.Cell(row, 8).Value = item.Status;
                worksheet.Cell(row, 9).Value = item.Reason;
                worksheet.Cell(row, 10).Value = item.AppliedOn;
                worksheet.Cell(row, 11).Value = item.ApprovedBy;
                worksheet.Cell(row, 12).Value = item.ApprovedOn;

                row++;
            }

            var headerRange = worksheet.Range("A1:L1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

            worksheet.Column(5).Style.DateFormat.Format = "dd-MMM-yyyy";
            worksheet.Column(6).Style.DateFormat.Format = "dd-MMM-yyyy";
            worksheet.Column(10).Style.DateFormat.Format = "dd-MMM-yyyy HH:mm";
            worksheet.Column(12).Style.DateFormat.Format = "dd-MMM-yyyy HH:mm";

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"LeaveApplications_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }

        private bool IsAttendanceStatus(string? status, params string[] validStatuses)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return false;
            }

            return validStatuses.Any(x =>
                string.Equals(
                    status.Trim(),
                    x,
                    StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsBirthdayLeave(LeaveTypeModel leaveType)
        {
            return string.Equals(
                       leaveType.LeaveCode,
                       "BDL",
                       StringComparison.OrdinalIgnoreCase) ||
                   leaveType.LeaveName.Contains(
                       "Birthday",
                       StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsWithinBirthdayLeaveWindow(
            DateTime leaveDate,
            DateTime? dateOfBirth,
            int year)
        {
            if (!dateOfBirth.HasValue)
            {
                return false;
            }

            var possibleBirthdays = new[]
            {
        GetBirthdayForYear(dateOfBirth.Value, year - 1),
        GetBirthdayForYear(dateOfBirth.Value, year),
        GetBirthdayForYear(dateOfBirth.Value, year + 1)
    };

            return possibleBirthdays.Any(birthday =>
                leaveDate.Date >= birthday.AddDays(-3).Date &&
                leaveDate.Date <= birthday.AddDays(3).Date);
        }

        private static DateTime GetBirthdayForYear(
            DateTime dateOfBirth,
            int year)
        {
            if (dateOfBirth.Month == 2 &&
                dateOfBirth.Day == 29 &&
                !DateTime.IsLeapYear(year))
            {
                return new DateTime(year, 2, 28);
            }

            return new DateTime(
                year,
                dateOfBirth.Month,
                dateOfBirth.Day);
        }

        private async Task<BirthdayLeaveBalanceViewModel> GetBirthdayLeaveBalanceAsync(
        EmployeeModel employee,
        DateTime referenceDate,
        int? excludeLeaveApplicationId = null)
        {
            DateTime financialYearStart =
                GetFinancialYearStart(referenceDate);

            DateTime financialYearEnd =
                GetFinancialYearEnd(referenceDate);

            var birthdayLeaveQuery =
                _leaveApplicationRepository.LeaveApplications
                    .AsNoTracking()
                    .Include(x => x.LeaveType)
                    .Where(x =>
            x.EmployeeId == employee.Id &&
            x.Status == "Approved" &&
            x.FromDate.Date >= financialYearStart.Date &&
            x.FromDate.Date <= financialYearEnd.Date &&
            x.LeaveType != null &&
            x.LeaveType.LeaveCode == "BDL");

            if (excludeLeaveApplicationId.HasValue)
            {
                birthdayLeaveQuery =
                    birthdayLeaveQuery.Where(x =>
                        x.Id != excludeLeaveApplicationId.Value);
            }

            decimal used =
                await birthdayLeaveQuery
                    .SumAsync(x => (decimal?)x.PaidDays) ?? 0m;

            return new BirthdayLeaveBalanceViewModel
            {
                Entitlement = 1m,
                Used = Math.Min(used, 1m)
            };
        }

        private static DateTime GetFinancialYearStart(DateTime date)
        {
            return date.Month >= FinancialYearStartMonth
                ? new DateTime(date.Year, FinancialYearStartMonth, FinancialYearStartDay)
                : new DateTime(date.Year - 1, FinancialYearStartMonth, FinancialYearStartDay);
        }

        private static DateTime GetFinancialYearEnd(DateTime date)
        {
            return GetFinancialYearStart(date).AddYears(1).AddDays(-1);
        }

        private static int GetMonthsJoinedLate(DateTime joiningDate, DateTime financialYearStart)
        {
            if (joiningDate.Date <= financialYearStart.Date)
            {
                return 0;
            }

            int monthsLate =
                ((joiningDate.Year - financialYearStart.Year) * 12) +
                joiningDate.Month -
                financialYearStart.Month;

            if (joiningDate.Day > FinancialYearStartDay)
            {
                monthsLate += 1;
            }

            return Math.Clamp(monthsLate, 0, 12);
        }

        private static decimal GetProratedPaidLeaveEntitlement(
            DateTime joiningDate,
            DateTime financialYearStart)
        {
            return BuildPaidLeaveMonthlyCredits(
                    joiningDate,
                    financialYearStart)
                .Sum(x => x.Credit);
        }

        private static List<PaidLeaveMonthlyCreditViewModel> BuildPaidLeaveMonthlyCredits(
            DateTime joiningDate,
            DateTime financialYearStart)
        {
            var credits = new List<PaidLeaveMonthlyCreditViewModel>();

            for (int monthIndex = 0; monthIndex < 12; monthIndex++)
            {
                var monthStart = financialYearStart.Date.AddMonths(monthIndex);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                decimal credit;
                string remarks;

                if (joiningDate.Date > monthEnd)
                {
                    credit = 0m;
                    remarks = "Before joining";
                }
                else if (joiningDate.Year == monthStart.Year &&
                         joiningDate.Month == monthStart.Month)
                {
                    credit = joiningDate.Day > 15
                        ? 1m
                        : MonthlyPaidLeaveAccrual;

                    remarks = joiningDate.Day > 15
                        ? "Joined after 15th"
                        : "Joined on/before 15th";
                }
                else
                {
                    credit = MonthlyPaidLeaveAccrual;
                    remarks = "Full month";
                }

                credits.Add(new PaidLeaveMonthlyCreditViewModel
                {
                    MonthStart = monthStart,
                    Credit = credit,
                    Remarks = remarks
                });
            }

            return credits;
        }

        private async Task<PaidLeaveBalanceSnapshotViewModel> GetPaidLeaveBalanceSnapshotAsync(
            EmployeeModel employee,
            DateTime referenceDate,
            decimal requestedDays,
            int? excludeLeaveApplicationId = null)
        {
            DateTime fyStart = GetFinancialYearStart(referenceDate);
            DateTime fyEnd = GetFinancialYearEnd(referenceDate);

            var monthlyCredits =
                BuildPaidLeaveMonthlyCredits(
                    employee.JoiningDate,
                    fyStart);

            decimal proratedEntitlement =
                monthlyCredits.Sum(x => x.Credit);

            decimal carryForwardDays =
    await GetPaidLeaveCarryForwardDaysAsync(
        employee.Id,
        fyStart.Year);

            var approvedLeavesQuery = _leaveApplicationRepository.LeaveApplications
               .AsNoTracking()
               .Include(x => x.LeaveType)
               .Where(x =>
               x.EmployeeId == employee.Id &&
               x.Status == "Approved" &&
               x.FromDate.Date >= fyStart.Date &&
               x.FromDate.Date <= fyEnd.Date &&
               (
                   x.LeaveType == null ||
                   x.LeaveType.LeaveCode == null ||
                   (
                       x.LeaveType.LeaveCode != "COMP" &&
                       x.LeaveType.LeaveCode != "BDL"
                   )
               ));

            if (excludeLeaveApplicationId.HasValue)
            {
                approvedLeavesQuery = approvedLeavesQuery
                    .Where(x => x.Id != excludeLeaveApplicationId.Value);
            }

            decimal paidLeaveUsed =
                await approvedLeavesQuery
                    .SumAsync(x => (decimal?)x.PaidDays) ?? 0m;

            var pendingLeavesQuery = _leaveApplicationRepository.LeaveApplications
                .AsNoTracking()
                .Include(x => x.LeaveType)
                .Where(x =>
                    x.EmployeeId == employee.Id &&
                    x.Status == "Pending" &&
                    x.FromDate.Date >= fyStart.Date &&
                    x.FromDate.Date <= fyEnd.Date &&
                    (
                        x.LeaveType == null ||
                        x.LeaveType.LeaveCode == null ||
                        (
                            x.LeaveType.LeaveCode != "COMP" &&
                            x.LeaveType.LeaveCode != "BDL"
                        )
                    ));

            if (excludeLeaveApplicationId.HasValue)
            {
                pendingLeavesQuery = pendingLeavesQuery
                    .Where(x => x.Id != excludeLeaveApplicationId.Value);
            }

            decimal pendingRequestedDays =
                await pendingLeavesQuery
                    .SumAsync(x => (decimal?)x.NumberOfDays) ?? 0m;

            decimal balanceBeforePending =
    Math.Max(0, carryForwardDays + proratedEntitlement - paidLeaveUsed);

            decimal pendingPaidLeaveReserved =
                Math.Min(pendingRequestedDays, balanceBeforePending);

            decimal paidLeaveBalance =
                Math.Max(0, balanceBeforePending - pendingPaidLeaveReserved);

            decimal paidDaysForRequest =
                Math.Min(requestedDays, paidLeaveBalance);

            decimal unpaidDaysForRequest =
                Math.Max(0, requestedDays - paidDaysForRequest);

            return new PaidLeaveBalanceSnapshotViewModel
            {
                FinancialYearStart = fyStart,
                FinancialYearEnd = fyEnd,
                AnnualEntitlement = AnnualPaidLeaveEntitlement,
                MonthlyAccrual = MonthlyPaidLeaveAccrual,
                ProratedEntitlement = proratedEntitlement,
                CarryForwardDays = carryForwardDays,
                PaidLeaveUsed = paidLeaveUsed,
                PendingPaidLeaveReserved = pendingPaidLeaveReserved,
                PaidLeaveBalance = paidLeaveBalance,
                MonthlyCredits = monthlyCredits,
                RequestedDays = requestedDays,
                PaidDaysForRequest = paidDaysForRequest,
                UnpaidDaysForRequest = unpaidDaysForRequest,

                BirthdayLeave = await GetBirthdayLeaveBalanceAsync(
                      employee,
                      referenceDate,
                      excludeLeaveApplicationId)
            };
        }

        private async Task<decimal> GetPaidLeaveCarryForwardDaysAsync(
    int employeeId,
    int leaveYear)
        {
            return await _leaveBalanceRepository.LeaveBalances
                .AsNoTracking()
                .Include(x => x.LeaveType)
                .Where(x =>
                    x.EmployeeId == employeeId &&
                    x.LeaveYear == leaveYear &&
                    x.LeaveType.IsPaidLeave &&
                    x.LeaveType.LeaveCode != "COMP" &&
                    x.LeaveType.LeaveCode != "BDL")
                .SumAsync(x => (decimal?)x.CarryForwardDays) ?? 0m;
        }

        private async Task<List<LeaveApplicationDayModel>> EnsureLeaveApplicationDaysAsync(
            LeaveApplicationModel leaveApplication)
        {
            var leaveDays =
                await _leaveApplicationDayRepository.LeaveApplicationDays
                    .Where(x => x.LeaveApplicationId == leaveApplication.Id)
                    .OrderBy(x => x.LeaveDate)
                    .ToListAsync();

            if (leaveDays.Any() ||
                leaveApplication.Status != "Approved")
            {
                return leaveDays;
            }

            await RebuildLeaveApplicationDaysAsync(leaveApplication);

            return await _leaveApplicationDayRepository.LeaveApplicationDays
                .Where(x => x.LeaveApplicationId == leaveApplication.Id)
                .OrderBy(x => x.LeaveDate)
                .ToListAsync();
        }

        private async Task<bool> ReverseCompOffUsageAsync(
            int leaveApplicationId,
            decimal daysToReverse)
        {
            if (daysToReverse <= 0)
            {
                return true;
            }

            var usages =
                await _compOffUsageRepository.CompOffUsages
                    .Include(x => x.CompOffCredit)
                    .Where(x =>
                        x.LeaveApplicationId == leaveApplicationId &&
                        !x.IsReversed &&
                        x.UsedDays > 0)
                    .OrderByDescending(x => x.UsedOn)
                    .ThenByDescending(x => x.Id)
                    .ToListAsync();

            if (!usages.Any())
            {
                return false;
            }

            decimal remainingDays = daysToReverse;

            foreach (var usage in usages)
            {
                if (remainingDays <= 0)
                {
                    break;
                }

                var credit = usage.CompOffCredit;

                if (credit == null)
                {
                    return false;
                }

                decimal reverseDays =
                    Math.Min(usage.UsedDays, remainingDays);

                credit.UsedDays -= reverseDays;

                if (credit.UsedDays < 0)
                {
                    credit.UsedDays = 0;
                }

                credit.Status =
                    credit.ExpiryDate.Date < DateTime.Today
                        ? "Expired"
                        : "Available";

                credit.UpdatedOn = DateTime.Now;

                usage.UsedDays -= reverseDays;

                if (usage.UsedDays <= 0)
                {
                    usage.UsedDays = 0;
                    usage.IsReversed = true;
                    usage.ReversedOn = DateTime.Now;
                    usage.ReversedBy = User.Identity?.Name;
                }

                await _compOffCreditRepository.UpdateAsync(credit);
                await _compOffUsageRepository.UpdateAsync(usage);

                remainingDays -= reverseDays;
            }

            return remainingDays <= 0;
        }

        private async Task RebuildLeaveApplicationDaysAsync(LeaveApplicationModel leaveApplication)
        {
            var existingDays = await _leaveApplicationDayRepository.LeaveApplicationDays
                .Where(x => x.LeaveApplicationId == leaveApplication.Id)
                .ToListAsync();

            if (existingDays.Any())
            {
                await _leaveApplicationDayRepository.DeleteRangeAsync(existingDays);
            }

            var workingDates =
                await GetLeaveWorkingDatesAsync(leaveApplication);

            decimal remainingPaidDays = leaveApplication.PaidDays;

            var leaveDays = new List<LeaveApplicationDayModel>();

            foreach (var date in workingDates)
            {
                decimal dayValue =
                    GetLeaveDayValue(leaveApplication, date);

                decimal paidDays = Math.Min(dayValue, remainingPaidDays);
                decimal unpaidDays = dayValue - paidDays;

                leaveDays.Add(new LeaveApplicationDayModel
                {
                    LeaveApplicationId = leaveApplication.Id,
                    LeaveDate = date,
                    DayValue = dayValue,
                    PaidDays = paidDays,
                    UnpaidDays = unpaidDays,
                    Status = "Active",
                    HalfDaySession = leaveApplication.IsHalfDay ? leaveApplication.HalfDaySession : null
                });

                remainingPaidDays -= paidDays;
            }

            if (leaveDays.Any())
            {
                await _leaveApplicationDayRepository.AddRangeAsync(leaveDays);
            }

            await _leaveApplicationDayRepository.SaveAsync();
        }

        #endregion Actions
    }
}