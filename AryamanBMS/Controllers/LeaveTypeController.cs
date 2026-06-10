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

        public IActionResult Index()
        {
            var leaveTypes = _leaveTypeRepository.LeaveTypes
                .OrderBy(x => x.LeaveCode)
                .ToList();

            return View(leaveTypes);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            LeaveTypeModel leaveType)
        {
            bool exists =
                _leaveTypeRepository.LeaveTypes.Any(x =>
                    x.LeaveCode == leaveType.LeaveCode);

            if (exists)
            {
                ModelState.AddModelError(
                    "LeaveCode",
                    "Leave Code already exists.");
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
        public async Task<IActionResult> Edit(
            LeaveTypeModel leaveType)
        {
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
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var leaveType =
                await _leaveTypeRepository.GetByIdAsync(id);

            if (leaveType != null)
            {
                await _leaveTypeRepository
                    .DeleteAsync(leaveType);

                await _leaveTypeRepository.SaveAsync();
            }

            TempData["Success"] =
                "Leave Type deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}