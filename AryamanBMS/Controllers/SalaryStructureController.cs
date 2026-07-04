using AryamanBMS.Data;
using AryamanBMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR,Finance")]
    public class SalaryStructureController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalaryStructureController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var salaryStructures =
                await _context.EmployeeSalaryStructures
                    .Include(x => x.Employee)
                    .OrderBy(x => x.Employee!.EmployeeCode)
                    .ThenByDescending(x => x.EffectiveFrom)
                    .ToListAsync();

            return View(salaryStructures);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create()
        {
            await LoadEmployeesAsync();

            return View(new EmployeeSalaryStructureModel
            {
                EffectiveFrom = DateTime.Today,
                IsActive = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create(EmployeeSalaryStructureModel model)
        {
            NormalizeModel(model);

            var closablePreviousStructure =
                await _context.EmployeeSalaryStructures
                    .Where(x =>
                        x.EmployeeId == model.EmployeeId &&
                        x.IsActive &&
                        x.EffectiveFrom.Date < model.EffectiveFrom.Date &&
                        (!x.EffectiveTo.HasValue ||
                         x.EffectiveTo.Value.Date >= model.EffectiveFrom.Date))
                    .OrderByDescending(x => x.EffectiveFrom)
                    .FirstOrDefaultAsync();

            await ValidateStructureAsync(
                model,
                null,
                closablePreviousStructure?.Id);

            if (!ModelState.IsValid)
            {
                await LoadEmployeesAsync();
                return View(model);
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            if (closablePreviousStructure != null)
            {
                closablePreviousStructure.EffectiveTo =
                    model.EffectiveFrom.Date.AddDays(-1);
                closablePreviousStructure.UpdatedOn = DateTime.Now;
            }

            model.CreatedOn = DateTime.Now;
            model.UpdatedOn = null;

            _context.EmployeeSalaryStructures.Add(model);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] =
                "Salary structure added successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(int id)
        {
            var salaryStructure =
                await _context.EmployeeSalaryStructures
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (salaryStructure == null)
            {
                return NotFound();
            }

            await LoadEmployeesAsync();

            return View(salaryStructure);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(EmployeeSalaryStructureModel model)
        {
            NormalizeModel(model);

            var salaryStructure =
                await _context.EmployeeSalaryStructures
                    .FirstOrDefaultAsync(x => x.Id == model.Id);

            if (salaryStructure == null)
            {
                return NotFound();
            }

            bool hasPayrollHistory =
                await HasLinkedSalaryRecordsAsync(salaryStructure);

            bool structureChanged =
                salaryStructure.EmployeeId != model.EmployeeId ||
                salaryStructure.EffectiveFrom.Date != model.EffectiveFrom.Date ||
                salaryStructure.EffectiveTo?.Date != model.EffectiveTo?.Date ||
                salaryStructure.ActualSalary != model.ActualSalary;

            if (hasPayrollHistory && structureChanged)
            {
                ModelState.AddModelError(
                    "",
                    "This salary structure is already used in payroll history and cannot be changed.");
            }

            await ValidateStructureAsync(model, model.Id);

            if (!ModelState.IsValid)
            {
                await LoadEmployeesAsync();
                return View(model);
            }

            salaryStructure.EmployeeId = model.EmployeeId;
            salaryStructure.EffectiveFrom = model.EffectiveFrom.Date;
            salaryStructure.EffectiveTo = model.EffectiveTo?.Date;
            salaryStructure.ActualSalary = model.ActualSalary;
            salaryStructure.IsActive = model.IsActive;
            salaryStructure.UpdatedOn = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Salary structure updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var salaryStructure =
                await _context.EmployeeSalaryStructures
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (salaryStructure == null)
            {
                return NotFound();
            }

            salaryStructure.IsActive = !salaryStructure.IsActive;
            salaryStructure.UpdatedOn = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                salaryStructure.IsActive
                    ? "Salary structure activated successfully."
                    : "Salary structure deactivated successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateStructureAsync(
            EmployeeSalaryStructureModel model,
            int? excludeId = null,
            int? closableOverlapId = null)
        {
            if (model.EffectiveFrom == default)
            {
                ModelState.AddModelError(
                    nameof(model.EffectiveFrom),
                    "Effective from date is required.");
            }

            if (model.ActualSalary < 0)
            {
                ModelState.AddModelError(
                    nameof(model.ActualSalary),
                    "Actual salary cannot be negative.");
            }

            if (model.EffectiveTo.HasValue &&
                model.EffectiveTo.Value.Date < model.EffectiveFrom.Date)
            {
                ModelState.AddModelError(
                    nameof(model.EffectiveTo),
                    "Effective to date cannot be earlier than effective from date.");
            }

            bool exactDuplicateExists =
                await _context.EmployeeSalaryStructures
                    .AnyAsync(x =>
                        x.Id != excludeId &&
                        x.EmployeeId == model.EmployeeId &&
                        x.EffectiveFrom.Date == model.EffectiveFrom.Date);

            if (exactDuplicateExists)
            {
                ModelState.AddModelError(
                    "",
                    "Salary structure already exists for this employee and effective from date.");
            }

            var overlapExists =
                await _context.EmployeeSalaryStructures
                    .Where(x =>
                        x.Id != excludeId &&
                        x.Id != closableOverlapId &&
                        x.EmployeeId == model.EmployeeId &&
                        x.IsActive)
                    .AnyAsync(x =>
                        x.EffectiveFrom.Date <= (model.EffectiveTo ?? DateTime.MaxValue).Date &&
                        model.EffectiveFrom.Date <= (x.EffectiveTo ?? DateTime.MaxValue).Date);

            if (overlapExists)
            {
                ModelState.AddModelError(
                    "",
                    "This date range overlaps with another active salary structure for the same employee.");
            }
        }

        private async Task<bool> HasLinkedSalaryRecordsAsync(
            EmployeeSalaryStructureModel structure)
        {
            var salaryPeriods =
                await _context.SalaryRecords
                    .AsNoTracking()
                    .Where(x => x.EmployeeId == structure.EmployeeId)
                    .Select(x => new DateTime(x.Year, x.Month, 1))
                    .ToListAsync();

            var rangeEnd = structure.EffectiveTo?.Date ?? DateTime.MaxValue.Date;

            return salaryPeriods.Any(x =>
                x.Date >= structure.EffectiveFrom.Date &&
                x.Date <= rangeEnd);
        }

        private void NormalizeModel(EmployeeSalaryStructureModel model)
        {
            model.EffectiveFrom = model.EffectiveFrom.Date;
            model.EffectiveTo = model.EffectiveTo?.Date;
        }

        private async Task LoadEmployeesAsync()
        {
            ViewBag.Employees =
                await _context.Employees
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.EmployeeCode)
                    .ToListAsync();
        }
    }
}
