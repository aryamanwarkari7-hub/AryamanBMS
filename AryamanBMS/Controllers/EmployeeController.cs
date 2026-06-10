using AryamanBMS.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using AryamanBMS.Models;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class EmployeeController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IDesignationRepository _designationRepository;
        private readonly UserManager<ApplicationUserModel> _userManager;

        public EmployeeController(
            IEmployeeRepository employeeRepository,
            IDepartmentRepository departmentRepository,
            IDesignationRepository designationRepository,
            UserManager<ApplicationUserModel> userManager)
        {
            _employeeRepository = employeeRepository;
            _departmentRepository = departmentRepository;
            _designationRepository = designationRepository;
            _userManager = userManager;
        }

        public IActionResult Index(string searchText, int page = 1)
        {
            int pageSize = 5;

            var employees = _employeeRepository.Employees;

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                employees = employees.Where(e =>
                    (e.EmployeeCode ?? "").Contains(searchText) ||
                    (e.FirstName ?? "").Contains(searchText) ||
                    (e.LastName ?? "").Contains(searchText) ||
                    (e.PersonalEmail ?? "").Contains(searchText) ||
                    (e.OfficialEmail ?? "").Contains(searchText));
            }

            int totalRecords = employees.Count();

            var pagedEmployees = employees
                .OrderBy(e => e.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages =
                (int)Math.Ceiling((double)totalRecords / pageSize);

            ViewBag.SearchText = searchText;

            return View(pagedEmployees);
        }

        [HttpGet]
        public IActionResult Create()
        {
            LoadDropdowns();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(EmployeeModel employee)
        {
            bool codeExists = _employeeRepository.Employees
                .Any(e => e.EmployeeCode == employee.EmployeeCode);

            if (codeExists)
            {
                ModelState.AddModelError(
                    "EmployeeCode",
                    "Employee Code already exists.");
            }

            if (employee.DateOfBirth > DateTime.Today)
            {
                ModelState.AddModelError(
                    "DateOfBirth",
                    "Date of Birth cannot be in future.");
            }

            if (employee.JoiningDate > DateTime.Today)
            {
                ModelState.AddModelError(
                    "JoiningDate",
                    "Joining Date cannot be in future.");
            }

            if (!string.IsNullOrWhiteSpace(employee.AadhaarNo) &&
                employee.AadhaarNo.Length != 12)
            {
                ModelState.AddModelError(
                    "AadhaarNo",
                    "Aadhaar number must be 12 digits.");
            }

            if (!string.IsNullOrWhiteSpace(employee.PanNo) &&
                employee.PanNo.Length != 10)
            {
                ModelState.AddModelError(
                    "PanNo",
                    "PAN number must be 10 characters.");
            }

            if (!string.IsNullOrEmpty(employee.ApplicationUserId))
            {
                bool alreadyMapped = await _employeeRepository.Employees
                    .AnyAsync(e =>
                        e.ApplicationUserId ==
                        employee.ApplicationUserId);

                if (alreadyMapped)
                {
                    ModelState.AddModelError(
                        "ApplicationUserId",
                        "Selected user is already assigned to another employee.");
                }
            }

            if (ModelState.IsValid)
            {
                await _employeeRepository.AddAsync(employee);
                await _employeeRepository.SaveAsync();

                TempData["Success"] =
                    "Employee created successfully.";

                return RedirectToAction(nameof(Index));
            }

            LoadDropdowns();

            return View(employee);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var employee =
                await _employeeRepository.GetByIdAsync(id);

            if (employee == null)
            {
                return NotFound();
            }

            LoadDropdowns();

            return View(employee);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EmployeeModel employee)
        {
            if (employee.DateOfBirth > DateTime.Today)
            {
                ModelState.AddModelError(
                    "DateOfBirth",
                    "Date of Birth cannot be in future.");
            }

            if (employee.JoiningDate > DateTime.Today)
            {
                ModelState.AddModelError(
                    "JoiningDate",
                    "Joining Date cannot be in future.");
            }

            if (!string.IsNullOrWhiteSpace(employee.AadhaarNo) &&
                employee.AadhaarNo.Length != 12)
            {
                ModelState.AddModelError(
                    "AadhaarNo",
                    "Aadhaar number must be 12 digits.");
            }

            if (!string.IsNullOrWhiteSpace(employee.PanNo) &&
                employee.PanNo.Length != 10)
            {
                ModelState.AddModelError(
                    "PanNo",
                    "PAN number must be 10 characters.");
            }

            if (!string.IsNullOrEmpty(employee.ApplicationUserId))
            {
                bool alreadyMapped =
                    _employeeRepository.Employees.Any(e =>
                        e.ApplicationUserId ==
                        employee.ApplicationUserId &&
                        e.Id != employee.Id);

                if (alreadyMapped)
                {
                    ModelState.AddModelError(
                        "ApplicationUserId",
                        "Selected user is already assigned to another employee.");
                }
            }

            if (ModelState.IsValid)
            {
                await _employeeRepository.UpdateAsync(employee);
                await _employeeRepository.SaveAsync();

                TempData["Success"] =
                    "Employee updated successfully.";

                return RedirectToAction(nameof(Index));
            }

            LoadDropdowns();

            return View(employee);
        }

        [HttpGet]
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
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee =
                await _employeeRepository.GetByIdAsync(id);

            if (employee != null)
            {
                await _employeeRepository.DeleteAsync(employee);
                await _employeeRepository.SaveAsync();
            }

            TempData["Success"] =
                "Employee deleted successfully.";

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

        // Exel Export
        [HttpGet]
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
    }
}