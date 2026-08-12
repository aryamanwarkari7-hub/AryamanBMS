using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class SalaryStructureController : Controller
    {
        #region Actions

        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public SalaryStructureController(
            ApplicationDbContext context,
            INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
           string? search,
           string sortBy = "EmployeeCode",
           string sortOrder = "asc")
        {
            var query =
                _context.EmployeeSalaryStructures
                    .Include(x => x.Employee)
                    .AsNoTracking()
                    .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim().ToLower();

                query = query.Where(x =>
                   x.Employee != null &&
                   (
                       (x.Employee.EmployeeCode ?? string.Empty).ToLower().Contains(keyword) ||
                       (x.Employee.FirstName ?? string.Empty).ToLower().Contains(keyword) ||
                       (x.Employee.LastName ?? string.Empty).ToLower().Contains(keyword)
                   ));
            }

            bool desc =
                string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            query = sortBy switch
            {
                "EmployeeName" => desc
                    ? query.OrderByDescending(x => x.Employee!.FirstName)
                           .ThenByDescending(x => x.Employee!.LastName)
                    : query.OrderBy(x => x.Employee!.FirstName)
                           .ThenBy(x => x.Employee!.LastName),

                "EffectiveFrom" => desc
                    ? query.OrderByDescending(x => x.EffectiveFrom)
                    : query.OrderBy(x => x.EffectiveFrom),

                "ActualSalary" => desc
                    ? query.OrderByDescending(x => x.ActualSalary)
                    : query.OrderBy(x => x.ActualSalary),

                "Status" => desc
                    ? query.OrderByDescending(x => x.IsActive)
                    : query.OrderBy(x => x.IsActive),

                "UpdatedOn" => desc
                    ? query.OrderByDescending(x => x.UpdatedOn)
                    : query.OrderBy(x => x.UpdatedOn),

                _ => desc
                    ? query.OrderByDescending(x => x.Employee!.EmployeeCode)
                           .ThenByDescending(x => x.EffectiveFrom)
                    : query.OrderBy(x => x.Employee!.EmployeeCode)
                           .ThenByDescending(x => x.EffectiveFrom)
            };

            var salaryStructures =
                await query.ToListAsync();

            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

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
            model.CreatedByUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);
            model.RevisedSalary = model.ActualSalary;
            model.RevisionEffectiveDate = model.EffectiveFrom;

            _context.EmployeeSalaryStructures.Add(model);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await NotifyEmployeeSalaryStructureAsync(
                model.EmployeeId,
                "Salary Structure Added",
                "Your salary structure has been added.",
                "SalaryStructureCreated");

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
                salaryStructure.ActualSalary != model.ActualSalary ||
                salaryStructure.BasicSalary != model.BasicSalary ||
                salaryStructure.HRA != model.HRA ||
                salaryStructure.DA != model.DA ||
                salaryStructure.Conveyance != model.Conveyance ||
                salaryStructure.MedicalAllowance != model.MedicalAllowance ||
                salaryStructure.EducationAllowance != model.EducationAllowance ||
                salaryStructure.SpecialAllowance != model.SpecialAllowance ||
                salaryStructure.OtherAllowances != model.OtherAllowances;

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
            decimal previousSalary = salaryStructure.ActualSalary;
            salaryStructure.ActualSalary = model.ActualSalary;
            salaryStructure.BasicSalary = model.BasicSalary;
            salaryStructure.HRA = model.HRA;
            salaryStructure.DA = model.DA;
            salaryStructure.Conveyance = model.Conveyance;
            salaryStructure.MedicalAllowance = model.MedicalAllowance;
            salaryStructure.EducationAllowance = model.EducationAllowance;
            salaryStructure.SpecialAllowance = model.SpecialAllowance;
            salaryStructure.OtherAllowances = model.OtherAllowances;
            salaryStructure.IsPfApplicable = model.IsPfApplicable;
            salaryStructure.IsEsicApplicable = model.IsEsicApplicable;
            salaryStructure.IsPtApplicable = model.IsPtApplicable;
            salaryStructure.IsTdsApplicable = model.IsTdsApplicable;
            salaryStructure.RevisionReason = model.RevisionReason?.Trim();
            salaryStructure.UpdatedByUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (previousSalary != model.ActualSalary)
            {
                salaryStructure.PreviousSalary = previousSalary;
                salaryStructure.RevisedSalary = model.ActualSalary;
                salaryStructure.RevisionEffectiveDate = model.EffectiveFrom;
                salaryStructure.RevisionPercentage =
                    previousSalary > 0
                        ? Math.Round(
                            (model.ActualSalary - previousSalary) /
                            previousSalary * 100,
                            2)
                        : 0;
                salaryStructure.ApprovedByUserId =
                    salaryStructure.UpdatedByUserId;
                salaryStructure.ApprovedOn = DateTime.Now;
            }

            salaryStructure.IsActive = model.IsActive;
            salaryStructure.UpdatedOn = DateTime.Now;

            await _context.SaveChangesAsync();

            await NotifyEmployeeSalaryStructureAsync(
                salaryStructure.EmployeeId,
                "Salary Structure Updated",
                "Your salary structure has been updated.",
                "SalaryStructureUpdated");

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

            await NotifyEmployeeSalaryStructureAsync(
                salaryStructure.EmployeeId,
                salaryStructure.IsActive
                    ? "Salary Structure Activated"
                    : "Salary Structure Deactivated",
                salaryStructure.IsActive
                    ? "Your salary structure has been activated."
                    : "Your salary structure has been deactivated.",
                salaryStructure.IsActive
                    ? "SalaryStructureActivated"
                    : "SalaryStructureDeactivated");

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

            decimal breakupTotal =
                model.BasicSalary +
                model.HRA +
                model.DA +
                model.Conveyance +
                model.MedicalAllowance +
                model.EducationAllowance +
                model.SpecialAllowance +
                model.OtherAllowances;

            if (breakupTotal < 0)
            {
                ModelState.AddModelError(
                    "",
                    "Salary breakup values cannot be negative.");
            }

            if (breakupTotal > 0 &&
                model.ActualSalary > 0 &&
                breakupTotal != model.ActualSalary)
            {
                ModelState.AddModelError(
                    nameof(model.ActualSalary),
                    $"Salary breakup total {breakupTotal:N2} must match actual salary {model.ActualSalary:N2}.");
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
            model.ActualSalary = Math.Round(model.ActualSalary, 2);
            model.BasicSalary = Math.Round(model.BasicSalary, 2);
            model.HRA = Math.Round(model.HRA, 2);
            model.DA = Math.Round(model.DA, 2);
            model.Conveyance = Math.Round(model.Conveyance, 2);
            model.MedicalAllowance = Math.Round(model.MedicalAllowance, 2);
            model.EducationAllowance = Math.Round(model.EducationAllowance, 2);
            model.SpecialAllowance = Math.Round(model.SpecialAllowance, 2);
            model.OtherAllowances = Math.Round(model.OtherAllowances, 2);
            model.RevisionReason =
                string.IsNullOrWhiteSpace(model.RevisionReason)
                    ? null
                    : model.RevisionReason.Trim();

            decimal breakupTotal =
                model.BasicSalary +
                model.HRA +
                model.DA +
                model.Conveyance +
                model.MedicalAllowance +
                model.EducationAllowance +
                model.SpecialAllowance +
                model.OtherAllowances;

            if (model.ActualSalary == 0 &&
                breakupTotal > 0)
            {
                model.ActualSalary = breakupTotal;
            }
        }

        private async Task LoadEmployeesAsync()
        {
            ViewBag.Employees =
                await _context.Employees
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.EmployeeCode)
                    .ToListAsync();
        }

        private async Task NotifyEmployeeSalaryStructureAsync(
            int employeeId,
            string title,
            string message,
            string notificationType)
        {
            var userId = await _context.Employees
                .AsNoTracking()
                .Where(x => x.Id == employeeId)
                .Select(x => x.ApplicationUserId)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            await _notificationService.CreateAsync(
                userId,
                title,
                message,
                notificationType,
                "SalaryStructure",
                employeeId,
                "/Salary/MySalary");
        }
        #endregion
    }
}
