using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.ViewModels;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using AryamanBMS.Data;
using AryamanBMS.Services.Interfaces;

namespace AryamanBMS.Controllers
{
    [Authorize]
    public class EmployeeController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IDesignationRepository _designationRepository;
        private readonly UserManager<ApplicationUserModel> _userManager;

        private readonly ApplicationDbContext _context;
        private readonly IEmployeeAcademicRepository _employeeAcademicRepository;
        private readonly IEmployeeDocumentRepository _employeeDocumentRepository;
        private readonly IEmployeeDocumentService _employeeDocumentService;

        public EmployeeController(
            IEmployeeRepository employeeRepository,
            IDepartmentRepository departmentRepository,
            IDesignationRepository designationRepository,
            UserManager<ApplicationUserModel> userManager,
            ApplicationDbContext context,
           IEmployeeAcademicRepository employeeAcademicRepository,
           IEmployeeDocumentRepository employeeDocumentRepository,
           IEmployeeDocumentService employeeDocumentService)
        {
            _employeeRepository = employeeRepository;
            _departmentRepository = departmentRepository;
            _designationRepository = designationRepository;
            _userManager = userManager;
            _context = context;
            _employeeAcademicRepository = employeeAcademicRepository;
            _employeeDocumentRepository = employeeDocumentRepository;
            _employeeDocumentService = employeeDocumentService;
        }

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Index(
           string? searchText,
           string statusFilter = "All",
           int page = 1)
        {
            const int pageSize = 5;

            var allEmployees =
                await _employeeRepository.GetAllAsync();

            var employees = allEmployees.AsQueryable();

            ViewBag.TotalEmployees = allEmployees.Count();
            ViewBag.ActiveEmployees =
                allEmployees.Count(e => e.IsActive);
            ViewBag.InactiveEmployees =
                allEmployees.Count(e => !e.IsActive);

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                searchText = searchText.Trim();

                employees = employees.Where(e =>
                    (!string.IsNullOrEmpty(e.EmployeeCode) &&
                     e.EmployeeCode.Contains(
                         searchText,
                         StringComparison.OrdinalIgnoreCase)) ||

                    (!string.IsNullOrEmpty(e.FirstName) &&
                     e.FirstName.Contains(
                         searchText,
                         StringComparison.OrdinalIgnoreCase)) ||

                    (!string.IsNullOrEmpty(e.LastName) &&
                     e.LastName.Contains(
                         searchText,
                         StringComparison.OrdinalIgnoreCase)) ||

                    (!string.IsNullOrEmpty(e.OfficialEmail) &&
                     e.OfficialEmail.Contains(
                         searchText,
                         StringComparison.OrdinalIgnoreCase)));
            }

            if (statusFilter == "Active")
            {
                employees = employees.Where(e => e.IsActive);
            }
            else if (statusFilter == "Inactive")
            {
                employees = employees.Where(e => !e.IsActive);
            }

            employees = employees
                .OrderBy(e => e.EmployeeCode);

            int totalRecords = employees.Count();

            int totalPages = (int)Math.Ceiling(
                totalRecords / (double)pageSize);

            page = page < 1 ? 1 : page;

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var employeePage = employees
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var routeValues =
                new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                routeValues["searchText"] = searchText;
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                routeValues["statusFilter"] = statusFilter;
            }

            var model = new PagedListViewModel<EmployeeModel>
            {
                Items = employeePage,

                Pagination = new PaginationViewModel
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    ControllerName = "Employee",
                    ActionName = nameof(Index),
                    RouteValues = routeValues
                }
            };

            ViewBag.SearchText = searchText;
            ViewBag.StatusFilter = statusFilter;

            return View(model);
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpGet]
        public IActionResult Create()
        {
            LoadDropdowns();

            var model = new EmployeeFormViewModel();

            model.Academics.Add(
                new EmployeeAcademicInputViewModel());

            return View(model);
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
    EmployeeFormViewModel model)
        {
            var employee = model.Employee;

            bool codeExists = await _employeeRepository.Employees
                .AnyAsync(x => x.EmployeeCode == employee.EmployeeCode);

            if (codeExists)
            {
                ModelState.AddModelError(
                    "Employee.EmployeeCode",
                    "Employee Code already exists.");
            }

            if (employee.DateOfBirth > DateTime.Today)
            {
                ModelState.AddModelError(
                    "Employee.DateOfBirth",
                    "Date of Birth cannot be in future.");
            }

            if (employee.JoiningDate > DateTime.Today)
            {
                ModelState.AddModelError(
                    "Employee.JoiningDate",
                    "Joining Date cannot be in future.");
            }

            if (model.Academics.Count(x =>
                    x.IsHighestQualification) > 1)
            {
                ModelState.AddModelError(
                    "Academics",
                    "Only one highest qualification is allowed.");
            }

            if (!ModelState.IsValid)
            {
                LoadDropdowns();
                return View(model);
            }

            var storedFiles = new List<string>();

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                await _employeeRepository.AddAsync(employee);
                await _employeeRepository.SaveAsync();

                var academicPairs =
                    new List<(EmployeeAcademicInputViewModel Input,
                              EmployeeAcademicModel Entity)>();

                foreach (var input in model.Academics)
                {
                    var academic = new EmployeeAcademicModel
                    {
                        EmployeeId = employee.Id,
                        QualificationLevel = input.QualificationLevel,
                        CourseName = input.CourseName,
                        Specialization = input.Specialization,
                        InstituteName = input.InstituteName,
                        BoardOrUniversity = input.BoardOrUniversity,
                        PassingYear = input.PassingYear,
                        ResultType = input.ResultType,
                        Score = input.Score,
                        Grade = input.Grade,
                        IsHighestQualification =
                            input.IsHighestQualification
                    };

                    await _employeeAcademicRepository.AddAsync(academic);

                    academicPairs.Add((input, academic));
                }

                await _employeeAcademicRepository.SaveAsync();

                foreach (var pair in academicPairs)
                {
                    foreach (var file in pair.Input.Documents)
                    {
                        var document =
                            await _employeeDocumentService.SaveAsync(
                                file,
                                employee.EmployeeCode ?? $"EMP{employee.Id}",
                                pair.Input.DocumentType,
                                User.Identity?.Name);

                        document.EmployeeId = employee.Id;
                        document.EmployeeAcademicId = pair.Entity.Id;

                        storedFiles.Add(document.StoragePath);

                        await _employeeDocumentRepository
                            .AddAsync(document);
                    }
                }

                await _employeeDocumentRepository.SaveAsync();
                await transaction.CommitAsync();

                TempData["Success"] =
                    "Employee created successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                foreach (var path in storedFiles)
                {
                    await _employeeDocumentService.DeleteAsync(path);
                }

                ModelState.AddModelError(
                    "",
                    $"Employee could not be created: {ex.Message}");

                LoadDropdowns();

                return View(model);
            }
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var employee =
                await _employeeRepository.GetByIdAsync(id);

            if (employee == null)
            {
                return NotFound();
            }

            var academics =
                await _employeeAcademicRepository.Academics
                    .AsNoTracking()
                    .Include(x => x.Documents)
                    .Where(x => x.EmployeeId == id)
                    .OrderBy(x => x.PassingYear)
                    .Select(x => new EmployeeAcademicInputViewModel
                    {
                        Id = x.Id,
                        QualificationLevel = x.QualificationLevel,
                        CourseName = x.CourseName,
                        Specialization = x.Specialization,
                        InstituteName = x.InstituteName,
                        BoardOrUniversity = x.BoardOrUniversity,
                        PassingYear = x.PassingYear,
                        ResultType = x.ResultType,
                        Score = x.Score,
                        Grade = x.Grade,
                        IsHighestQualification =
                            x.IsHighestQualification,
                        ExistingDocuments = x.Documents.ToList()
                    })
                    .ToListAsync();

            if (!academics.Any())
            {
                academics.Add(
                    new EmployeeAcademicInputViewModel());
            }

            var model = new EmployeeFormViewModel
            {
                Employee = employee,
                Academics = academics
            };

            LoadDropdowns();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(EmployeeFormViewModel model)
        {
            var inputEmployee = model.Employee;

            var existingEmployee =
                await _employeeRepository.GetByIdAsync(inputEmployee.Id);

            if (existingEmployee == null)
            {
                return NotFound();
            }

            bool codeExists = await _employeeRepository.Employees
                .AnyAsync(x =>
                    x.EmployeeCode == inputEmployee.EmployeeCode &&
                    x.Id != inputEmployee.Id);

            if (codeExists)
            {
                ModelState.AddModelError(
                    "Employee.EmployeeCode",
                    "Employee Code already exists.");
            }

            if (inputEmployee.DateOfBirth > DateTime.Today)
            {
                ModelState.AddModelError(
                    "Employee.DateOfBirth",
                    "Date of Birth cannot be in future.");
            }

            if (inputEmployee.JoiningDate > DateTime.Today)
            {
                ModelState.AddModelError(
                    "Employee.JoiningDate",
                    "Joining Date cannot be in future.");
            }

            if (!string.IsNullOrWhiteSpace(inputEmployee.ApplicationUserId))
            {
                bool userMapped = await _employeeRepository.Employees
                    .AnyAsync(x =>
                        x.ApplicationUserId == inputEmployee.ApplicationUserId &&
                        x.Id != inputEmployee.Id);

                if (userMapped)
                {
                    ModelState.AddModelError(
                        "Employee.ApplicationUserId",
                        "Selected user is already assigned.");
                }
            }

            if (model.Academics.Count(x =>
                    x.IsHighestQualification) > 1)
            {
                ModelState.AddModelError(
                    "Academics",
                    "Only one highest qualification is allowed.");
            }

            if (!ModelState.IsValid)
            {
                await ReloadExistingDocumentsAsync(model);
                LoadDropdowns();

                return View(model);
            }

            var newFilePaths = new List<string>();
            var deletedFilePaths = new List<string>();

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Entry(existingEmployee)
                    .CurrentValues
                    .SetValues(inputEmployee);

                var existingAcademics =
                    await _employeeAcademicRepository.Academics
                        .Include(x => x.Documents)
                        .Where(x => x.EmployeeId == inputEmployee.Id)
                        .ToListAsync();

                var submittedIds = model.Academics
                    .Where(x => x.Id.HasValue)
                    .Select(x => x.Id!.Value)
                    .ToHashSet();

                foreach (var academic in existingAcademics
                    .Where(x => !submittedIds.Contains(x.Id)))
                {
                    foreach (var document in academic.Documents)
                    {
                        deletedFilePaths.Add(document.StoragePath);

                        await _employeeDocumentRepository
                            .DeleteAsync(document);
                    }

                    await _employeeAcademicRepository
                        .DeleteAsync(academic);
                }

                var academicPairs =
                    new List<(EmployeeAcademicInputViewModel Input,
                              EmployeeAcademicModel Entity)>();

                foreach (var input in model.Academics)
                {
                    EmployeeAcademicModel academic;

                    if (input.Id.HasValue)
                    {
                        academic = existingAcademics
                            .FirstOrDefault(x => x.Id == input.Id.Value)
                            ?? throw new InvalidOperationException(
                                "Academic record not found.");

                        academic.QualificationLevel =
                            input.QualificationLevel;

                        academic.CourseName = input.CourseName;
                        academic.Specialization = input.Specialization;
                        academic.InstituteName = input.InstituteName;
                        academic.BoardOrUniversity =
                            input.BoardOrUniversity;

                        academic.PassingYear = input.PassingYear;
                        academic.ResultType = input.ResultType;
                        academic.Score = input.Score;
                        academic.Grade = input.Grade;

                        academic.IsHighestQualification =
                            input.IsHighestQualification;

                        await _employeeAcademicRepository
                            .UpdateAsync(academic);
                    }
                    else
                    {
                        academic = new EmployeeAcademicModel
                        {
                            EmployeeId = inputEmployee.Id,
                            QualificationLevel =
                                input.QualificationLevel,
                            CourseName = input.CourseName,
                            Specialization = input.Specialization,
                            InstituteName = input.InstituteName,
                            BoardOrUniversity =
                                input.BoardOrUniversity,
                            PassingYear = input.PassingYear,
                            ResultType = input.ResultType,
                            Score = input.Score,
                            Grade = input.Grade,
                            IsHighestQualification =
                                input.IsHighestQualification
                        };

                        await _employeeAcademicRepository
                            .AddAsync(academic);
                    }

                    academicPairs.Add((input, academic));
                }

                await _employeeAcademicRepository.SaveAsync();

                foreach (var pair in academicPairs)
                {
                    foreach (var file in pair.Input.Documents)
                    {
                        var document =
                            await _employeeDocumentService.SaveAsync(
                                file,
                                existingEmployee.EmployeeCode
                                    ?? $"EMP{existingEmployee.Id}",
                                pair.Input.DocumentType,
                                User.Identity?.Name);

                        document.EmployeeId = existingEmployee.Id;
                        document.EmployeeAcademicId = pair.Entity.Id;

                        newFilePaths.Add(document.StoragePath);

                        await _employeeDocumentRepository
                            .AddAsync(document);
                    }
                }

                await _employeeDocumentRepository.SaveAsync();
                await transaction.CommitAsync();

                foreach (var path in deletedFilePaths)
                {
                    await _employeeDocumentService.DeleteAsync(path);
                }

                TempData["Success"] =
                    "Employee updated successfully.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = existingEmployee.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                foreach (var path in newFilePaths)
                {
                    await _employeeDocumentService.DeleteAsync(path);
                }

                ModelState.AddModelError(
                    "",
                    $"Employee could not be updated: {ex.Message}");

                await ReloadExistingDocumentsAsync(model);
                LoadDropdowns();

                return View(model);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,HR")]
        public IActionResult Delete(int id)
        {
            var employee = _employeeRepository.Employees
                .FirstOrDefault(e => e.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee =
                await _employeeRepository.GetByIdAsync(id);

            if (employee == null)
            {
                return NotFound();
            }

            employee.IsActive = false;

            await _employeeRepository.UpdateAsync(employee);
            await _employeeRepository.SaveAsync();

            TempData["Success"] =
                "Employee deactivated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public JsonResult GetDesignations(int departmentId)
        {
            var designations =
                _designationRepository.Designations
                .Where(d => d.DepartmentId == departmentId)
                .Select(d => new
                {
                    id = d.Id,
                    designationName = d.DesignationName
                })
                .ToList();

            return Json(designations);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Details(int id)
        {
            var employee =
                await _employeeRepository.GetDetailsAsync(id);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        private void LoadDropdowns()
        {
            ViewBag.Departments =
                _departmentRepository.Departments
                .OrderBy(d => d.DepartmentName)
                .ToList();

            ViewBag.Designations =
                _designationRepository.Designations
                .OrderBy(d => d.DesignationName)
                .ToList();

            ViewBag.Users = _userManager.Users
                .Where(x => x.IsActive)
                .OrderBy(x => x.FullName)
                .ToList();
        }

        // Excel Export
        [HttpGet]
        [Authorize(Roles = "Admin,HR")]
        public IActionResult Export()
        {
            var employees = _employeeRepository.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .OrderBy(e => e.EmployeeCode)
                .ToList();

            using var workbook = new XLWorkbook();

            var worksheet = workbook.Worksheets.Add("Employees");

            worksheet.Cell("A1").Value = "Employee Code";
            worksheet.Cell("B1").Value = "Employee Name";
            worksheet.Cell("C1").Value = "Department";
            worksheet.Cell("D1").Value = "Designation";
            worksheet.Cell("E1").Value = "Mobile Number";
            worksheet.Cell("F1").Value = "Official Email";
            worksheet.Cell("G1").Value = "Joining Date";
            worksheet.Cell("H1").Value = "Status";

            var headerRange = worksheet.Range("A1:H1");

            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor =
                XLColor.LightBlue;

            worksheet.Cell(1, 1).Value = "Employee Code";
            worksheet.Cell(1, 2).Value = "Employee Name";
            worksheet.Cell(1, 3).Value = "Department";
            worksheet.Cell(1, 4).Value = "Designation";
            worksheet.Cell(1, 5).Value = "Mobile Number";
            worksheet.Cell(1, 6).Value = "Official Email";
            worksheet.Cell(1, 7).Value = "Joining Date";
            worksheet.Cell(1, 8).Value = "Status";

            int row = 2;

            foreach (var employee in employees)
            {
                worksheet.Cell(row, 1).Value =
                    employee.EmployeeCode;

                worksheet.Cell(row, 2).Value =
                    $"{employee.FirstName} {employee.LastName}";

                worksheet.Cell(row, 3).Value =
                    employee.Department?.DepartmentName;

                worksheet.Cell(row, 4).Value =
                    employee.Designation?.DesignationName;

                worksheet.Cell(row, 5).Value =
                    employee.MobileNumber;

                worksheet.Cell(row, 6).Value =
                    employee.OfficialEmail;

                worksheet.Cell(row, 7).Value =
                    employee.JoiningDate;

                worksheet.Cell(row, 8).Value =
                    employee.IsActive ? "Active" : "Inactive";

                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Employees_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }

        [Authorize(Roles = "Admin,HR")]
        public IActionResult Dashboard()
        {
            var totalEmployees =
                _employeeRepository.Employees.Count();

            var activeEmployees =
                _employeeRepository.Employees.Count(x => x.IsActive);

            var inactiveEmployees =
                _employeeRepository.Employees.Count(x => !x.IsActive);

            var recentEmployees =
                _employeeRepository.Employees
                .OrderByDescending(x => x.Id)
                .Take(5)
                .ToList();

            ViewBag.TotalEmployees = totalEmployees;
            ViewBag.ActiveEmployees = activeEmployees;
            ViewBag.InactiveEmployees = inactiveEmployees;
            ViewBag.RecentEmployees = recentEmployees;

            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _employeeRepository.Employees
                .Include(x => x.Department)
                .Include(x => x.Designation)
                .Include(x => x.ApplicationUser)
                .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);

            if (employee == null)
            {
                TempData["Error"] = "Employee profile not found.";

                return RedirectToAction("Index", "Attendance");
            }

            return View(employee);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var document = await _employeeDocumentRepository.Documents
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (document == null)
            {
                return NotFound();
            }

            string filePath =
                _employeeDocumentService.GetAbsolutePath(
                    document.StoragePath);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            return PhysicalFile(
                filePath,
                document.ContentType ?? "application/octet-stream",
                document.OriginalFileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> DeleteDocument(
          int id,
          int employeeId)
        {
            var document = await _employeeDocumentRepository.Documents
                .FirstOrDefaultAsync(x => x.Id == id);

            if (document == null)
            {
                return NotFound();
            }

            string storagePath = document.StoragePath;

            await _employeeDocumentRepository.DeleteAsync(document);
            await _employeeDocumentRepository.SaveAsync();

            await _employeeDocumentService.DeleteAsync(storagePath);

            TempData["Success"] =
                "Document deleted successfully.";

            return RedirectToAction(
                nameof(Edit),
                new { id = employeeId });
        }

        private async Task ReloadExistingDocumentsAsync(EmployeeFormViewModel model)
        {
            foreach (var academic in model.Academics
                .Where(x => x.Id.HasValue))
            {
                academic.ExistingDocuments =
                    await _employeeDocumentRepository.Documents
                        .AsNoTracking()
                        .Where(x =>
                            x.EmployeeId == model.Employee.Id &&
                            x.EmployeeAcademicId == academic.Id)
                        .ToListAsync();
            }
        }
    }
}