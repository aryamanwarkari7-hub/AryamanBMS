using AryamanBMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;

using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR,Master")]
    public class WorkingDayOverrideController : Controller
    {
        #region Actions

        private readonly IWorkingDayOverrideRepository _workingDayOverrideRepository;
        private readonly UserManager<ApplicationUserModel> _userManager;

        public WorkingDayOverrideController(
           IWorkingDayOverrideRepository workingDayOverrideRepository,
            UserManager<ApplicationUserModel> userManager)
        {
            _workingDayOverrideRepository = workingDayOverrideRepository;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? status,
            string sortBy = "OverrideDate",
            string sortOrder = "asc")
        {
            string selectedStatus = string.IsNullOrWhiteSpace(status) ? "All" : status;
            sortBy = sortBy switch
            {
                "OverrideType" => "OverrideType",
                "Status" => "Status",
                _ => "OverrideDate"
            };
            sortOrder = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase)
                ? "desc"
                : "asc";

            var overrides = await _workingDayOverrideRepository
    .GetAllAsync(
        selectedStatus,
        sortBy,
        sortOrder);

            ViewBag.Status = selectedStatus;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

            return View(overrides);
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel()
        {
            var overrides = await _workingDayOverrideRepository
    .GetForExportAsync();

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
        [Authorize(Roles = "Admin,HR")]
        public IActionResult Create()
        {
            return View(new WorkingDayOverrideModel
            {
                OverrideDate = DateTime.Today,
                OverrideType = "Working Day"
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,HR")]
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

            var dateExists = await _workingDayOverrideRepository
    .ExistsForDateAsync(model.OverrideDate);

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

            await _workingDayOverrideRepository.AddAsync(model);
            await _workingDayOverrideRepository.SaveAsync();

            TempData["Success"] =
                "Working-day override created successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(int id)
        {
            var model =
                await _workingDayOverrideRepository.GetByIdAsync(id);

            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,HR")]
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

            var duplicateDate = await _workingDayOverrideRepository
    .ExistsForDateAsync(
        model.OverrideDate,
        id);

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

            var existing = await _workingDayOverrideRepository.GetByIdAsync(id);

            if (existing == null)
            {
                return NotFound();
            }

            existing.OverrideDate = model.OverrideDate;
            existing.OverrideType = model.OverrideType;
            existing.Reason = model.Reason;
            existing.IsActive = model.IsActive;
            existing.UpdatedOn = DateTime.Now;

            await _workingDayOverrideRepository.SaveAsync();

            TempData["Success"] =
                "Working-day override updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,HR")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var model = await _workingDayOverrideRepository.GetByIdAsync(id);

            if (model == null)
            {
                return NotFound();
            }

            model.IsActive = false;
            model.UpdatedOn = DateTime.Now;

            await _workingDayOverrideRepository.SaveAsync();

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
        #endregion
    }
}
