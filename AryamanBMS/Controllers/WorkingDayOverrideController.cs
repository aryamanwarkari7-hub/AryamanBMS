using AryamanBMS.Data;
using AryamanBMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR,Master")]
    public class WorkingDayOverrideController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUserModel> _userManager;

        public WorkingDayOverrideController(
            ApplicationDbContext context,
            UserManager<ApplicationUserModel> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var overrides =
                await _context.WorkingDayOverrides
                    .AsNoTracking()
                    .OrderBy(x => x.OverrideDate)
                    .ToListAsync();

            return View(overrides);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new WorkingDayOverrideModel
            {
                OverrideDate = DateTime.Today,
                OverrideType = "Working Day"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            WorkingDayOverrideModel model)
        {
            model.OverrideDate = model.OverrideDate.Date;

            if (!IsValidOverrideType(model.OverrideType))
            {
                ModelState.AddModelError(
                    nameof(model.OverrideType),
                    "Select a valid override type.");
            }

            var dateExists =
                await _context.WorkingDayOverrides
                    .AnyAsync(x =>
                        x.OverrideDate.Date == model.OverrideDate);

            if (dateExists)
            {
                ModelState.AddModelError(
                    nameof(model.OverrideDate),
                    "An override already exists for this date.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.CreatedByUserId =
                _userManager.GetUserId(User);

            model.CreatedOn = DateTime.Now;
            model.IsActive = true;

            _context.WorkingDayOverrides.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Working-day override created successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var model =
                await _context.WorkingDayOverrides
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            WorkingDayOverrideModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            model.OverrideDate = model.OverrideDate.Date;

            if (!IsValidOverrideType(model.OverrideType))
            {
                ModelState.AddModelError(
                    nameof(model.OverrideType),
                    "Select a valid override type.");
            }

            var duplicateDate =
                await _context.WorkingDayOverrides
                    .AnyAsync(x =>
                        x.Id != id &&
                        x.OverrideDate.Date == model.OverrideDate);

            if (duplicateDate)
            {
                ModelState.AddModelError(
                    nameof(model.OverrideDate),
                    "An override already exists for this date.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existing =
                await _context.WorkingDayOverrides
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (existing == null)
            {
                return NotFound();
            }

            existing.OverrideDate = model.OverrideDate;
            existing.OverrideType = model.OverrideType;
            existing.Reason = model.Reason;
            existing.IsActive = model.IsActive;
            existing.UpdatedOn = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Working-day override updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var model =
                await _context.WorkingDayOverrides
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (model == null)
            {
                return NotFound();
            }

            model.IsActive = false;
            model.UpdatedOn = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Working-day override deactivated.";

            return RedirectToAction(nameof(Index));
        }

        private static bool IsValidOverrideType(string? value)
        {
            return value == "Working Day" ||
                   value == "Holiday" ||
                   value == "Weekly Off";
        }
    }
}