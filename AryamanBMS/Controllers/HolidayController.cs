
using AryamanBMS.Services.Interface;
using AryamanBMS.Models;
using AryamanBMS.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Controllers
{
    [Authorize]
    public class HolidayController : Controller
    {
        #region Actions

        private readonly IHolidayRepository _holidayRepository;
        private readonly IHolidayExcelImportService _holidayExcelImportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly INotificationService _notificationService;

        public HolidayController(
            IHolidayRepository holidayRepository,
            IHolidayExcelImportService holidayExcelImportService,
            IWebHostEnvironment webHostEnvironment,
            UserManager<ApplicationUserModel> userManager,
            INotificationService notificationService)
        {
            _holidayRepository = holidayRepository;
            _holidayExcelImportService = holidayExcelImportService;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index(
            int? year,
            int? month,
            string? status,
            string sortBy = "HolidayDate",
            string sortOrder = "asc",
            int page = 1)
        {
            const int pageSize = 12;

            bool canManageHolidays =
                User.IsInRole("Admin") ||
                User.IsInRole("HR") ||
                User.IsInRole("Master");
            int selectedYear = year ?? DateTime.Today.Year;
            string selectedStatus = canManageHolidays &&
                        !string.IsNullOrWhiteSpace(status)
                ? status
                : "Active";

            sortBy = sortBy switch
            {
                "HolidayName" => "HolidayName",
                "HolidayType" => "HolidayType",
                "Status" => "Status",
                _ => "HolidayDate"
            };

            sortOrder = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase)
                ? "desc"
                : "asc";

            ViewBag.Year = selectedYear;
            ViewBag.Month = month;
            ViewBag.Status = selectedStatus;
            ViewBag.CanManageHolidays = canManageHolidays;

            var result = await _holidayRepository.GetPagedAsync(
    selectedYear,
    month,
    selectedStatus,
    sortBy,
    sortOrder,
    page,
    pageSize);

            int totalRecords = result.TotalRecords;

            int totalPages = totalRecords == 0
                ? 1
                : (int)Math.Ceiling(
                    totalRecords / (double)pageSize);

            page = Math.Clamp(page, 1, totalPages);

            var holidays = result.Records;

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalRecords = totalRecords;
            ViewBag.TotalPages = totalPages;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

            return View(holidays);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,HR")]
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

        [Authorize(Roles = "Admin,HR,Master")]
        public async Task<IActionResult> ExportExcel(int? year, int? month, string? status)
        {
            int selectedYear = year ?? DateTime.Today.Year;
            string selectedStatus = string.IsNullOrWhiteSpace(status) ? "All" : status;

            var holidays = await _holidayRepository.GetForExportAsync(
    selectedYear,
    month,
    selectedStatus);



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


        [Authorize(Roles = "Admin,HR,Master")]
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
        #endregion
    }
}
