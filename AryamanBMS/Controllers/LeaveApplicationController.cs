using AryamanBMS.Extensions;
using AryamanBMS.Models;
using AryamanBMS.Repositories;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using ClosedXML.Excel;

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

        public LeaveApplicationController(
              ILeaveApplicationRepository leaveApplicationRepository,
              ILeaveTypeRepository leaveTypeRepository,
              IEmployeeRepository employeeRepository,
              IAttendanceRepository attendanceRepository,
              ILeaveBalanceRepository leaveBalanceRepository,
              UserManager<ApplicationUserModel> userManager)
        {
            _leaveApplicationRepository = leaveApplicationRepository;
            _leaveTypeRepository = leaveTypeRepository;
            _employeeRepository = employeeRepository;
            _attendanceRepository = attendanceRepository;
            _leaveBalanceRepository = leaveBalanceRepository;
            _userManager = userManager;
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

            bool leaveTypeActive =  await _leaveTypeRepository.LeaveTypes
                    .AnyAsync(x =>
                        x.Id == leaveApplication.LeaveTypeId &&
                        x.IsActive);
                    
                     if (!leaveTypeActive)
                     {
                         ModelState.AddModelError(
                             "LeaveTypeId",
                             "Selected leave type is inactive.");
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
                leaveApplication.ApplicationNumber =  GenerateApplicationNumber();

                leaveApplication.AppliedOn =  DateTime.Now;

                leaveApplication.Status ="Pending";

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

            bool requiresBalance =
                leaveType.IsPaidLeave &&
                leaveType.DaysPerYear > 0;

            LeaveBalanceModel? leaveBalance = null;

            if (requiresBalance)
            {
                leaveBalance = await _leaveBalanceRepository.LeaveBalances
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == leaveApplication.EmployeeId &&
                        x.LeaveTypeId == leaveApplication.LeaveTypeId &&
                        x.LeaveYear == leaveApplication.FromDate.Year);

                if (leaveBalance == null)
                {
                    TempData["Error"] =
                        "Leave balance not found for this employee and leave type.";

                    return RedirectToAction(nameof(Index));
                }

                if (leaveBalance.BalanceDays < leaveApplication.NumberOfDays)
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

            await _attendanceRepository.SaveAsync();

            TempData["Success"] =
                requiresBalance
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