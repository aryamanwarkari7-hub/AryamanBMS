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

        public async Task<IActionResult> Index()
        {
            var query = _leaveApplicationRepository.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .AsQueryable();

            if (User.IsInRole("Employee") &&
                !User.IsInRole("Admin") &&
                !User.IsInRole("HR"))
            {
                var user = await _userManager.GetUserAsync(User);

                var employee = await _employeeRepository.Employees
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == user.Id);

                if (employee == null)
                {
                    TempData["Error"] =
                        "No employee record mapped to this user.";

                    return View(new List<LeaveApplicationModel>());
                }

                query = query.Where(x => x.EmployeeId == employee.Id);
            }

            var leaveApplications = await query
                .OrderByDescending(x => x.AppliedOn)
                .ToListAsync();

            return View(leaveApplications);
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

            var leaveBalance =
                  await _leaveBalanceRepository.LeaveBalances
                  .FirstOrDefaultAsync(x =>
                  x.EmployeeId == leaveApplication.EmployeeId &&
                  x.LeaveTypeId == leaveApplication.LeaveTypeId &&
                  x.LeaveYear == leaveApplication.FromDate.Year);

            if (leaveBalance == null)
            {
                ModelState.AddModelError(
                    "",
                    "Leave balance not found. Please contact HR.");
            }
            else if (leaveBalance.BalanceDays <
                     leaveApplication.NumberOfDays)
            {
                ModelState.AddModelError(
                    "",
                    $"Insufficient leave balance. Available balance: {leaveBalance.BalanceDays} day(s).");
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

            return View(leaveApplication);
        }

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

            var leaveBalance =
                await _leaveBalanceRepository.LeaveBalances
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

            leaveApplication.Status = "Approved";
            leaveApplication.ApprovedOn = DateTime.Now;
            leaveApplication.ApprovedBy = User.Identity?.Name;

            leaveBalance.UsedDays += leaveApplication.NumberOfDays;
            leaveBalance.BalanceDays -= leaveApplication.NumberOfDays;

            DateTime currentDate = leaveApplication.FromDate.Date;

            while (currentDate <= leaveApplication.ToDate.Date)
            {
                bool attendanceExists =
                    await _attendanceRepository.Attendances
                    .AnyAsync(a =>
                        a.EmployeeId == leaveApplication.EmployeeId &&
                        a.AttendanceDate.Date == currentDate);

                if (!attendanceExists)
                {
                    var attendance = new AttendanceModel
                    {
                        EmployeeId = leaveApplication.EmployeeId,
                        AttendanceDate = currentDate,
                        Status = "L",
                        Remarks =
                            $"Leave approved: {leaveApplication.ApplicationNumber}",
                        CreatedOn = DateTime.Now
                    };

                    await _attendanceRepository.AddAsync(attendance);
                }

                currentDate = currentDate.AddDays(1);
            }

            await _leaveApplicationRepository.UpdateAsync(leaveApplication);
            await _leaveBalanceRepository.UpdateAsync(leaveBalance);

            await _leaveApplicationRepository.SaveAsync();
            await _leaveBalanceRepository.SaveAsync();
            await _attendanceRepository.SaveAsync();

            TempData["Success"] =
                "Leave application approved, balance updated and attendance marked.";

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,HR")]
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

        [Authorize(Roles = "Admin,HR,Employee")]
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
    }
}