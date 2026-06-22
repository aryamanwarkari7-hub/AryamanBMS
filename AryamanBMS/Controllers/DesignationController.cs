using AryamanBMS.Extensions;
using Microsoft.EntityFrameworkCore;
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

        public async Task<IActionResult> Index(
    string? searchText,
    int page = 1)
        {
            const int pageSize = 5;

            var designations = _designationRepository.Designations
                .AsNoTracking()
                .Include(d => d.Department)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                searchText = searchText.Trim();

                designations = designations.Where(d =>
                    d.DesignationName.Contains(searchText) ||
                    d.DisplayCode.Contains(searchText));
            }

            designations = designations
                .OrderBy(d => d.Id);

            var routeValues = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                routeValues["searchText"] = searchText;
            }

            var model = await designations.ToPagedListAsync(
                page,
                pageSize,
                routeValues);

            model.Pagination.ControllerName = "Designation";
            model.Pagination.ActionName = nameof(Index);

            ViewBag.SearchText = searchText;

            return View(model);
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