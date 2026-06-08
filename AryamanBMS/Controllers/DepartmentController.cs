using AryamanBMS.Data;
using AryamanBMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    public class DepartmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        public DepartmentController(ApplicationDbContext context)
        {
            _context = context;
        }


        public IActionResult Index(string searchText, int page = 1)
        {
            int pageSize = 5;

            var departments = _context.Departments
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                departments = departments.Where(d =>
                    d.DepartmentName.Contains(searchText) ||
                    d.DisplayCode.Contains(searchText));
            }

            int totalRecords = departments.Count();

            var pagedDepartments = departments
                .OrderBy(d => d.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages =
                (int)Math.Ceiling((double)totalRecords / pageSize);

            ViewBag.SearchText = searchText;

            return View(pagedDepartments);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(DepartmentModel department)
        {
            if (ModelState.IsValid)
            {
                _context.Departments.Add(department);
                _context.SaveChanges();
                TempData["Success"] = "Department created successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View(department);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var department = _context.Departments.Find(id);

            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        [HttpPost]
        public IActionResult Edit(DepartmentModel department)
        {
            _context.Departments.Update(department);
            _context.SaveChanges();
            TempData["Success"] = "Department updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var department = _context.Departments.Find(id);

            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        [HttpPost]
        public IActionResult DeleteConfirmed(int id)
        {
            bool isUsed = _context.Designations
                                  .Any(d => d.DepartmentId == id);

            if (isUsed)
            {
                TempData["Error"] =
                    "Cannot delete department because designations exist.";
                
                return RedirectToAction(nameof(Index));
            }

            var department = _context.Departments.Find(id);

            if (department != null)
            {
                _context.Departments.Remove(department);
                _context.SaveChanges();
            }
            TempData["Success"] = "Department deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

    }
}