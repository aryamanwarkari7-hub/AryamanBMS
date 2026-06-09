using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class DepartmentController : Controller
    {
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IDesignationRepository _designationRepository;

        public DepartmentController(
            IDepartmentRepository departmentRepository,
            IDesignationRepository designationRepository)
        {
            _departmentRepository = departmentRepository;
            _designationRepository = designationRepository;
        }

        public IActionResult Index(string searchText, int page = 1)
        {
            int pageSize = 5;

            var departments = _departmentRepository.Departments
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
        public async Task<IActionResult> Create(
            DepartmentModel department)
        {
            if (ModelState.IsValid)
            {
                await _departmentRepository.AddAsync(department);
                await _departmentRepository.SaveAsync();

                TempData["Success"] =
                    "Department created successfully.";

                return RedirectToAction(nameof(Index));
            }

            return View(department);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var department =
                await _departmentRepository.GetByIdAsync(id);

            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(
            DepartmentModel department)
        {
            if (!ModelState.IsValid)
            {
                return View(department);
            }

            await _departmentRepository.UpdateAsync(department);
            await _departmentRepository.SaveAsync();

            TempData["Success"] =
                "Department updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var department =
                await _departmentRepository.GetByIdAsync(id);

            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {

            bool isUsed = _designationRepository.Designations
                      .Any(d => d.DepartmentId == id);

            if (isUsed)
            {
                TempData["Error"] =
                    "Cannot delete department because designations exist.";

                return RedirectToAction(nameof(Index));
            }


            var department =
                await _departmentRepository.GetByIdAsync(id);
            


            if (department == null)
            {
                return RedirectToAction(nameof(Index));
            }

            await _departmentRepository.DeleteAsync(department);
            await _departmentRepository.SaveAsync();

            TempData["Success"] =
                "Department deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}