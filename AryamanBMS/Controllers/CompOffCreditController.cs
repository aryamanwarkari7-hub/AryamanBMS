using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR,Employee")]
    public class CompOffCreditController : Controller
    {
        private readonly ICompOffCreditRepository
            _compOffCreditRepository;

        private readonly IAttendanceRepository
            _attendanceRepository;

        private readonly IEmployeeRepository
            _employeeRepository;

        private readonly UserManager<ApplicationUserModel>
            _userManager;

        public CompOffCreditController(
            ICompOffCreditRepository compOffCreditRepository,
            IAttendanceRepository attendanceRepository,
            IEmployeeRepository employeeRepository,
            UserManager<ApplicationUserModel> userManager)
        {
            _compOffCreditRepository =
                compOffCreditRepository;

            _attendanceRepository =
                attendanceRepository;

            _employeeRepository =
                employeeRepository;

            _userManager =
                userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Request()
        {
            await UpdateExpiredCreditsAsync();

            var model = new CompOffRequestViewModel
            {
                WorkedDate = DateTime.Today,
                CreditDays = 1.0m
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            await UpdateExpiredCreditsAsync();

            var query =
                _compOffCreditRepository.CompOffCredits
                    .AsNoTracking()
                    .Include(x => x.Employee)
                    .Include(x => x.Attendance)
                    .AsQueryable();

            if (!User.IsInRole("Admin") &&
                !User.IsInRole("HR"))
            {
                var user =
                    await _userManager.GetUserAsync(User);

                if (user == null)
                {
                    return Challenge();
                }

                var employee =
                    await _employeeRepository.Employees
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.ApplicationUserId == user.Id);

                if (employee == null)
                {
                    TempData["Error"] =
                        "No employee record is mapped to this user.";

                    return View(
                        new List<CompOffCreditModel>());
                }

                query = query.Where(x =>
                    x.EmployeeId == employee.Id);
            }

            var requests =
                await query
                    .OrderByDescending(x => x.RequestedOn)
                    .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Request(
            CompOffRequestViewModel model)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var employee =
                await _employeeRepository.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == user.Id);

            if (employee == null)
            {
                ModelState.AddModelError(
                    "",
                    "No employee record is mapped to this user.");
            }

            if (model.WorkedDate.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.WorkedDate),
                    "Worked date cannot be in the future.");
            }

            if (model.CreditDays != 0.5m &&
                model.CreditDays != 1.0m)
            {
                ModelState.AddModelError(
                    nameof(model.CreditDays),
                    "Comp Off credit must be either 0.5 or 1 day.");
            }

            // Current policy: request must be raised
            // within 60 days of the worked date.
            if (model.WorkedDate.Date <
                DateTime.Today.AddDays(-60))
            {
                ModelState.AddModelError(
                    nameof(model.WorkedDate),
                    "Comp Off must be requested within 60 days of the worked date.");
            }

            AttendanceModel? attendance = null;

            if (employee != null)
            {
                attendance =
                    await _attendanceRepository.Attendances
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.EmployeeId == employee.Id &&
                            x.AttendanceDate.Date ==
                                model.WorkedDate.Date);

                if (attendance == null)
                {
                    ModelState.AddModelError(
                        nameof(model.WorkedDate),
                        "Attendance was not found for the selected worked date.");
                }
                else if (!IsWorkingAttendanceStatus(
                             attendance.Status))
                {
                    ModelState.AddModelError(
                        nameof(model.WorkedDate),
                        "Comp Off can be requested only for Present or On Duty attendance.");
                }

                bool duplicateExists =
                    await _compOffCreditRepository
                        .CompOffCredits
                        .AnyAsync(x =>
                            x.EmployeeId == employee.Id &&
                            x.WorkedDate.Date ==
                                model.WorkedDate.Date);

                if (duplicateExists)
                {
                    ModelState.AddModelError(
                        nameof(model.WorkedDate),
                        "A Comp Off request already exists for this worked date.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var compOffCredit =
                new CompOffCreditModel
                {
                    EmployeeId = employee!.Id,
                    AttendanceId = attendance!.Id,

                    WorkedDate =
                        model.WorkedDate.Date,

                    CreditDays =
                        model.CreditDays,

                    ExpiryDate =
                        model.WorkedDate.Date.AddDays(60),

                    Status = "Pending",

                    RequestedOn =
                        DateTime.Now,

                    RequestedBy =
                        User.Identity?.Name,

                    Remarks =
                        model.Remarks.Trim(),

                    CreatedOn =
                        DateTime.Now
                };

            await _compOffCreditRepository
                .AddAsync(compOffCredit);

            await _compOffCreditRepository
                .SaveAsync();

            TempData["Success"] =
                "Comp Off request submitted successfully for HR approval.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Approve(int id)
        {
            var compOffCredit =
                await _compOffCreditRepository.GetByIdAsync(id);

            if (compOffCredit == null)
            {
                return NotFound();
            }

            if (compOffCredit.Status != "Pending")
            {
                TempData["Error"] =
                    "Only pending Comp Off requests can be approved.";

                return RedirectToAction(nameof(Index));
            }

            if (compOffCredit.ExpiryDate.Date < DateTime.Today)
            {
                compOffCredit.Status = "Expired";
                compOffCredit.UpdatedOn = DateTime.Now;

                await _compOffCreditRepository
                    .UpdateAsync(compOffCredit);

                await _compOffCreditRepository.SaveAsync();

                TempData["Error"] =
                    "This Comp Off request has already expired.";

                return RedirectToAction(nameof(Index));
            }

            compOffCredit.Status = "Available";
            compOffCredit.ApprovedOn = DateTime.Now;
            compOffCredit.ApprovedBy = User.Identity?.Name;
            compOffCredit.RejectedOn = null;
            compOffCredit.RejectedBy = null;
            compOffCredit.UpdatedOn = DateTime.Now;

            await _compOffCreditRepository
                .UpdateAsync(compOffCredit);

            await _compOffCreditRepository.SaveAsync();

            TempData["Success"] =
                "Comp Off request approved successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Reject(int id)
        {
            var compOffCredit =
                await _compOffCreditRepository.GetByIdAsync(id);

            if (compOffCredit == null)
            {
                return NotFound();
            }

            if (compOffCredit.Status != "Pending")
            {
                TempData["Error"] =
                    "Only pending Comp Off requests can be rejected.";

                return RedirectToAction(nameof(Index));
            }

            compOffCredit.Status = "Rejected";
            compOffCredit.RejectedOn = DateTime.Now;
            compOffCredit.RejectedBy = User.Identity?.Name;
            compOffCredit.ApprovedOn = null;
            compOffCredit.ApprovedBy = null;
            compOffCredit.UpdatedOn = DateTime.Now;

            await _compOffCreditRepository
                .UpdateAsync(compOffCredit);

            await _compOffCreditRepository.SaveAsync();

            TempData["Success"] =
                "Comp Off request rejected successfully.";

            return RedirectToAction(nameof(Index));
        }

        private static bool IsWorkingAttendanceStatus(
            string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return false;
            }

            return status.Equals(
                       "P",
                       StringComparison.OrdinalIgnoreCase) ||

                   status.Equals(
                       "Present",
                       StringComparison.OrdinalIgnoreCase) ||

                   status.Equals(
                       "OD",
                       StringComparison.OrdinalIgnoreCase) ||

                   status.Equals(
                       "On Duty",
                       StringComparison.OrdinalIgnoreCase) ||

                   status.Equals(
                       "OnDuty",
                       StringComparison.OrdinalIgnoreCase);
        }

        private async Task UpdateExpiredCreditsAsync()
        {
            var today = DateTime.Today;

            var expiredCredits =
                await _compOffCreditRepository.CompOffCredits
                    .Where(x =>
                        x.ExpiryDate.Date < today &&
                        (
                            x.Status == "Pending" ||
                            x.Status == "Available"
                        ))
                    .ToListAsync();

            if (!expiredCredits.Any())
            {
                return;
            }

            foreach (var credit in expiredCredits)
            {
                credit.Status = "Expired";
                credit.UpdatedOn = DateTime.Now;

                await _compOffCreditRepository
                    .UpdateAsync(credit);
            }

            await _compOffCreditRepository.SaveAsync();
        }
    }
}