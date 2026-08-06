using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class LeaveTypeController : Controller
    {
        private readonly ILeaveTypeRepository _leaveTypeRepository;

        public LeaveTypeController(
            ILeaveTypeRepository leaveTypeRepository)
        {
            _leaveTypeRepository = leaveTypeRepository;
        }

        public IActionResult Index(
    string? searchText,
    string sortBy = "LeaveName",
    string sortOrder = "asc")
        {
            var leaveTypes = _leaveTypeRepository.LeaveTypes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string search = searchText.Trim();

                leaveTypes = leaveTypes.Where(x =>
                    x.LeaveCode.Contains(search) ||
                    x.LeaveName.Contains(search));
            }

            bool desc = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            leaveTypes = sortBy switch
            {
                "LeaveCode" => desc
                    ? leaveTypes.OrderByDescending(x => x.LeaveCode)
                    : leaveTypes.OrderBy(x => x.LeaveCode),

                "Status" => desc
                    ? leaveTypes.OrderByDescending(x => x.IsActive)
                    : leaveTypes.OrderBy(x => x.IsActive),

                _ => desc
                    ? leaveTypes.OrderByDescending(x => x.LeaveName)
                    : leaveTypes.OrderBy(x => x.LeaveName)
            };

            ViewBag.SearchText = searchText;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

            return View(leaveTypes.ToList());
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create(
            LeaveTypeModel leaveType)
        {
            NormalizeCompOffLeaveType(leaveType);

            bool exists =
                _leaveTypeRepository.LeaveTypes.Any(x =>
                    x.LeaveCode == leaveType.LeaveCode);

            if (exists)
            {
                ModelState.AddModelError(
                    "LeaveCode",
                    "Leave Code already exists.");
            }

            if (!leaveType.IsCarryForward)
            {
                leaveType.MaximumCarryForwardDays = 0;
            }
            else if (!leaveType.MaximumCarryForwardDays.HasValue ||
                     leaveType.MaximumCarryForwardDays.Value <= 0)
            {
                ModelState.AddModelError(
                    nameof(leaveType.MaximumCarryForwardDays),
                    "Maximum carry-forward days must be greater than zero.");
            }

            if (ModelState.IsValid)
            {
                await _leaveTypeRepository.AddAsync(
                    leaveType);

                await _leaveTypeRepository.SaveAsync();

                TempData["Success"] =
                    "Leave Type created successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(leaveType);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var leaveType =
                await _leaveTypeRepository.GetByIdAsync(id);

            if (leaveType == null)
            {
                return NotFound();
            }

            return View(leaveType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(
            LeaveTypeModel leaveType)
        {
            NormalizeCompOffLeaveType(leaveType);

            if (!leaveType.IsCarryForward)
            {
                leaveType.MaximumCarryForwardDays = 0;
            }
            else if (!leaveType.MaximumCarryForwardDays.HasValue ||
                     leaveType.MaximumCarryForwardDays.Value <= 0)
            {
                ModelState.AddModelError(
                    nameof(leaveType.MaximumCarryForwardDays),
                    "Maximum carry-forward days must be greater than zero.");
            }
            if (ModelState.IsValid)
            {
                await _leaveTypeRepository
                    .UpdateAsync(leaveType);

                await _leaveTypeRepository.SaveAsync();

                TempData["Success"] =
                    "Leave Type updated successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(leaveType);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var leaveType =
                await _leaveTypeRepository.GetByIdAsync(id);

            if (leaveType == null)
            {
                return NotFound();
            }

            return View(leaveType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var leaveType = await _leaveTypeRepository.GetByIdAsync(id);

            if (leaveType == null)
            {
                return NotFound();
            }

            leaveType.IsActive = false;

            await _leaveTypeRepository.UpdateAsync(leaveType);
            await _leaveTypeRepository.SaveAsync();

            TempData["Success"] = "Leave type deactivated successfully.";

            return RedirectToAction(nameof(Index));
        }

        private static void NormalizeCompOffLeaveType(
            LeaveTypeModel leaveType)
        {
            if (!IsCompOffLeaveType(leaveType))
            {
                return;
            }

            leaveType.DaysPerYear = 0;
            leaveType.IsCarryForward = false;
            leaveType.MaximumCarryForwardDays = 0;
        }

        private static bool IsCompOffLeaveType(
            LeaveTypeModel leaveType)
        {
            return string.Equals(
                       leaveType.LeaveCode?.Trim(),
                       "COMP",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       leaveType.LeaveName?.Trim(),
                       "Comp Off",
                       StringComparison.OrdinalIgnoreCase);
        }
    }
}
