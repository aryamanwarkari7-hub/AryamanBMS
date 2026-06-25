using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interface;
using AryamanBMS.ViewModels;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        private readonly ILocationRepository _locationRepository;
        private readonly IEmployeePreviousEmploymentRepository
    _employeePreviousEmploymentRepository;

        public EmployeeController(
            IEmployeeRepository employeeRepository,
            IDepartmentRepository departmentRepository,
            IDesignationRepository designationRepository,
            UserManager<ApplicationUserModel> userManager,
            ApplicationDbContext context,
            IEmployeeAcademicRepository employeeAcademicRepository,
            IEmployeeDocumentRepository employeeDocumentRepository,
            IEmployeeDocumentService employeeDocumentService,
            ILocationRepository locationRepository,
            IEmployeePreviousEmploymentRepository employeePreviousEmploymentRepository)
        {
            _employeeRepository = employeeRepository;
            _departmentRepository = departmentRepository;
            _designationRepository = designationRepository;
            _userManager = userManager;
            _context = context;
            _employeeAcademicRepository = employeeAcademicRepository;
            _employeeDocumentRepository = employeeDocumentRepository;
            _employeeDocumentService = employeeDocumentService;
            _locationRepository = locationRepository;
            _employeePreviousEmploymentRepository = employeePreviousEmploymentRepository;
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

                    (!string.IsNullOrEmpty(e.MiddleName) &&
                     e.MiddleName.Contains(
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
        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();

            var model = new EmployeeFormViewModel
            {
                Employee = new EmployeeModel
                {
                    JoiningDate = DateTime.Today,
                    IsActive = true
                },

                Academics = new List<EmployeeAcademicInputViewModel>
                {
                    new()
                },

                StatutoryDocuments = GetStatutoryDocumentInputs(),

                JoiningDocuments = GetJoiningDocumentInputs(),

                PreviousEmployments =
                new List<EmployeePreviousEmploymentInputViewModel>
                {
                    new()
                }
            };

            return View(model);
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeFormViewModel model)
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

            ValidateFixedPdfDocuments(model.StatutoryDocuments,
                           "StatutoryDocuments");

            ValidateFixedPdfDocuments(model.JoiningDocuments,
                             "JoiningDocuments");

            for (int i = 0; i < model.Academics.Count; i++)
            {
                var academic = model.Academics[i];

                bool isNewQualification = !academic.Id.HasValue;

                bool hasNewDocument =
                    academic.Documents != null &&
                    academic.Documents.Any(x => x != null && x.Length > 0);

                if (isNewQualification && !hasNewDocument)
                {
                    ModelState.AddModelError(
                        $"Academics[{i}].Documents",
                        "Please upload a document for the new qualification.");
                }
            }

            model.PreviousEmployments ??=
            new List<EmployeePreviousEmploymentInputViewModel>();

            ValidatePreviousEmployments(
                model.PreviousEmployments);

            if (!ModelState.IsValid)
            {
                model.StatutoryDocuments =
                    RestoreDocumentInputs(
                        model.StatutoryDocuments,
                        GetStatutoryDocumentInputs());

                model.JoiningDocuments =
                    RestoreDocumentInputs(
                        model.JoiningDocuments,
                        GetJoiningDocumentInputs());

                await LoadDropdownsAsync();

                return View(model);
            }

            employee.EsicNo = string.IsNullOrWhiteSpace(employee.EsicNo) ? null
        : employee.EsicNo.Trim();

            var storedFiles = new List<string>();

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                await _employeeRepository.AddAsync(employee);
                await _employeeRepository.SaveAsync();

                var previousEmploymentPairs =
                new List<(
                    EmployeePreviousEmploymentInputViewModel Input,
                    EmployeePreviousEmploymentModel Entity)>();

                foreach (var input in model.PreviousEmployments)
                {
                    bool rowIsEmpty =
                        string.IsNullOrWhiteSpace(input.CompanyName) &&
                        !input.StartDate.HasValue &&
                        !input.EndDate.HasValue &&
                        input.ExperienceLetter == null &&
                        input.RelievingLetter == null;

                    if (rowIsEmpty)
                    {
                        continue;
                    }

                    var previousEmployment =
                        new EmployeePreviousEmploymentModel
                        {
                            EmployeeId = employee.Id,
                            CompanyName = input.CompanyName.Trim(),
                            Designation = input.Designation,
                            Department = input.Department,
                            EmploymentType = input.EmploymentType,
                            StartDate = input.StartDate!.Value,
                            EndDate = input.EndDate!.Value,
                            LastSalary = input.LastSalary,
                            ReasonForLeaving = input.ReasonForLeaving,
                            CompanyAddress = input.CompanyAddress,
                            CompanyCity = input.CompanyCity,
                            CompanyState = input.CompanyState,
                            CompanyPinCode = input.CompanyPinCode,
                            CompanyWebsite = input.CompanyWebsite,
                            HRContactName = input.HRContactName,
                            HRContactEmail = input.HRContactEmail,
                            HRContactNumber = input.HRContactNumber,
                            CreatedOn = DateTime.Now
                        };

                    await _employeePreviousEmploymentRepository
                        .AddAsync(previousEmployment);

                    previousEmploymentPairs.Add(
                        (input, previousEmployment));
                }

                await _employeePreviousEmploymentRepository.SaveAsync();

                int previousEmploymentDocumentCount = 0;

                foreach (var pair in previousEmploymentPairs)
                {
                    previousEmploymentDocumentCount +=
                        await SavePreviousEmploymentDocumentAsync(
                            pair.Input.ExperienceLetter,
                            employee,
                            pair.Entity.Id,
                            "Experience Letter",
                            storedFiles);

                    previousEmploymentDocumentCount +=
                        await SavePreviousEmploymentDocumentAsync(
                            pair.Input.RelievingLetter,
                            employee,
                            pair.Entity.Id,
                            "Relieving Letter",
                            storedFiles);
                }

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
                    foreach (var file in pair.Input.Documents ?? new List<IFormFile>())
                    {
                        if (file == null || file.Length == 0)
                        {
                            continue;
                        }

                        var document =
                            await _employeeDocumentService.SaveAsync(
                                file,
                                employee.EmployeeCode ?? $"EMP{employee.Id}",
                                pair.Input.DocumentType,
                                User.Identity?.Name);

                        document.EmployeeId = employee.Id;
                        document.EmployeeAcademicId = pair.Entity.Id;
                        document.DocumentCategory = "Academic";

                        storedFiles.Add(document.StoragePath);

                        await _employeeDocumentRepository
                            .AddAsync(document);
                    }
                }

                int statutoryUploadCount =
                       await SaveFixedDocumentsAsync(
                           model.StatutoryDocuments,
                           employee,
                           "Statutory",
                           storedFiles);

                int joiningUploadCount =
                        await SaveFixedDocumentsAsync(
                            model.JoiningDocuments,
                            employee,
                            "Joining",
                            storedFiles);

                await _employeeDocumentRepository.SaveAsync();
                await transaction.CommitAsync();

                int totalUploadedDocuments =
                 academicPairs.Sum(x =>
                     x.Input.Documents?.Count(f =>
                         f != null && f.Length > 0) ?? 0)
                 + statutoryUploadCount
                 + joiningUploadCount
                 + previousEmploymentDocumentCount;

                TempData["Success"] =
                    totalUploadedDocuments > 0
                        ? $"Employee created successfully. {totalUploadedDocuments} document(s) uploaded successfully."
                        : "Employee created successfully.";

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

                await LoadDropdownsAsync();

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

            var fixedDocuments = await _context.EmployeeDocuments
                 .AsNoTracking()
                 .Where(x =>
                     x.EmployeeId == id &&
                     x.EmployeeAcademicId == null)
                 .ToListAsync();

            var previousEmployments =
            await _employeePreviousEmploymentRepository
                .PreviousEmployments
                .AsNoTracking()
                .Include(x => x.Documents)
                .Where(x => x.EmployeeId == id)
                .OrderByDescending(x => x.EndDate)
                .Select(x =>
                    new EmployeePreviousEmploymentInputViewModel
                    {
                        Id = x.Id,
                        CompanyName = x.CompanyName,
                        Designation = x.Designation,
                        Department = x.Department,
                        EmploymentType = x.EmploymentType,
                        StartDate = x.StartDate,
                        EndDate = x.EndDate,
                        LastSalary = x.LastSalary,
                        ReasonForLeaving = x.ReasonForLeaving,
                        CompanyAddress = x.CompanyAddress,
                        CompanyCity = x.CompanyCity,
                        CompanyState = x.CompanyState,
                        CompanyPinCode = x.CompanyPinCode,
                        CompanyWebsite = x.CompanyWebsite,
                        HRContactName = x.HRContactName,
                        HRContactEmail = x.HRContactEmail,
                        HRContactNumber = x.HRContactNumber,

                        ExistingExperienceLetter =
                            x.Documents.FirstOrDefault(d =>
                                d.DocumentType == "Experience Letter"),

                        ExistingRelievingLetter =
                            x.Documents.FirstOrDefault(d =>
                                d.DocumentType == "Relieving Letter")
                    })
                .ToListAsync();

            var model = new EmployeeFormViewModel
            {
                Employee = employee,

                Academics = academics,

                StatutoryDocuments =
                BuildFixedDocumentInputs(
                    GetStatutoryDocumentInputs(),
                    fixedDocuments.Where(x =>
                        x.DocumentCategory == "Statutory")),

                JoiningDocuments =
                BuildFixedDocumentInputs(
                    GetJoiningDocumentInputs(),
                    fixedDocuments.Where(x =>
                        x.DocumentCategory == "Joining")),

                PreviousEmployments = previousEmployments
            };

            await LoadDropdownsAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(EmployeeFormViewModel model)
        {
            var inputEmployee = model.Employee;

            model.PreviousEmployments ??=
            new List<EmployeePreviousEmploymentInputViewModel>();

            model.RemovedPreviousEmploymentIds ??=
                new List<int>();

            var removedPreviousEmploymentIds =
                model.RemovedPreviousEmploymentIds.ToHashSet();

            model.PreviousEmployments =
                model.PreviousEmployments
                    .Where(x =>
                        !x.Id.HasValue ||
                        !removedPreviousEmploymentIds.Contains(x.Id.Value))
                    .ToList();

            model.Academics ??=
                new List<EmployeeAcademicInputViewModel>();

            model.RemovedAcademicIds ??=
                new List<int>();

            var removedAcademicIds =
                model.RemovedAcademicIds.ToHashSet();

            model.Academics = model.Academics
                .Where(x =>
                    !x.Id.HasValue ||
                    !removedAcademicIds.Contains(x.Id.Value))
                .ToList();

            model.Academics = model.Academics
                .Select((academic, index) => new
                {
                    Academic = academic,
                    Index = index
                })
                .GroupBy(x =>
                    x.Academic.Id.HasValue
                        ? $"Existing-{x.Academic.Id.Value}"
                        : $"New-{x.Index}")
                .Select(x => x.First().Academic)
                .ToList();

            model.StatutoryDocuments ??=
                new List<EmployeeDocumentInputViewModel>();

            model.JoiningDocuments ??=
                new List<EmployeeDocumentInputViewModel>();

            for (int i = 0; i < model.Academics.Count; i++)
            {
                ModelState.Remove(
                    $"Academics[{i}].ExistingDocuments");
            }

            for (int i = 0; i < model.StatutoryDocuments.Count; i++)
            {
                ModelState.Remove(
                    $"StatutoryDocuments[{i}].ExistingDocument");
            }

            for (int i = 0; i < model.JoiningDocuments.Count; i++)
            {
                ModelState.Remove(
                    $"JoiningDocuments[{i}].ExistingDocument");
            }

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

            model.Academics ??= new List<EmployeeAcademicInputViewModel>();

            int highestQualificationCount =
               model.Academics.Count(x =>
               x.IsHighestQualification);

            if (highestQualificationCount > 1)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Only one highest qualification is allowed.");
            }
            ValidateFixedPdfDocuments(model.StatutoryDocuments,
                  "StatutoryDocuments");

            ValidateFixedPdfDocuments(model.JoiningDocuments,
                "JoiningDocuments");

            ValidatePreviousEmployments(model.PreviousEmployments);

            if (!ModelState.IsValid)
            {
                model.Academics = model.Academics
                    .Where(x =>
                        !x.Id.HasValue ||
                        !removedAcademicIds.Contains(x.Id.Value))
                    .ToList();

                await ReloadExistingDocumentsAsync(model);
                await LoadExistingFixedDocumentsAsync(model);
                await ReloadExistingPreviousEmploymentDocumentsAsync(model);
                await LoadDropdownsAsync();

                return View(model);
            }

            var newFilePaths = new List<string>();
            var deletedFilePaths = new List<string>();

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                inputEmployee.EsicNo =
                  string.IsNullOrWhiteSpace(inputEmployee.EsicNo)
                      ? null
                      : inputEmployee.EsicNo.Trim();

                _context.Entry(existingEmployee)
                    .CurrentValues
                    .SetValues(inputEmployee);

                var existingAcademics =
                    await _employeeAcademicRepository.Academics
                        .Include(x => x.Documents)
                        .Where(x => x.EmployeeId == inputEmployee.Id)
                        .ToListAsync();

                var existingPreviousEmployments =
    await _employeePreviousEmploymentRepository
        .PreviousEmployments
        .Include(x => x.Documents)
        .Where(x =>
            x.EmployeeId == inputEmployee.Id)
        .ToListAsync();

                var submittedPreviousEmploymentIds =
                    model.PreviousEmployments
                        .Where(x => x.Id.HasValue)
                        .Select(x => x.Id!.Value)
                        .ToHashSet();

                foreach (var previousEmployment in
                    existingPreviousEmployments.Where(x =>
                        !submittedPreviousEmploymentIds.Contains(x.Id)))
                {
                    foreach (var document in previousEmployment.Documents)
                    {
                        if (!string.IsNullOrWhiteSpace(document.StoragePath))
                        {
                            deletedFilePaths.Add(document.StoragePath);
                        }

                        await _employeeDocumentRepository
                            .DeleteAsync(document);
                    }

                    await _employeePreviousEmploymentRepository
                        .DeleteAsync(previousEmployment);
                }

                var previousEmploymentPairs =
    new List<(
        EmployeePreviousEmploymentInputViewModel Input,
        EmployeePreviousEmploymentModel Entity)>();

                foreach (var input in model.PreviousEmployments)
                {
                    bool rowIsEmpty =
                        string.IsNullOrWhiteSpace(input.CompanyName) &&
                        !input.StartDate.HasValue &&
                        !input.EndDate.HasValue &&
                        input.ExperienceLetter == null &&
                        input.RelievingLetter == null;

                    if (rowIsEmpty)
                    {
                        continue;
                    }

                    EmployeePreviousEmploymentModel previousEmployment;

                    if (input.Id.HasValue)
                    {
                        previousEmployment =
                            existingPreviousEmployments
                                .FirstOrDefault(x => x.Id == input.Id.Value)
                            ?? throw new InvalidOperationException(
                                "Previous employment record not found.");

                        previousEmployment.CompanyName =
                            input.CompanyName.Trim();

                        previousEmployment.Designation =
                            input.Designation;

                        previousEmployment.Department =
                            input.Department;

                        previousEmployment.EmploymentType =
                            input.EmploymentType;

                        previousEmployment.StartDate =
                            input.StartDate!.Value;

                        previousEmployment.EndDate =
                            input.EndDate!.Value;

                        previousEmployment.LastSalary =
                            input.LastSalary;

                        previousEmployment.ReasonForLeaving =
                            input.ReasonForLeaving;

                        previousEmployment.CompanyAddress =
                            input.CompanyAddress;

                        previousEmployment.CompanyCity =
                            input.CompanyCity;

                        previousEmployment.CompanyState =
                            input.CompanyState;

                        previousEmployment.CompanyPinCode =
                            input.CompanyPinCode;

                        previousEmployment.CompanyWebsite =
                            input.CompanyWebsite;

                        previousEmployment.HRContactName =
                            input.HRContactName;

                        previousEmployment.HRContactEmail =
                            input.HRContactEmail;

                        previousEmployment.HRContactNumber =
                            input.HRContactNumber;

                        previousEmployment.UpdatedOn =
                            DateTime.Now;

                        await _employeePreviousEmploymentRepository
                            .UpdateAsync(previousEmployment);
                    }
                    else
                    {
                        previousEmployment =
                            new EmployeePreviousEmploymentModel
                            {
                                EmployeeId = inputEmployee.Id,

                                CompanyName =
                                    input.CompanyName.Trim(),

                                Designation =
                                    input.Designation,

                                Department =
                                    input.Department,

                                EmploymentType =
                                    input.EmploymentType,

                                StartDate =
                                    input.StartDate!.Value,

                                EndDate =
                                    input.EndDate!.Value,

                                LastSalary =
                                    input.LastSalary,

                                ReasonForLeaving =
                                    input.ReasonForLeaving,

                                CompanyAddress =
                                    input.CompanyAddress,

                                CompanyCity =
                                    input.CompanyCity,

                                CompanyState =
                                    input.CompanyState,

                                CompanyPinCode =
                                    input.CompanyPinCode,

                                CompanyWebsite =
                                    input.CompanyWebsite,

                                HRContactName =
                                    input.HRContactName,

                                HRContactEmail =
                                    input.HRContactEmail,

                                HRContactNumber =
                                    input.HRContactNumber,

                                CreatedOn =
                                    DateTime.Now
                            };

                        await _employeePreviousEmploymentRepository
                            .AddAsync(previousEmployment);
                    }

                    previousEmploymentPairs.Add(
                        (input, previousEmployment));
                }

                await _employeePreviousEmploymentRepository
                    .SaveAsync();

                int previousEmploymentDocumentCount = 0;

                foreach (var pair in previousEmploymentPairs)
                {
                    previousEmploymentDocumentCount +=
                        await ReplacePreviousEmploymentDocumentAsync(
                            pair.Input.ExperienceLetter,
                            existingEmployee,
                            pair.Entity.Id,
                            "Experience Letter",
                            newFilePaths,
                            deletedFilePaths);

                    previousEmploymentDocumentCount +=
                        await ReplacePreviousEmploymentDocumentAsync(
                            pair.Input.RelievingLetter,
                            existingEmployee,
                            pair.Entity.Id,
                            "Relieving Letter",
                            newFilePaths,
                            deletedFilePaths);
                }

                var submittedIds = model.Academics
                     .Where(x =>
                         x.Id.HasValue &&
                         !removedAcademicIds.Contains(x.Id.Value))
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

                    var uploadedFiles =
                       pair.Input.Documents ??
                       new List<IFormFile>();

                    foreach (var file in
                        pair.Input.Documents ?? new List<IFormFile>())
                    {
                        if (file == null || file.Length == 0)
                        {
                            continue;
                        }

                        var document =
                            await _employeeDocumentService.SaveAsync(
                                file,
                                existingEmployee.EmployeeCode
                                    ?? $"EMP{existingEmployee.Id}",
                                pair.Input.DocumentType,
                                User.Identity?.Name);

                        document.EmployeeId =
                            existingEmployee.Id;

                        document.EmployeeAcademicId =
                            pair.Entity.Id;

                        document.DocumentCategory =
                            "Academic";

                        newFilePaths.Add(
                            document.StoragePath);

                        await _employeeDocumentRepository
                            .AddAsync(document);
                    }
                }

                int statutoryUploadCount =
                    await ReplaceFixedDocumentsAsync(
                        model.StatutoryDocuments,
                        existingEmployee,
                        "Statutory",
                        newFilePaths,
                        deletedFilePaths);

                int joiningUploadCount =
                    await ReplaceFixedDocumentsAsync(
                        model.JoiningDocuments,
                        existingEmployee,
                        "Joining",
                        newFilePaths,
                        deletedFilePaths);

                await _employeeRepository.SaveAsync();
                await _employeeAcademicRepository.SaveAsync();
                await _employeeDocumentRepository.SaveAsync();
                await transaction.CommitAsync();

                foreach (var path in deletedFilePaths)
                {
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        await _employeeDocumentService.DeleteAsync(path);
                    }
                }

                int academicUploadCount =
                     academicPairs.Sum(x =>
                         x.Input.Documents?.Count(file =>
                             file != null && file.Length > 0) ?? 0);

                int totalUploadedDocuments =
                    academicUploadCount +
                    statutoryUploadCount +
                    joiningUploadCount +
                    previousEmploymentDocumentCount;

                TempData["Success"] =
                    totalUploadedDocuments > 0
                        ? $"Employee updated successfully. {totalUploadedDocuments} document(s) uploaded successfully."
                        : "Employee updated successfully.";

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
                await LoadExistingFixedDocumentsAsync(model);
                await ReloadExistingPreviousEmploymentDocumentsAsync(model);
                await LoadDropdownsAsync();

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

            var academics =
                await _employeeAcademicRepository.Academics
                    .AsNoTracking()
                    .Include(x => x.Documents)
                    .Where(x => x.EmployeeId == id)
                    .OrderByDescending(x => x.PassingYear)
                    .ToListAsync();

            var fixedDocuments =
                await _employeeDocumentRepository.Documents
                    .AsNoTracking()
                    .Where(x =>
                        x.EmployeeId == id &&
                        x.EmployeeAcademicId == null &&
                        x.EmployeePreviousEmploymentId == null)
                    .ToListAsync();

            var previousEmployments =
                await _employeePreviousEmploymentRepository
                    .PreviousEmployments
                    .AsNoTracking()
                    .Include(x => x.Documents)
                    .Where(x => x.EmployeeId == id)
                    .OrderByDescending(x => x.EndDate)
                    .ToListAsync();

            var model = new EmployeeDetailsViewModel
            {
                Employee = employee,
                Academics = academics,

                StatutoryDocuments = fixedDocuments
                    .Where(x => x.DocumentCategory == "Statutory")
                    .ToList(),

                JoiningDocuments = fixedDocuments
                    .Where(x => x.DocumentCategory == "Joining")
                    .ToList(),

                PreviousEmployments = previousEmployments
            };

            return View(model);
        }

        private async Task LoadDropdownsAsync()
        {
            ViewBag.Departments =
                _departmentRepository.Departments
                    .OrderBy(d => d.DepartmentName)
                    .ToList();

            ViewBag.Designations =
                _designationRepository.Designations
                    .OrderBy(d => d.DesignationName)
                    .ToList();

            ViewBag.Users =
                _userManager.Users
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.FullName)
                    .ToList();

            ViewBag.States =
                await _locationRepository
                    .GetActiveStatesAsync();
        }

        private static List<EmployeeDocumentInputViewModel>
    GetStatutoryDocumentInputs()
        {
            return new List<EmployeeDocumentInputViewModel>
    {
        new() { DocumentType = "Aadhaar Card" },
        new() { DocumentType = "PAN Card" },
        new() { DocumentType = "UAN Document" },
        new() { DocumentType = "ESIC Document" }
    };
        }

        private static List<EmployeeDocumentInputViewModel>
            GetJoiningDocumentInputs()
        {
            return new List<EmployeeDocumentInputViewModel>
    {
        new() { DocumentType = "Resume / CV" },
        new() { DocumentType = "Offer Letter" },
        new() { DocumentType = "Appointment Letter" },
        new() { DocumentType = "Joining Form" },
        new() { DocumentType = "Relieving Letter" },
        new() { DocumentType = "Experience Letter" },
        new() { DocumentType = "Salary Slip" },
        new() { DocumentType = "Bank Proof" },
        new() { DocumentType = "Address Proof" },
        new() { DocumentType = "Signed NDA" },
        new() { DocumentType = "Other Joining Document" }
    };
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
    employee.FullName;

                worksheet.Cell(row, 3).Value =
                    employee.Department?.DepartmentName;

                worksheet.Cell(row, 4).Value =
                    employee.Designation?.DesignationName;

                worksheet.Cell(row, 5).Value =
                    employee.MobileNumber;

                worksheet.Cell(row, 6).Value =
                    employee.OfficialEmail;

                if (employee.JoiningDate >= new DateTime(1900, 1, 1))
                {
                    worksheet.Cell(row, 7).Value =
                        employee.JoiningDate;

                    worksheet.Cell(row, 7).Style.DateFormat.Format =
                        "dd-MM-yyyy";
                }
                else
                {
                    worksheet.Cell(row, 7).Value = "";
                }

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

        private static List<EmployeeDocumentInputViewModel>
    RestoreDocumentInputs(
        List<EmployeeDocumentInputViewModel>? submittedDocuments,
        List<EmployeeDocumentInputViewModel> defaultDocuments)
        {
            submittedDocuments ??=
                new List<EmployeeDocumentInputViewModel>();

            foreach (var defaultDocument in defaultDocuments)
            {
                var submittedDocument =
                    submittedDocuments.FirstOrDefault(x =>
                        x.DocumentType == defaultDocument.DocumentType);

                if (submittedDocument == null)
                {
                    submittedDocuments.Add(defaultDocument);
                }
            }

            return submittedDocuments;
        }

        private void ValidateFixedPdfDocuments(
    List<EmployeeDocumentInputViewModel>? documents,
    string modelKey)
        {
            if (documents == null)
            {
                return;
            }

            const long maximumFileSize = 5 * 1024 * 1024;

            for (int i = 0; i < documents.Count; i++)
            {
                var file = documents[i].File;

                if (file == null || file.Length == 0)
                {
                    continue;
                }

                var extension =
                    Path.GetExtension(file.FileName).ToLowerInvariant();

                if (extension != ".pdf")
                {
                    ModelState.AddModelError(
                        $"{modelKey}[{i}].File",
                        $"{documents[i].DocumentType} must be a PDF file.");
                }

                if (!string.Equals(
                        file.ContentType,
                        "application/pdf",
                        StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(
                        $"{modelKey}[{i}].File",
                        $"{documents[i].DocumentType} has an invalid file type.");
                }

                if (file.Length > maximumFileSize)
                {
                    ModelState.AddModelError(
                        $"{modelKey}[{i}].File",
                        $"{documents[i].DocumentType} cannot exceed 5 MB.");
                }
            }
        }

        private async Task<int> SaveFixedDocumentsAsync(
    List<EmployeeDocumentInputViewModel>? documentInputs,
    EmployeeModel employee,
    string documentCategory,
    List<string> storedFiles)
        {
            if (documentInputs == null)
            {
                return 0;
            }

            int uploadedCount = 0;

            foreach (var input in documentInputs)
            {
                var file = input.File;

                if (file == null || file.Length == 0)
                {
                    continue;
                }

                var document =
                    await _employeeDocumentService.SaveAsync(
                        file,
                        employee.EmployeeCode ?? $"EMP{employee.Id}",
                        input.DocumentType,
                        User.Identity?.Name);

                document.EmployeeId = employee.Id;
                document.EmployeeAcademicId = null;
                document.DocumentCategory = documentCategory;

                storedFiles.Add(document.StoragePath);

                await _employeeDocumentRepository.AddAsync(document);

                uploadedCount++;
            }

            return uploadedCount;
        }

        private static List<EmployeeDocumentInputViewModel>
    BuildFixedDocumentInputs(
        List<EmployeeDocumentInputViewModel> defaults,
        IEnumerable<EmployeeDocumentModel> existingDocuments)
        {
            var existingList = existingDocuments.ToList();

            foreach (var item in defaults)
            {
                item.ExistingDocument =
                    existingList.FirstOrDefault(x =>
                        x.DocumentType == item.DocumentType);
            }

            return defaults;
        }

        private async Task LoadExistingFixedDocumentsAsync(
    EmployeeFormViewModel model)
        {
            var existingDocuments =
                await _context.EmployeeDocuments
                    .AsNoTracking()
                    .Where(x =>
                        x.EmployeeId == model.Employee.Id &&
                        x.EmployeeAcademicId == null)
                    .ToListAsync();

            model.StatutoryDocuments =
                BuildFixedDocumentInputs(
                    RestoreDocumentInputs(
                        model.StatutoryDocuments,
                        GetStatutoryDocumentInputs()),
                    existingDocuments.Where(x =>
                        x.DocumentCategory == "Statutory"));

            model.JoiningDocuments =
                BuildFixedDocumentInputs(
                    RestoreDocumentInputs(
                        model.JoiningDocuments,
                        GetJoiningDocumentInputs()),
                    existingDocuments.Where(x =>
                        x.DocumentCategory == "Joining"));
        }

        private async Task<int> ReplaceFixedDocumentsAsync(
            List<EmployeeDocumentInputViewModel>? inputs,
            EmployeeModel employee,
            string category,
            List<string> newlyStoredFiles,
            List<string> deletedFilePaths)
        {
            if (inputs == null)
            {
                return 0;
            }

            int uploadedCount = 0;

            foreach (var input in inputs)
            {
                if (input.File == null || input.File.Length == 0)
                {
                    continue;
                }

                var existingDocument =
                    await _context.EmployeeDocuments
                        .FirstOrDefaultAsync(x =>
                            x.EmployeeId == employee.Id &&
                            x.EmployeeAcademicId == null &&
                            x.DocumentCategory == category &&
                            x.DocumentType == input.DocumentType);

                var newDocument =
                    await _employeeDocumentService.SaveAsync(
                        input.File,
                        employee.EmployeeCode ?? $"EMP{employee.Id}",
                        input.DocumentType,
                        User.Identity?.Name);

                newDocument.EmployeeId = employee.Id;
                newDocument.EmployeeAcademicId = null;
                newDocument.DocumentCategory = category;

                newlyStoredFiles.Add(newDocument.StoragePath);

                await _employeeDocumentRepository.AddAsync(
                    newDocument);

                if (existingDocument != null)
                {
                    if (!string.IsNullOrWhiteSpace(
                            existingDocument.StoragePath))
                    {
                        deletedFilePaths.Add(
                            existingDocument.StoragePath);
                    }

                    await _employeeDocumentRepository.DeleteAsync(
                        existingDocument);
                }

                uploadedCount++;
            }

            return uploadedCount;
        }

        private void ValidatePreviousEmployments(
    List<EmployeePreviousEmploymentInputViewModel> employments)
        {
            const long maximumFileSize = 5 * 1024 * 1024;

            for (int i = 0; i < employments.Count; i++)
            {
                var employment = employments[i];

                bool rowIsEmpty =
                    string.IsNullOrWhiteSpace(employment.CompanyName) &&
                    !employment.StartDate.HasValue &&
                    !employment.EndDate.HasValue &&
                    employment.ExperienceLetter == null &&
                    employment.RelievingLetter == null;

                if (rowIsEmpty)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(employment.CompanyName))
                {
                    ModelState.AddModelError(
                        $"PreviousEmployments[{i}].CompanyName",
                        "Company name is required.");
                }

                if (!employment.StartDate.HasValue)
                {
                    ModelState.AddModelError(
                        $"PreviousEmployments[{i}].StartDate",
                        "Start date is required.");
                }

                if (!employment.EndDate.HasValue)
                {
                    ModelState.AddModelError(
                        $"PreviousEmployments[{i}].EndDate",
                        "End date is required.");
                }

                if (employment.StartDate.HasValue &&
                    employment.EndDate.HasValue &&
                    employment.EndDate < employment.StartDate)
                {
                    ModelState.AddModelError(
                        $"PreviousEmployments[{i}].EndDate",
                        "End date cannot be before start date.");
                }

                ValidatePreviousEmploymentPdf(
                    employment.ExperienceLetter,
                    $"PreviousEmployments[{i}].ExperienceLetter",
                    "Experience Letter",
                    maximumFileSize);

                ValidatePreviousEmploymentPdf(
                    employment.RelievingLetter,
                    $"PreviousEmployments[{i}].RelievingLetter",
                    "Relieving Letter",
                    maximumFileSize);
            }
        }


        private void ValidatePreviousEmploymentPdf(
IFormFile? file,
string modelKey,
string documentName,
long maximumFileSize)
        {
            if (file == null || file.Length == 0)
            {
                return;
            }

            string extension =
                Path.GetExtension(file.FileName)
                    .ToLowerInvariant();

            if (extension != ".pdf")
            {
                ModelState.AddModelError(
                    modelKey,
                    $"{documentName} must be a PDF file.");
            }

            if (!string.Equals(
                    file.ContentType,
                    "application/pdf",
                    StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    modelKey,
                    $"{documentName} has an invalid file type.");
            }

            if (file.Length > maximumFileSize)
            {
                ModelState.AddModelError(
                    modelKey,
                    $"{documentName} cannot exceed 5 MB.");
            }
        }

        private async Task<int>
    SavePreviousEmploymentDocumentAsync(
        IFormFile? file,
        EmployeeModel employee,
        int previousEmploymentId,
        string documentType,
        List<string> storedFiles)
        {
            if (file == null || file.Length == 0)
            {
                return 0;
            }

            var document =
                await _employeeDocumentService.SaveAsync(
                    file,
                    employee.EmployeeCode ?? $"EMP{employee.Id}",
                    documentType,
                    User.Identity?.Name);

            document.EmployeeId = employee.Id;
            document.EmployeeAcademicId = null;
            document.EmployeePreviousEmploymentId =
                previousEmploymentId;

            document.DocumentCategory =
                "PreviousEmployment";

            storedFiles.Add(document.StoragePath);

            await _employeeDocumentRepository
                .AddAsync(document);

            return 1;
        }

        private async Task ReloadExistingPreviousEmploymentDocumentsAsync(
    EmployeeFormViewModel model)
        {
            model.PreviousEmployments ??=
                new List<EmployeePreviousEmploymentInputViewModel>();

            foreach (var employment in model.PreviousEmployments
                .Where(x => x.Id.HasValue))
            {
                var documents =
                    await _employeeDocumentRepository.Documents
                        .AsNoTracking()
                        .Where(x =>
                            x.EmployeeId == model.Employee.Id &&
                            x.EmployeePreviousEmploymentId ==
                                employment.Id.Value &&
                            x.DocumentCategory ==
                                "PreviousEmployment")
                        .ToListAsync();

                employment.ExistingExperienceLetter =
                    documents.FirstOrDefault(x =>
                        x.DocumentType == "Experience Letter");

                employment.ExistingRelievingLetter =
                    documents.FirstOrDefault(x =>
                        x.DocumentType == "Relieving Letter");
            }
        }

        private async Task<int> ReplacePreviousEmploymentDocumentAsync(
    IFormFile? file,
    EmployeeModel employee,
    int previousEmploymentId,
    string documentType,
    List<string> newFilePaths,
    List<string> deletedFilePaths)
        {
            if (file == null || file.Length == 0)
            {
                return 0;
            }

            var existingDocument =
                await _employeeDocumentRepository.Documents
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == employee.Id &&
                        x.EmployeePreviousEmploymentId ==
                            previousEmploymentId &&
                        x.DocumentCategory ==
                            "PreviousEmployment" &&
                        x.DocumentType == documentType);

            var newDocument =
                await _employeeDocumentService.SaveAsync(
                    file,
                    employee.EmployeeCode ?? $"EMP{employee.Id}",
                    documentType,
                    User.Identity?.Name);

            newDocument.EmployeeId = employee.Id;
            newDocument.EmployeeAcademicId = null;
            newDocument.EmployeePreviousEmploymentId =
                previousEmploymentId;
            newDocument.DocumentCategory =
                "PreviousEmployment";

            newFilePaths.Add(newDocument.StoragePath);

            await _employeeDocumentRepository
                .AddAsync(newDocument);

            if (existingDocument != null)
            {
                if (!string.IsNullOrWhiteSpace(
                        existingDocument.StoragePath))
                {
                    deletedFilePaths.Add(
                        existingDocument.StoragePath);
                }

                await _employeeDocumentRepository
                    .DeleteAsync(existingDocument);
            }

            return 1;
        }
    }
}

