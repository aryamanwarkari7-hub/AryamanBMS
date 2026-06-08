using AryamanBMS.Data;
using AryamanBMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    public class DesignationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DesignationController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(string searchText, int page = 1)
        {
            int pageSize = 5;

            var designations = _context.Designations
                .Include(d => d.Department)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                designations = designations.Where(d =>
                    d.DesignationName.Contains(searchText) ||
                    d.DisplayCode.Contains(searchText));
            }

            int totalRecords = designations.Count();

            var pagedDesignations = designations
                .OrderBy(d => d.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages =
                (int)Math.Ceiling((double)totalRecords / pageSize);

            ViewBag.SearchText = searchText;

            return View(pagedDesignations);
        }

        public IActionResult Create()
        {
            ViewBag.Departments = _context.Departments.ToList();

            return View();
        }

        [HttpPost]
        public IActionResult Create(DesignationModel designation)
        {
            if (ModelState.IsValid)
            {
                _context.Designations.Add(designation);
                _context.SaveChanges();
                TempData["Success"] = "Department created successfully.";
                return RedirectToAction("Index");
            }

            ViewBag.Departments = _context.Departments.ToList();

            return View(designation);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var designation = _context.Designations.Find(id);

            if (designation == null)
            {
                return NotFound();
            }

            ViewBag.Departments = _context.Departments.ToList();

            return View(designation);
        }

        [HttpPost]
        public IActionResult Edit(DesignationModel designation)
        {
            if (ModelState.IsValid)
            {
                _context.Designations.Update(designation);
                _context.SaveChanges();
                TempData["Success"] = "Department updated successfully.";
                return RedirectToAction("Index");
            }

            ViewBag.Departments = _context.Departments.ToList();

            return View(designation);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var designation = _context.Designations.Find(id);

            if (designation == null)
            {
                return NotFound();
            }

            return View(designation);
        }

        [HttpPost]
        public IActionResult Delete(DesignationModel designation)
        {
            var existingDesignation = _context.Designations.Find(designation.Id);

            if (existingDesignation == null)
            {
                return NotFound();
            }

            _context.Designations.Remove(existingDesignation);
            _context.SaveChanges();
            TempData["Success"] = "Department deleted successfully.";
            return RedirectToAction("Index");
        }
    }
}