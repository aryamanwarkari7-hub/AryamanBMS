using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize]
    public class DailyWorkReportController : Controller
    {
        private readonly IDailyWorkReportService _reportService;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly UserManager<ApplicationUserModel> _userManager;

        public DailyWorkReportController(
            IDailyWorkReportService reportService,
            IEmployeeRepository employeeRepository,
            IProjectRepository projectRepository,
            UserManager<ApplicationUserModel> userManager
        )
        {
            _reportService = reportService;
            _employeeRepository = employeeRepository;
            _projectRepository = projectRepository;
            _userManager = userManager;
        }

        private async Task<EmployeeModel?> GetCurrentEmployeeAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return null;

            return await _employeeRepository
                .Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.ApplicationUserId == user.Id);
        }

        private bool IsAdminOrManager()
        {
            return User.IsInRole("Admin") || User.IsInRole("HR") || User.IsInRole("Master");
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            DateTime? fromDate,
            DateTime? toDate,
            int? employeeId,
            int? projectId,
            bool mine = false
        )
        {
            var currentEmployee = await GetCurrentEmployeeAsync();
            bool isAdmin = IsAdminOrManager();

            // Default to current month if no dates selected
            var now = DateTime.Today;
            var fDate = fromDate ?? new DateTime(now.Year, now.Month, 1);
            var tDate = toDate ?? now;

            // Restrict employees to only their reports
            int? targetEmployeeId = null;
            if (!isAdmin || mine)
            {
                if (currentEmployee == null)
                {
                    TempData["Error"] = "Your user account is not linked to an employee profile.";
                    return RedirectToAction("Index", "Home");
                }
                targetEmployeeId = currentEmployee.Id;
            }
            else
            {
                targetEmployeeId = employeeId;
            }

            var reports = await _reportService.GetTeamReportsAsync(
                targetEmployeeId,
                projectId,
                fDate,
                tDate
            );

            var empIds = reports.Select(r => r.EmployeeId).Distinct().ToList();
            if (empIds.Any())
            {
                var empMap = await _employeeRepository
                    .Employees.Where(e => empIds.Contains(e.Id))
                    .ToDictionaryAsync(e => e.Id);

                foreach (var r in reports)
                {
                    if (r.Employee == null && empMap.TryGetValue(r.EmployeeId, out var emp))
                    {
                        r.Employee = emp;
                    }
                }
            }

            // Lookups for filters
            ViewBag.FromDate = fDate.ToString("yyyy-MM-dd");
            ViewBag.ToDate = tDate.ToString("yyyy-MM-dd");
            ViewBag.IsAdminOrManager = isAdmin && !mine;
            ViewBag.IsMine = !isAdmin || mine;
            ViewBag.SelectedProjectId = projectId;
            ViewBag.SelectedEmployeeId = targetEmployeeId;

            ViewBag.Projects = await _projectRepository
                .Projects.Where(p => p.IsActive)
                .OrderBy(p => p.ProjectName)
                .Select(p => new { p.Id, p.ProjectName })
                .ToListAsync();

            if (isAdmin && !mine)
            {
                ViewBag.Employees = await _employeeRepository
                    .Employees.Where(e => e.IsActive)
                    .OrderBy(e => e.FirstName)
                    .Select(e => new { e.Id, Name = e.FirstName + " " + e.LastName })
                    .ToListAsync();
            }

            return View(reports);
        }

        [HttpGet]
        public async Task<IActionResult> Create(DateTime? date, bool mine = false)
        {
            var currentEmployee = await GetCurrentEmployeeAsync();
            if (currentEmployee == null)
            {
                TempData["Error"] = "Your user account is not linked to an employee profile.";
                return RedirectToAction(nameof(Index), new { mine });
            }

            var reportDate = (date ?? DateTime.Today).Date;

            // Check if report already exists for this date
            var existing = await _reportService.GetReportByEmployeeAndDateAsync(
                currentEmployee.Id,
                reportDate
            );
            if (existing != null)
            {
                return RedirectToAction(
                    nameof(Edit),
                    new { id = existing.DailyWorkReportId, mine }
                );
            }

            var model = new DailyWorkReportModel
            {
                EmployeeId = currentEmployee.Id,
                ReportDate = reportDate,
                WorkDetails = new List<DailyWorkReportDetailModel>(),
                NextDayPlans = new List<DailyWorkReportPlanModel>(),
            };

            await LoadProjectLookups();
            ViewBag.IsMine = mine;
            ViewBag.EmployeeName = $"{currentEmployee.FirstName} {currentEmployee.LastName}";

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DailyWorkReportModel model, bool mine = false)
        {
            var user = await _userManager.GetUserAsync(User);
            var currentEmployee = await GetCurrentEmployeeAsync();

            if (currentEmployee == null)
            {
                TempData["Error"] = "Your user account is not linked to an employee profile.";
                return RedirectToAction(nameof(Index), new { mine });
            }

            model.EmployeeId = currentEmployee.Id;

            var result = await _reportService.SaveReportAsync(model, user?.Id ?? string.Empty);
            if (!result.Success)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.Error ?? "Failed to save daily work report."
                );
                await LoadProjectLookups();
                ViewBag.IsMine = mine;
                ViewBag.EmployeeName = $"{currentEmployee.FirstName} {currentEmployee.LastName}";
                return View(model);
            }

            TempData["Success"] =
                $"Daily Work Report for {model.ReportDate:dd-MM-yyyy} saved successfully.";
            return RedirectToAction(nameof(Index), new { mine });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, bool mine = false)
        {
            var currentEmployee = await GetCurrentEmployeeAsync();
            var report = await _reportService.GetReportByIdAsync(id);

            if (report == null)
                return NotFound();

            if (
                !IsAdminOrManager()
                && (currentEmployee == null || report.EmployeeId != currentEmployee.Id)
            )
            {
                return Forbid();
            }

            await LoadProjectLookups();
            ViewBag.IsMine = mine;
            ViewBag.EmployeeName =
                currentEmployee != null
                    ? $"{currentEmployee.FirstName} {currentEmployee.LastName}"
                    : "Employee";

            return View(report);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DailyWorkReportModel model, bool mine = false)
        {
            var user = await _userManager.GetUserAsync(User);
            var currentEmployee = await GetCurrentEmployeeAsync();

            if (
                !IsAdminOrManager()
                && (currentEmployee == null || model.EmployeeId != currentEmployee.Id)
            )
            {
                return Forbid();
            }

            var result = await _reportService.SaveReportAsync(model, user?.Id ?? string.Empty);
            if (!result.Success)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.Error ?? "Failed to update daily work report."
                );
                await LoadProjectLookups();
                ViewBag.IsMine = mine;
                return View(model);
            }

            TempData["Success"] =
                $"Daily Work Report for {model.ReportDate:dd-MM-yyyy} updated successfully.";
            return RedirectToAction(nameof(Index), new { mine });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var report = await _reportService.GetReportByIdAsync(id);
            if (report == null)
                return NotFound();

            if (report.Employee == null && report.EmployeeId > 0)
            {
                report.Employee = await _employeeRepository.GetByIdAsync(report.EmployeeId);
            }

            return View(report);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, bool mine = false)
        {
            var user = await _userManager.GetUserAsync(User);
            var success = await _reportService.DeleteReportAsync(
                id,
                user?.Id ?? "",
                IsAdminOrManager()
            );

            if (!success)
            {
                TempData["Error"] = "Unable to delete report or access denied.";
            }
            else
            {
                TempData["Success"] = "Daily work report removed.";
            }

            return RedirectToAction(nameof(Index), new { mine });
        }

        // AJAX endpoint: Imports previous day's plan rows
        [HttpGet]
        public async Task<IActionResult> GetPreviousPlans(DateTime date)
        {
            var currentEmployee = await GetCurrentEmployeeAsync();
            if (currentEmployee == null)
                return Json(new { success = false, message = "Employee not found." });

            var plans = await _reportService.GetPreviousPlansToImportAsync(
                currentEmployee.Id,
                date
            );
            return Json(new { success = true, plans });
        }

        // Export Excel matching company spreadsheet format
        [HttpGet]
        public async Task<IActionResult> ExportExcel(
            DateTime? fromDate,
            DateTime? toDate,
            int? employeeId,
            int? projectId
        )
        {
            var currentEmployee = await GetCurrentEmployeeAsync();
            bool isAdmin = IsAdminOrManager();

            int? targetEmpId = isAdmin ? employeeId : currentEmployee?.Id;
            var reports = await _reportService.GetTeamReportsAsync(
                targetEmpId,
                projectId,
                fromDate,
                toDate
            );

            // Batch-load employee profiles for all reports
            var empIds = reports.Select(r => r.EmployeeId).Distinct().ToList();
            var empMap = empIds.Any()
                ? await _employeeRepository
                    .Employees.Where(e => empIds.Contains(e.Id))
                    .ToDictionaryAsync(e => e.Id)
                : new Dictionary<int, EmployeeModel>();

            foreach (var r in reports)
            {
                if (empMap.TryGetValue(r.EmployeeId, out var emp))
                    r.Employee = emp;
            }

            using var workbook = new XLWorkbook();
            var usedSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!reports.Any())
            {
                var emptySheet = workbook.Worksheets.Add("Daily Work Report");
                emptySheet.Cell(1, 1).Value = "No work reports found for the selected criteria.";
                emptySheet.Columns().AdjustToContents();
            }
            else
            {
                // Group reports by Employee so each employee gets their own sheet
                var employeeGroups = reports
                    .GroupBy(r => r.EmployeeId)
                    .OrderBy(g => g.First().Employee?.FullName ?? $"Employee_{g.Key}");

                foreach (var empGroup in employeeGroups)
                {
                    var employee = empGroup.First().Employee;
                    string rawName = !string.IsNullOrWhiteSpace(employee?.FullName)
                        ? employee.FullName
                        : $"Employee_{empGroup.Key}";

                    string sheetName = SanitizeWorksheetName(rawName, usedSheetNames);
                    var worksheet = workbook.Worksheets.Add(sheetName);

                    int row = 1;

                    // Row 1: Employee Name Badge (A1 = Label, B1:H1 Merged = Value)
                    worksheet.Cell(row, 1).Value = "Employee Name:";
                    worksheet.Cell(row, 1).Style.Font.Bold = true;
                    worksheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(
                        217,
                        225,
                        242
                    ); // Soft Blue-Gray #D9E1F2
                    worksheet.Cell(row, 1).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    worksheet.Cell(row, 1).Style.Alignment.Vertical =
                        XLAlignmentVerticalValues.Center;

                    var empValRange = worksheet.Range(row, 2, row, 8);
                    empValRange.Merge();
                    empValRange.Value =
                        employee != null
                            ? (
                                !string.IsNullOrWhiteSpace(employee.EmployeeCode)
                                    ? $"{employee.FullName} ({employee.EmployeeCode})"
                                    : employee.FullName
                            )
                            : $"Employee #{empGroup.Key}";
                    empValRange.Style.Font.Bold = true;
                    empValRange.Style.Font.FontSize = 11;
                    empValRange.Style.Font.FontColor = XLColor.FromArgb(31, 78, 121); // Navy Blue #1F4E79
                    empValRange.Style.Fill.BackgroundColor = XLColor.FromArgb(242, 242, 242);
                    empValRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    empValRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    empValRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    worksheet.Row(row).Height = 22;

                    row += 2; // Row 2 is Blank

                    // Loop through this specific employee's reports chronologically
                    foreach (var report in empGroup.OrderBy(r => r.ReportDate))
                    {
                        // Row 3: Date Header
                        worksheet.Cell(row, 1).Value = $"Date:-{report.ReportDate:dd-MM-yyyy}";
                        worksheet.Cell(row, 1).Style.Font.Bold = true;
                        worksheet.Cell(row, 1).Style.Alignment.Vertical =
                            XLAlignmentVerticalValues.Center;
                        row += 2; // Row 4 is Blank

                        // Row 5: Daily Work Details (Solid Black Bar across A:H with White Text)
                        var dwdRange = worksheet.Range(row, 1, row, 8);
                        dwdRange.Merge();
                        dwdRange.Value = "Daily Work Details";
                        dwdRange.Style.Font.Bold = true;
                        dwdRange.Style.Font.FontColor = XLColor.White;
                        dwdRange.Style.Fill.BackgroundColor = XLColor.Black;
                        dwdRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        dwdRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        worksheet.Row(row).Height = 22;
                        row += 2; // Row 6 is Blank

                        // Row 7: Today's Work Details (Soft Blue Bar across A:H)
                        var twdRange = worksheet.Range(row, 1, row, 8);
                        twdRange.Merge();
                        twdRange.Value = "Today's Work Details";
                        twdRange.Style.Font.Bold = true;
                        twdRange.Style.Font.FontColor = XLColor.Black;
                        twdRange.Style.Fill.BackgroundColor = XLColor.FromArgb(180, 198, 231); // Soft Blue #B4C6E7
                        twdRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        twdRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        worksheet.Row(row).Height = 20;
                        row++;

                        // Row 8: Table 1 Headers
                        string[] headers1 =
                        {
                            "Sr. No.",
                            "Project Name",
                            "Activity",
                            "Description",
                            "Planned Efforts (hrs)",
                            "Actual Effort (hrs)",
                            "Dependencies",
                            "Issues",
                        };
                        for (int c = 0; c < headers1.Length; c++)
                        {
                            var cell = worksheet.Cell(row, c + 1);
                            cell.Value = headers1[c];
                            cell.Style.Font.Bold = true;
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        }
                        worksheet.Row(row).Height = 20;
                        row++;

                        // Table 1 Rows
                        int sr = 1;
                        foreach (var detail in report.WorkDetails.OrderBy(d => d.SortOrder))
                        {
                            worksheet.Cell(row, 1).Value = sr++;
                            worksheet.Cell(row, 2).Value = detail.ProjectName;
                            worksheet.Cell(row, 3).Value = detail.Activity;
                            worksheet.Cell(row, 4).Value = detail.Description ?? "";
                            worksheet.Cell(row, 5).Value = detail.PlannedEffortHours;
                            worksheet.Cell(row, 6).Value = detail.ActualEffortHours;
                            worksheet.Cell(row, 7).Value = detail.Dependencies ?? "";
                            worksheet.Cell(row, 8).Value = detail.Issues ?? "";

                            worksheet.Cell(row, 1).Style.Alignment.Horizontal =
                                XLAlignmentHorizontalValues.Right;
                            worksheet.Cell(row, 5).Style.Alignment.Horizontal =
                                XLAlignmentHorizontalValues.Right;
                            worksheet.Cell(row, 6).Style.Alignment.Horizontal =
                                XLAlignmentHorizontalValues.Right;

                            for (int c = 1; c <= 8; c++)
                            {
                                worksheet.Cell(row, c).Style.Border.OutsideBorder =
                                    XLBorderStyleValues.Thin;
                                worksheet.Cell(row, c).Style.Alignment.Vertical =
                                    XLAlignmentVerticalValues.Center;
                            }

                            row++;
                        }

                        row++; // Spacing before Next Day's Plan

                        // Next Day's Plan (Soft Blue Bar across A:H)
                        var ndpRange = worksheet.Range(row, 1, row, 8);
                        ndpRange.Merge();
                        ndpRange.Value = "Next Day's Plan";
                        ndpRange.Style.Font.Bold = true;
                        ndpRange.Style.Font.FontColor = XLColor.Black;
                        ndpRange.Style.Fill.BackgroundColor = XLColor.FromArgb(180, 198, 231); // Soft Blue #B4C6E7
                        ndpRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ndpRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        worksheet.Row(row).Height = 20;
                        row++;

                        string[] headers2 =
                        {
                            "Sr. No.",
                            "Project Name",
                            "Activity",
                            "Description",
                            "Estimated Efforts (hrs)",
                            "Priority",
                            "Dependencies",
                            "Issues",
                        };
                        for (int c = 0; c < headers2.Length; c++)
                        {
                            var cell = worksheet.Cell(row, c + 1);
                            cell.Value = headers2[c];
                            cell.Style.Font.Bold = true;
                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        }
                        worksheet.Row(row).Height = 20;
                        row++;

                        int planSr = 1;
                        foreach (var plan in report.NextDayPlans.OrderBy(p => p.SortOrder))
                        {
                            worksheet.Cell(row, 1).Value = planSr++;
                            worksheet.Cell(row, 2).Value = plan.ProjectName;
                            worksheet.Cell(row, 3).Value = plan.Activity;
                            worksheet.Cell(row, 4).Value = plan.Description ?? "";
                            worksheet.Cell(row, 5).Value = plan.EstimatedEffortHours;
                            worksheet.Cell(row, 6).Value = plan.Priority;
                            worksheet.Cell(row, 7).Value = plan.Dependencies ?? "";
                            worksheet.Cell(row, 8).Value = plan.Issues ?? "";

                            worksheet.Cell(row, 1).Style.Alignment.Horizontal =
                                XLAlignmentHorizontalValues.Right;
                            worksheet.Cell(row, 5).Style.Alignment.Horizontal =
                                XLAlignmentHorizontalValues.Right;
                            worksheet.Cell(row, 6).Style.Alignment.Horizontal =
                                XLAlignmentHorizontalValues.Center;

                            for (int c = 1; c <= 8; c++)
                            {
                                worksheet.Cell(row, c).Style.Border.OutsideBorder =
                                    XLBorderStyleValues.Thin;
                                worksheet.Cell(row, c).Style.Alignment.Vertical =
                                    XLAlignmentVerticalValues.Center;
                            }

                            row++;
                        }

                        row += 2; // Spacing before next date block
                    }

                    worksheet.Columns().AdjustToContents();
                    worksheet.Column(1).Width = 8; // Sr. No. compact width matching image
                    if (worksheet.Column(4).Width > 60)
                        worksheet.Column(4).Width = 60;
                    worksheet.Column(1).Width = 8; // Keep 'Sr. No.' compact (matching original company spreadsheet)
                    if (worksheet.Column(4).Width > 60)
                        worksheet.Column(4).Width = 60; // Cap Description column to avoid over-stretching
                }
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            string fileName = $"Daily_Work_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

        private async Task LoadProjectLookups()
        {
            ViewBag.ActiveProjects = await _projectRepository
                .Projects.Where(p => p.IsActive)
                .OrderBy(p => p.ProjectName)
                .Select(p => new { p.Id, p.ProjectName })
                .ToListAsync();
        }

        private static string SanitizeWorksheetName(string name, HashSet<string> existingNames)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = "Employee";

            // Remove characters not allowed in Excel worksheet names: \ / ? * [ ] :
            char[] invalidChars = { '\\', '/', '?', '*', '[', ']', ':' };
            foreach (var c in invalidChars)
            {
                name = name.Replace(c, '_');
            }

            name = name.Trim('\'', ' ');

            // Excel sheet name limit is 31 characters
            if (name.Length > 28)
                name = name.Substring(0, 28);

            string candidate = name;
            int counter = 1;
            while (existingNames.Contains(candidate))
            {
                candidate = $"{name}_{counter++}";
                if (candidate.Length > 31)
                    candidate = candidate.Substring(0, 31);
            }

            existingNames.Add(candidate);
            return candidate;
        }
    }
}
