using AryamanBMS.Data;
using AryamanBMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace AryamanBMS.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly UserManager<ApplicationUserModel> _userManager;

        public EmployeeController(ApplicationDbContext context,
            UserManager<ApplicationUserModel> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index(string searchText, int page = 1)
        {
            int pageSize = 5;

            var employees = _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .Include(e => e.ApplicationUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                employees = employees.Where(e =>
                    e.EmployeeCode.Contains(searchText) ||
                    e.FirstName.Contains(searchText) ||
                    e.LastName.Contains(searchText) ||
                    e.Email.Contains(searchText));
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
            ViewBag.Departments = _context.Departments.ToList();

            ViewBag.Designations = _context.Designations.ToList();

            ViewBag.Users = _userManager.Users
                             .Where(x => x.IsActive)
                             .OrderBy(x => x.FullName)
                             .ToList();

            return View();
        }

        [HttpPost]
        public IActionResult Create(EmployeeModel employee)
        {
            bool codeExists = _context.Employees
                .Any(e => e.EmployeeCode == employee.EmployeeCode);

            if (codeExists)
            {
                ModelState.AddModelError(
                    "EmployeeCode",
                    "Employee Code already exists.");
            }

            if (!string.IsNullOrEmpty(employee.ApplicationUserId))
            {
                bool alreadyMapped = _context.Employees
                    .Any(e => e.ApplicationUserId == employee.ApplicationUserId);

                if (alreadyMapped)
                {
                    ModelState.AddModelError(
                        "ApplicationUserId",
                        "Selected user is already assigned to another employee.");
                }
            }

            if (ModelState.IsValid)
            {
                _context.Employees.Add(employee);

                _context.SaveChanges();
                TempData["Success"] = "Department created successfully.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Departments = _context.Departments.ToList();
            ViewBag.Designations = _context.Designations.ToList();
            ViewBag.Users = _userManager.Users
                                      .Where(u => u.IsActive)
                                      .OrderBy(u => u.FullName)
                                      .ToList();
            return View(employee);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var employee = _context.Employees.Find(id);

            if (employee == null)
            {
                return NotFound();
            }

            ViewBag.Departments = _context.Departments.ToList();

            ViewBag.Designations = _context.Designations.ToList();

            ViewBag.Users = _userManager.Users
                           .Where(x => x.IsActive)
                           .OrderBy(x => x.FullName)
                           .ToList();

            return View(employee);
        }

        [HttpPost]
        public IActionResult Edit(EmployeeModel employee)
        {
            if (!string.IsNullOrEmpty(employee.ApplicationUserId))
            {
                bool alreadyMapped = _context.Employees
                    .Any(e =>
                        e.ApplicationUserId == employee.ApplicationUserId
                        && e.Id != employee.Id);

                if (alreadyMapped)
                {
                    ModelState.AddModelError(
                        "ApplicationUserId",
                        "Selected user is already assigned to another employee.");
                }
            }

            if (ModelState.IsValid)
            {
                _context.Employees.Update(employee);

                _context.SaveChanges();
                TempData["Success"] = "Department updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Departments = _context.Departments.ToList();

            ViewBag.Designations = _context.Designations.ToList();

            ViewBag.Users = _userManager.Users
                                   .Where(u => u.IsActive)
                                   .OrderBy(u => u.FullName)
                                   .ToList();

            return View(employee);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var employee = _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .FirstOrDefault(e => e.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        [HttpPost]
        public IActionResult DeleteConfirmed(int id)
        {
            var employee = _context.Employees.Find(id);

            if (employee != null)
            {
                _context.Employees.Remove(employee);

                _context.SaveChanges();
            }
            TempData["Success"] = "Department deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public JsonResult GetDesignations(int departmentId)
        {
            var designations = _context.Designations
                .Where(d => d.DepartmentId == departmentId)
                .Select(d => new
                {
                    id = d.Id,
                    designationName = d.DesignationName
                })
                .ToList();

            return Json(designations);
        }

        public async Task<IActionResult> Details(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .Include(e => e.ApplicationUser)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }
    }
}