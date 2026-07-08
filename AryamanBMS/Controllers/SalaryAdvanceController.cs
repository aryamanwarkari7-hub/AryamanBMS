using AryamanBMS.Data;
using AryamanBMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR,Finance")]
    public class SalaryAdvanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalaryAdvanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var advances =
                await _context.SalaryAdvances
                    .AsNoTracking()
                    .Include(x => x.Employee)
                    .OrderByDescending(x => x.AdvanceDate)
                    .ThenBy(x => x.Employee!.EmployeeCode)
                    .ToListAsync();

            return View(advances);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadEmployeesAsync();

            return View(new SalaryAdvanceModel
            {
                AdvanceDate = DateTime.Today,
                RecoveryStartMonth = DateTime.Today.Month,
                RecoveryStartYear = DateTime.Today.Year,
                Status = "Open"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SalaryAdvanceModel model)
        {
            Normalize(model);
            Validate(model);

            if (!ModelState.IsValid)
            {
                await LoadEmployeesAsync();
                return View(model);
            }

            model.OutstandingBalance = model.AdvanceAmount;
            model.TotalRecovered = 0;
            model.ApprovedByUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);
            model.ApprovedOn = DateTime.Now;
            model.CreatedOn = DateTime.Now;

            await _context.SalaryAdvances.AddAsync(model);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Salary advance created successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadEmployeesAsync()
        {
            ViewBag.Employees =
                await _context.Employees
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.EmployeeCode)
                    .ToListAsync();
        }

        private static void Normalize(SalaryAdvanceModel model)
        {
            model.AdvanceAmount = Math.Round(model.AdvanceAmount, 2);
            model.MonthlyRecoveryAmount =
                Math.Round(model.MonthlyRecoveryAmount, 2);
            model.Status =
                string.IsNullOrWhiteSpace(model.Status)
                    ? "Open"
                    : model.Status.Trim();
            model.Remarks =
                string.IsNullOrWhiteSpace(model.Remarks)
                    ? null
                    : model.Remarks.Trim();
        }

        private void Validate(SalaryAdvanceModel model)
        {
            if (model.EmployeeId <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.EmployeeId),
                    "Employee is required.");
            }

            if (model.AdvanceAmount <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.AdvanceAmount),
                    "Advance amount must be greater than zero.");
            }

            if (model.MonthlyRecoveryAmount <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.MonthlyRecoveryAmount),
                    "Monthly recovery amount must be greater than zero.");
            }

            if (model.RecoveryStartMonth < 1 ||
                model.RecoveryStartMonth > 12)
            {
                ModelState.AddModelError(
                    nameof(model.RecoveryStartMonth),
                    "Recovery start month is invalid.");
            }
        }
    }
}
