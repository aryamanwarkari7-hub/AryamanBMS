using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class DesignationController : Controller
    {
        private readonly IDesignationRepository _designationRepository;
        private readonly IDepartmentRepository _departmentRepository;

        public DesignationController(
            IDesignationRepository designationRepository,
            IDepartmentRepository departmentRepository)
        {
            _designationRepository = designationRepository;
            _departmentRepository = departmentRepository;
        }

        public IActionResult Index(string searchText, int page = 1)
        {
            int pageSize = 5;

            var designations = _designationRepository.Designations
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

        [HttpGet]
        public IActionResult Create()
        {
            LoadDepartments();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            DesignationModel designation)
        {
            if (ModelState.IsValid)
            {
                await _designationRepository.AddAsync(designation);
                await _designationRepository.SaveAsync();

                TempData["Success"] =
                    "Designation created successfully.";

                return RedirectToAction(nameof(Index));
            }

            LoadDepartments();

            return View(designation);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var designation =
                await _designationRepository.GetByIdAsync(id);

            if (designation == null)
            {
                return NotFound();
            }

            LoadDepartments();

            return View(designation);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(
            DesignationModel designation)
        {
            if (!ModelState.IsValid)
            {
                LoadDepartments();

                return View(designation);
            }

            await _designationRepository.UpdateAsync(designation);
            await _designationRepository.SaveAsync();

            TempData["Success"] =
                "Designation updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var designation =
                await _designationRepository.GetByIdAsync(id);

            if (designation == null)
            {
                return NotFound();
            }

            return View(designation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var designation =
                await _designationRepository.GetByIdAsync(id);

            if (designation == null)
            {
                return NotFound();
            }

            await _designationRepository.DeleteAsync(designation);
            await _designationRepository.SaveAsync();

            TempData["Success"] =
                "Designation deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private void LoadDepartments()
        {
            ViewBag.Departments =
                _departmentRepository.Departments
                .OrderBy(d => d.DepartmentName)
                .ToList();
        }
    }
}