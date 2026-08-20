using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize]
    public class OffDayWorkController : Controller
    {
        private readonly IOffDayWorkRequestRepository _repository;
        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly IEmployeeRepository _employeeRepository;

        public OffDayWorkController(
            IOffDayWorkRequestRepository repository,
            UserManager<ApplicationUserModel> userManager,
            IEmployeeRepository employeeRepository)
        {
            _repository = repository;
            _userManager = userManager;
            _employeeRepository = employeeRepository;
        }

        #region Employee Actions

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _employeeRepository.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == user.Id);

            if (employee == null)
            {
                return NotFound();
            }

            var requests = await _repository.Requests
                .AsNoTracking()
                .Where(x => x.EmployeeId == employee.Id)
                .OrderByDescending(x => x.WorkDate)
                .ToListAsync();

            return View(requests);
        }

        [HttpGet]
        public async Task<IActionResult> Create(
            DateTime? workDate)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _employeeRepository.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == user.Id);

            if (employee == null)
            {
                return NotFound();
            }

            var model = new OffDayWorkRequestModel
            {
                EmployeeId = employee.Id,
                WorkDate = workDate?.Date ?? DateTime.Today,
                Status = "Pending",
                RequestedByUserId = user.Id,
                RequestedOn = DateTime.Now
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            OffDayWorkRequestModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _employeeRepository.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ApplicationUserId == user.Id);

            if (employee == null)
            {
                return NotFound();
            }

            model.EmployeeId = employee.Id;
            model.RequestedByUserId = user.Id;
            model.RequestedOn = DateTime.Now;
            model.Status = "Pending";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.OffDayType != "WeeklyOff" &&
                model.OffDayType != "Holiday")
            {
                ModelState.AddModelError(
                    "OffDayType",
                    "Invalid off-day type.");

                return View(model);
            }

            bool alreadyExists = await _repository.Requests
                .AnyAsync(x =>
                    x.EmployeeId == employee.Id &&
                    x.WorkDate.Date == model.WorkDate.Date &&
                    x.Status != "Rejected");

            if (alreadyExists)
            {
                ModelState.AddModelError(
                    "",
                    "An off-day work request already exists for this date.");

                return View(model);
            }

            await _repository.AddAsync(model);
            await _repository.SaveAsync();

            TempData["Success"] =
                "Off-day work request submitted successfully.";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Admin / HR Actions

        [HttpGet]
        [Authorize(Roles = "Admin,HR,Master")]
        public async Task<IActionResult> Manage()
        {
            var requests = await _repository.Requests
                .AsNoTracking()
                .Include(x => x.Employee)
                .OrderByDescending(x => x.RequestedOn)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR,Master")]
        public async Task<IActionResult> Approve(
    int id,
    string? approvalRemarks)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var request = await _repository.GetByIdAsync(id);

            if (request == null)
            {
                return NotFound();
            }

            if (!string.Equals(
                    request.Status,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Only pending requests can be approved.";

                return RedirectToAction(nameof(Manage));
            }

            request.Status = "Approved";
            request.ApprovedByUserId = user.Id;
            request.ApprovedOn = DateTime.Now;
            request.ApprovalRemarks =
                string.IsNullOrWhiteSpace(approvalRemarks)
                    ? null
                    : approvalRemarks.Trim();

            await _repository.UpdateAsync(request);
            await _repository.SaveAsync();

            TempData["Success"] =
                "Off-day work request approved successfully.";

            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR,Master")]
        public async Task<IActionResult> Reject(
    int id,
    string? approvalRemarks)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var request = await _repository.GetByIdAsync(id);

            if (request == null)
            {
                return NotFound();
            }

            if (!string.Equals(
                    request.Status,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Only pending requests can be rejected.";

                return RedirectToAction(nameof(Manage));
            }

            request.Status = "Rejected";
            request.RejectedByUserId = user.Id;
            request.RejectedOn = DateTime.Now;
            request.ApprovalRemarks =
                string.IsNullOrWhiteSpace(approvalRemarks)
                    ? null
                    : approvalRemarks.Trim();

            await _repository.UpdateAsync(request);
            await _repository.SaveAsync();

            TempData["Success"] =
                "Off-day work request rejected.";

            return RedirectToAction(nameof(Manage));
        }

        #endregion
    }
}