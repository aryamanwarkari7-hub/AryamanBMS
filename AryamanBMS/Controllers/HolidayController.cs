using AryamanBMS.Data;
using AryamanBMS.Services.Interface;
using AryamanBMS.Models;
using AryamanBMS.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR,Master")]
    public class HolidayController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHolidayExcelImportService _holidayExcelImportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly INotificationService _notificationService;

        public HolidayController(
            ApplicationDbContext context,
            IHolidayExcelImportService holidayExcelImportService,
            IWebHostEnvironment webHostEnvironment,
            UserManager<ApplicationUserModel> userManager,
            INotificationService notificationService)
        {
            _context = context;
            _holidayExcelImportService = holidayExcelImportService;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index(int? year, int? month, string? status)
        {
            int selectedYear = year ?? DateTime.Today.Year;
            string selectedStatus = string.IsNullOrWhiteSpace(status) ? "All" : status;

            ViewBag.Year = selectedYear;
            ViewBag.Month = month;
            ViewBag.Status = selectedStatus;

            var query = _context.Holidays
                .Where(x => x.HolidayDate.Year == selectedYear);

            if (month.HasValue)
            {
                query = query.Where(x => x.HolidayDate.Month == month.Value);
            }

            if (selectedStatus == "Active")
            {
                query = query.Where(x => x.IsActive);
            }
            else if (selectedStatus == "Inactive")
            {
                query = query.Where(x => !x.IsActive);
            }

            var holidays = await query
                .OrderBy(x => x.HolidayDate)
                .ToListAsync();

            return View(holidays);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExcel(IFormFile file, int year)
        {
            var result = await _holidayExcelImportService.ImportAsync(file);

            if (result.HasErrors)
            {
                TempData["Error"] = string.Join("<br />", result.Errors);
            }
            else
            {
                TempData["Success"] = result.Message;
            }

            await NotifyHolidayImportUsersAsync(
                result.HasErrors
                    ? "Holiday Import Failed"
                    : "Holiday Import Completed",
                result.HasErrors
                    ? $"Holiday import completed with {result.Errors.Count} error(s). {result.Message}"
                    : result.Message,
                result.HasErrors
                    ? "HolidayImportFailed"
                    : "HolidayImportSucceeded",
                year);

            return RedirectToAction(nameof(Index), new { year });
        }

        public async Task<IActionResult> ExportExcel(int? year, int? month, string? status)
        {
            int selectedYear = year ?? DateTime.Today.Year;
            string selectedStatus = string.IsNullOrWhiteSpace(status) ? "All" : status;

            var query = _context.Holidays
                .Where(x => x.HolidayDate.Year == selectedYear);

            if (month.HasValue)
            {
                query = query.Where(x => x.HolidayDate.Month == month.Value);
            }

            if (selectedStatus == "Active")
            {
                query = query.Where(x => x.IsActive);
            }
            else if (selectedStatus == "Inactive")
            {
                query = query.Where(x => !x.IsActive);
            }

            var holidays = await query
                .OrderBy(x => x.HolidayDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Holiday Register");

            worksheet.Cell(1, 1).Value = "Holiday Register";
            worksheet.Cell(2, 1).Value = $"Year: {selectedYear}";

            worksheet.Cell(4, 1).Value = "Date";
            worksheet.Cell(4, 2).Value = "Day";
            worksheet.Cell(4, 3).Value = "Month";
            worksheet.Cell(4, 4).Value = "Holiday Name";
            worksheet.Cell(4, 5).Value = "Type";
            worksheet.Cell(4, 6).Value = "Status";

            var header = worksheet.Range(4, 1, 4, 6);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.LightGray;

            int row = 5;

            foreach (var item in holidays)
            {
                worksheet.Cell(row, 1).Value = item.HolidayDate;
                worksheet.Cell(row, 1).Style.DateFormat.Format = "dd-mmm-yyyy";
                worksheet.Cell(row, 2).Value = item.DayName;
                worksheet.Cell(row, 3).Value = item.MonthName;
                worksheet.Cell(row, 4).Value = item.HolidayName;
                worksheet.Cell(row, 5).Value = item.HolidayType;
                worksheet.Cell(row, 6).Value = item.IsActive ? "Active" : "Inactive";

                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            string fileName = $"Holiday_Register_{selectedYear}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        public IActionResult DownloadTemplate()
        {
            var templatePath = Path.Combine(
                _webHostEnvironment.WebRootPath,
                "templates",
                "HolidayTemplate.xlsx");

            if (!System.IO.File.Exists(templatePath))
            {
                TempData["Error"] =
                    "Holiday Excel template not found. Please add HolidayTemplate.xlsx in wwwroot/templates.";

                return RedirectToAction(nameof(Index));
            }

            return PhysicalFile(
                templatePath,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "HolidayTemplate.xlsx");
        }

        private async Task NotifyHolidayImportUsersAsync(
            string title,
            string message,
            string notificationType,
            int year)
        {
            var recipients = new Dictionary<string, ApplicationUserModel>();

            foreach (var role in new[] { "Admin", "HR", "Master" })
            {
                foreach (var user in await _userManager.GetUsersInRoleAsync(role))
                {
                    recipients[user.Id] = user;
                }
            }

            foreach (var user in recipients.Values.Where(x => x.IsActive))
            {
                await _notificationService.CreateAsync(
                    user.Id,
                    title,
                    message,
                    notificationType,
                    "HolidayImport",
                    year,
                    $"/Holiday?year={year}");
            }
        }
    }
}
