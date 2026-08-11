using AryamanBMS.Data;
using AryamanBMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using ClosedXML.Excel;

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
        public async Task<IActionResult> ExportExcel()
        {
            var overrides =
                await _context.WorkingDayOverrides
                    .AsNoTracking()
                    .OrderBy(x => x.OverrideDate)
                    .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Saturday Switcher");

            worksheet.Cell(1, 1).Value = "Saturday Switcher";
            worksheet.Cell(2, 1).Value =
                "Date-specific Saturday schedule changes";

            worksheet.Cell(4, 1).Value = "Date";
            worksheet.Cell(4, 2).Value = "Day";
            worksheet.Cell(4, 3).Value = "Schedule";
            worksheet.Cell(4, 4).Value = "Reason";
            worksheet.Cell(4, 5).Value = "Status";

            var header = worksheet.Range(4, 1, 4, 5);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.LightGray;

            var row = 5;

            foreach (var item in overrides)
            {
                worksheet.Cell(row, 1).Value = item.OverrideDate;
                worksheet.Cell(row, 1).Style.DateFormat.Format =
                    "dd-mmm-yyyy";
                worksheet.Cell(row, 2).Value =
                    item.OverrideDate.ToString("dddd");
                worksheet.Cell(row, 3).Value =
                    item.OverrideType == "Working Day"
                        ? "Working Saturday"
                        : item.OverrideType;
                worksheet.Cell(row, 4).Value = item.Reason ?? string.Empty;
                worksheet.Cell(row, 5).Value =
                    item.IsActive ? "Active" : "Inactive";

                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Saturday_Switcher_{DateTime.Today:yyyyMMdd}.xlsx");
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
