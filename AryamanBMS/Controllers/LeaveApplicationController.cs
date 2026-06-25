using AryamanBMS.Extensions;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.ViewModels;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR,Employee")]
    public class LeaveApplicationController : Controller
    {
        private readonly ILeaveApplicationRepository _leaveApplicationRepository;
        private readonly ILeaveTypeRepository _leaveTypeRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly ILeaveBalanceRepository _leaveBalanceRepository;
        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly ICompOffCreditRepository _compOffCreditRepository;
        private readonly ICompOffUsageRepository _compOffUsageRepository;

        public LeaveApplicationController(
              ILeaveApplicationRepository leaveApplicationRepository,
              ILeaveTypeRepository leaveTypeRepository,
              IEmployeeRepository employeeRepository,
              IAttendanceRepository attendanceRepository,
              ILeaveBalanceRepository leaveBalanceRepository,
              UserManager<ApplicationUserModel> userManager,
              ICompOffCreditRepository compOffCreditRepository,
              ICompOffUsageRepository compOffUsageRepository)
        {
            _leaveApplicationRepository = leaveApplicationRepository;
            _leaveTypeRepository = leaveTypeRepository;
            _employeeRepository = employeeRepository;
            _attendanceRepository = attendanceRepository;
            _leaveBalanceRepository = leaveBalanceRepository;
            _userManager = userManager;
            _compOffCreditRepository = compOffCreditRepository;
            _compOffUsageRepository = compOffUsageRepository;
        }

        public async Task<IActionResult> Index(
           string? searchText,
           string status = "All",
           int page = 1)
        {
            const int pageSize = 10;

            var query = _leaveApplicationRepository.LeaveApplications
                .AsNoTracking()
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .AsQueryable();

            bool isEmployeeOnly =
                User.IsInRole("Employee") &&
                !User.IsInRole("Admin") &&
                !User.IsInRole("HR");

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

            query = query
                .OrderByDescending(x => x.AppliedOn)
                .ThenByDescending(x => x.Id);

            var routeValues = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                routeValues["searchText"] = searchText;
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                routeValues["status"] = status;
            }

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

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            LeaveApplicationModel leaveApplication)
        {
            var user = await _userManager.GetUserAsync(User);

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

            if (leaveApplication.FromDate > leaveApplication.ToDate)
            {
                ModelState.AddModelError(
                    "ToDate",
                    "To Date cannot be earlier than From Date.");
            }

            leaveApplication.NumberOfDays =
                (leaveApplication.ToDate.Date -
                 leaveApplication.FromDate.Date).Days + 1;

            if (leaveApplication.NumberOfDays <= 0)
            {
                ModelState.AddModelError(
                    "",
                    "Number of leave days must be greater than zero.");

                var leaveType = await _leaveTypeRepository.LeaveTypes
                      .FirstOrDefaultAsync(x =>
                      x.Id == leaveApplication.LeaveTypeId);
            }

            bool overlappingLeaveExists =
               await _leaveApplicationRepository.LeaveApplications
               .AnyAsync(x =>
                   x.EmployeeId == leaveApplication.EmployeeId &&
                   x.Status != "Rejected" &&
                   x.Status != "Cancelled" &&
                   leaveApplication.FromDate <= x.ToDate &&
                   leaveApplication.ToDate >= x.FromDate);

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
            ModelState.Remove("ApprovedBy");
            ModelState.Remove("ApprovedOn");
            ModelState.Remove("ApprovalRemarks");

            if (ModelState.IsValid)
            {
                leaveApplication.ApplicationNumber = GenerateApplicationNumber();

                leaveApplication.AppliedOn = DateTime.Now;

                leaveApplication.Status = "Pending";

                await _leaveApplicationRepository.AddAsync(leaveApplication);

                await _leaveApplicationRepository.SaveAsync();

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

            if (User.IsInRole("Employee") && !User.IsInRole("Admin") &&
                     !User.IsInRole("HR"))
            {
                var user = await _userManager.GetUserAsync(User);

                var employee = await _employeeRepository.Employees
                    .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);

                if (employee == null || leaveApplication.EmployeeId != employee.Id)
                {
                    return Forbid();
                }
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

            var leaveType = await _leaveTypeRepository.LeaveTypes
                .FirstOrDefaultAsync(x => x.Id == leaveApplication.LeaveTypeId);

            if (leaveType == null)
            {
                TempData["Error"] =
                    "Leave type not found.";

                return RedirectToAction(nameof(Index));
            }

            bool isCompOff =
               string.Equals(
              leaveType.LeaveCode,
              "COMP",
              StringComparison.OrdinalIgnoreCase);

            bool requiresBalance =
                !isCompOff &&
                leaveType.IsPaidLeave &&
                leaveType.DaysPerYear > 0;

            LeaveBalanceModel? leaveBalance = null;



            var compOffAllocations =
                new List<(CompOffCreditModel Credit, decimal DaysToUse)>();

            if (isCompOff)
            {
                var availableCredits =
                    await _compOffCreditRepository.CompOffCredits
                        .Where(x =>
                            x.EmployeeId == leaveApplication.EmployeeId &&
                            x.Status == "Available" &&
                            x.ExpiryDate.Date >=
                                leaveApplication.ToDate.Date &&
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
                        Math.Min(
                            remainingCreditDays,
                            remainingRequiredDays);

                    compOffAllocations.Add(
                        (credit, daysToUse));

                    remainingRequiredDays -= daysToUse;
                }

                if (remainingRequiredDays > 0)
                {
                    TempData["Error"] =
                        "Insufficient valid Comp Off credit to approve this leave.";

                    return RedirectToAction(nameof(Index));
                }
            }
            else if (requiresBalance)
            {
                leaveBalance =
                    await _leaveBalanceRepository.LeaveBalances
                        .FirstOrDefaultAsync(x =>
                            x.EmployeeId ==
                                leaveApplication.EmployeeId &&
                            x.LeaveTypeId ==
                                leaveApplication.LeaveTypeId &&
                            x.LeaveYear ==
                                leaveApplication.FromDate.Year);

                if (leaveBalance == null)
                {
                    TempData["Error"] =
                        "Leave balance not found for this employee and leave type.";

                    return RedirectToAction(nameof(Index));
                }

                if (leaveBalance.BalanceDays <
                    leaveApplication.NumberOfDays)
                {
                    TempData["Error"] =
                        "Insufficient leave balance.";

                    return RedirectToAction(nameof(Index));
                }
            }

            bool attendanceConflict =
                await _attendanceRepository.Attendances
                .AnyAsync(a =>
                    a.EmployeeId == leaveApplication.EmployeeId &&
                    a.AttendanceDate.Date >= leaveApplication.FromDate.Date &&
                    a.AttendanceDate.Date <= leaveApplication.ToDate.Date &&
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

            leaveApplication.Status = "Approved";
            leaveApplication.ApprovedOn = DateTime.Now;
            leaveApplication.ApprovedBy = User.Identity?.Name;

            if (leaveBalance != null)
            {
                leaveBalance.UsedDays += leaveApplication.NumberOfDays;
                leaveBalance.BalanceDays -= leaveApplication.NumberOfDays;
            }

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

                    await _compOffCreditRepository
                        .UpdateAsync(credit);

                    var usage =
                        new CompOffUsageModel
                        {
                            CompOffCreditId = credit.Id,
                            LeaveApplicationId = leaveApplication.Id,
                            UsedDays = allocation.DaysToUse,
                            UsedOn = DateTime.Now,
                            IsReversed = false
                        };

                    await _compOffUsageRepository
                        .AddAsync(usage);
                }
            }

            for (var date = leaveApplication.FromDate.Date;
                 date <= leaveApplication.ToDate.Date;
                 date = date.AddDays(1))
            {
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
                        Remarks =
                            $"Leave approved: {leaveApplication.ApplicationNumber}",
                        CreatedOn = DateTime.Now
                    };

                    await _attendanceRepository.AddAsync(attendance);
                }
                else if (IsAttendanceStatus(existingAttendance.Status, "A", "Absent"))
                {
                    existingAttendance.Status = "L";
                    existingAttendance.Remarks =
                        $"Absent converted to leave: {leaveApplication.ApplicationNumber}";

                    await _attendanceRepository.UpdateAsync(existingAttendance);
                }
                else if (IsAttendanceStatus(existingAttendance.Status, "L", "Leave"))
                {
                    existingAttendance.Remarks =
                        $"Leave approved: {leaveApplication.ApplicationNumber}";

                    await _attendanceRepository.UpdateAsync(existingAttendance);
                }
            }

            await _leaveApplicationRepository.UpdateAsync(leaveApplication);

            if (leaveBalance != null)
            {
                await _leaveBalanceRepository.UpdateAsync(leaveBalance);
            }

            await _leaveApplicationRepository.SaveAsync();

            if (leaveBalance != null)
            {
                await _leaveBalanceRepository.SaveAsync();
            }


            if (isCompOff)
            {
                await _compOffCreditRepository.SaveAsync();
                await _compOffUsageRepository.SaveAsync();
            }

            await _attendanceRepository.SaveAsync();

            TempData["Success"] = isCompOff ? "Comp Off leave approved, credits consumed and attendance marked."
        : requiresBalance
            ? "Leave application approved, balance updated and attendance marked."
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

            leaveApplication.Status = "Rejected";
            leaveApplication.ApprovedOn = DateTime.Now;
            leaveApplication.ApprovedBy = User.Identity?.Name;

            await _leaveApplicationRepository.UpdateAsync(leaveApplication);
            await _leaveApplicationRepository.SaveAsync();

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
        [Authorize(Roles = "Admin,HR,Employee")]
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

            if (User.IsInRole("Employee") && !User.IsInRole("Admin") &&
                      !User.IsInRole("HR"))
            {
                var user = await _userManager.GetUserAsync(User);

                var employee = await _employeeRepository.Employees
                    .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);

                if (employee == null || leaveApplication.EmployeeId != employee.Id)
                {
                    return Forbid();
                }
            }


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
        [Authorize(Roles = "Admin,HR,Employee")]
        public async Task<IActionResult> RequestCancellation(
    int id,
    string cancellationReason)
        {
            var leaveApplication =
                await _leaveApplicationRepository.GetByIdAsync(id);

            if (leaveApplication == null)
            {
                return NotFound();
            }

            // Employee can request cancellation only for their own leave
            if (!User.IsInRole("Admin") &&
                !User.IsInRole("HR"))
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

            if (leaveApplication.CancellationStatus == "Pending")
            {
                TempData["Error"] =
                    "A cancellation request is already pending.";

                return RedirectToAction(nameof(Index));
            }

            if (leaveApplication.CancellationStatus == "Approved")
            {
                TempData["Error"] =
                    "This leave application has already been cancelled.";

                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(cancellationReason))
            {
                TempData["Error"] =
                    "Cancellation reason is required.";

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

            await _leaveApplicationRepository
                .UpdateAsync(leaveApplication);

            await _leaveApplicationRepository.SaveAsync();

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

            bool requiresBalance =
                !isCompOff &&
                leaveType.IsPaidLeave &&
                leaveType.DaysPerYear > 0;

            LeaveBalanceModel? leaveBalance = null;

            if (requiresBalance)
            {
                leaveBalance =
                    await _leaveBalanceRepository.LeaveBalances
                        .FirstOrDefaultAsync(x =>
                            x.EmployeeId ==
                                leaveApplication.EmployeeId &&
                            x.LeaveTypeId ==
                                leaveApplication.LeaveTypeId &&
                            x.LeaveYear ==
                                leaveApplication.FromDate.Year);

                if (leaveBalance == null)
                {
                    TempData["Error"] =
                        "Leave balance record not found.";

                    return RedirectToAction(nameof(Index));
                }

                leaveBalance.UsedDays -=
                    leaveApplication.NumberOfDays;

                leaveBalance.BalanceDays +=
                    leaveApplication.NumberOfDays;

                if (leaveBalance.UsedDays < 0)
                {
                    leaveBalance.UsedDays = 0;
                }

                if (leaveBalance.BalanceDays >
                    leaveBalance.AllocatedDays)
                {
                    leaveBalance.BalanceDays =
                        leaveBalance.AllocatedDays;
                }
            }

            if (isCompOff)
            {
                var usages =
                    await _compOffUsageRepository.CompOffUsages
                        .Include(x => x.CompOffCredit)
                        .Where(x =>
                            x.LeaveApplicationId == leaveApplication.Id &&
                            !x.IsReversed)
                        .ToListAsync();

                if (!usages.Any())
                {
                    TempData["Error"] =
                        "Comp Off usage records were not found.";

                    return RedirectToAction(nameof(Index));
                }

                foreach (var usage in usages)
                {
                    var credit = usage.CompOffCredit;

                    if (credit == null)
                    {
                        TempData["Error"] =
                            "A linked Comp Off credit was not found.";

                        return RedirectToAction(nameof(Index));
                    }

                    credit.UsedDays -= usage.UsedDays;

                    if (credit.UsedDays < 0)
                    {
                        credit.UsedDays = 0;
                    }

                    credit.Status =
                        credit.ExpiryDate.Date < DateTime.Today
                            ? "Expired"
                            : "Available";

                    credit.UpdatedOn = DateTime.Now;

                    usage.IsReversed = true;
                    usage.ReversedOn = DateTime.Now;
                    usage.ReversedBy = User.Identity?.Name;

                    await _compOffCreditRepository.UpdateAsync(credit);
                    await _compOffUsageRepository.UpdateAsync(usage);
                }
            }

            var attendanceRecords =
                await _attendanceRepository.Attendances
                    .Where(x =>
                        x.EmployeeId ==
                            leaveApplication.EmployeeId &&
                        x.AttendanceDate.Date >=
                            leaveApplication.FromDate.Date &&
                        x.AttendanceDate.Date <=
                            leaveApplication.ToDate.Date &&
                        x.Status == "L" &&
                        x.Remarks != null &&
                        x.Remarks.Contains(
                            leaveApplication.ApplicationNumber))
                    .ToListAsync();

            foreach (var attendance in attendanceRecords)
            {
                await _attendanceRepository.DeleteAsync(attendance);
            }

            leaveApplication.Status = "Cancelled";
            leaveApplication.CancellationStatus = "Approved";
            leaveApplication.CancellationReviewedOn =
                DateTime.Now;
            leaveApplication.CancellationReviewedBy =
                User.Identity?.Name;
            leaveApplication.CancellationRemarks =
                string.IsNullOrWhiteSpace(cancellationRemarks)
                    ? null
                    : cancellationRemarks.Trim();

            await _leaveApplicationRepository
                .UpdateAsync(leaveApplication);

            if (leaveBalance != null)
            {
                await _leaveBalanceRepository
                    .UpdateAsync(leaveBalance);
            }

            if (isCompOff)
            {
                await _compOffCreditRepository.SaveAsync();
                await _compOffUsageRepository.SaveAsync();
            }

            await _leaveApplicationRepository.SaveAsync();

            if (leaveBalance != null)
            {
                await _leaveBalanceRepository.SaveAsync();
            }

            await _attendanceRepository.SaveAsync();

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

            await _leaveApplicationRepository
                .UpdateAsync(leaveApplication);

            await _leaveApplicationRepository.SaveAsync();

            TempData["Success"] =
                "Leave cancellation request rejected.";

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,HR")]
        public IActionResult Dashboard()
        {
            var applications =
                _leaveApplicationRepository.LeaveApplications;

            var model = new LeaveDashboardViewModel
            {
                TotalApplications = applications.Count(),

                PendingApplications =
                    applications.Count(x => x.Status == "Pending"),

                ApprovedApplications =
                    applications.Count(x => x.Status == "Approved"),

                RejectedApplications =
                    applications.Count(x => x.Status == "Rejected"),

                CancelledApplications =
                    applications.Count(x => x.Status == "Cancelled")
            };

            return View(model);
        }

        [Authorize(Roles = "Admin,HR")]
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

        private bool IsAttendanceStatus(
    string? status,
    params string[] validStatuses)
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
    }

}