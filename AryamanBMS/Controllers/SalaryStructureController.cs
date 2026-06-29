using AryamanBMS.Data;
using AryamanBMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class SalaryStructureController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalaryStructureController(
            ApplicationDbContext context)
        {
            _context = context;
        }

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
        public async Task<IActionResult> Create(
            EmployeeSalaryStructureModel model)
        {
            if (model.EffectiveFrom == default)
            {
                ModelState.AddModelError(
                    nameof(model.EffectiveFrom),
                    "Effective from date is required.");
            }

            bool duplicateExists =
                await _context.EmployeeSalaryStructures
                    .AnyAsync(x =>
                        x.EmployeeId == model.EmployeeId &&
                        x.EffectiveFrom.Date ==
                        model.EffectiveFrom.Date);

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    "",
                    "Salary structure already exists for this employee and effective date.");
            }

            if (!ModelState.IsValid)
            {
                await LoadEmployeesAsync();
                return View(model);
            }

            model.CreatedOn = DateTime.Now;
            model.UpdatedOn = null;

            _context.EmployeeSalaryStructures.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Salary structure added successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
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
        public async Task<IActionResult> Edit(
            EmployeeSalaryStructureModel model)
        {
            if (model.EffectiveFrom == default)
            {
                ModelState.AddModelError(
                    nameof(model.EffectiveFrom),
                    "Effective from date is required.");
            }

            bool duplicateExists =
                await _context.EmployeeSalaryStructures
                    .AnyAsync(x =>
                        x.Id != model.Id &&
                        x.EmployeeId == model.EmployeeId &&
                        x.EffectiveFrom.Date ==
                        model.EffectiveFrom.Date);

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    "",
                    "Salary structure already exists for this employee and effective date.");
            }

            if (!ModelState.IsValid)
            {
                await LoadEmployeesAsync();
                return View(model);
            }

            var salaryStructure =
                await _context.EmployeeSalaryStructures
                    .FirstOrDefaultAsync(x => x.Id == model.Id);

            if (salaryStructure == null)
            {
                return NotFound();
            }

            salaryStructure.EmployeeId = model.EmployeeId;
            salaryStructure.EffectiveFrom = model.EffectiveFrom;
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