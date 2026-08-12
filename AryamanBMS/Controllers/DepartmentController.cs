using AryamanBMS.Extensions;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class DepartmentController : Controller
    {
        #region Actions

        private readonly IDepartmentRepository _departmentRepository;
        private readonly IDesignationRepository _designationRepository;

        public DepartmentController(
            IDepartmentRepository departmentRepository,
            IDesignationRepository designationRepository)
        {
            _departmentRepository = departmentRepository;
            _designationRepository = designationRepository;
        }

        public async Task<IActionResult> Index(
    string? searchText,
    string sortBy = "DepartmentName",
    string sortOrder = "asc",
    int page = 1)
        {
            const int pageSize = 5;

            var departments = _departmentRepository.Departments
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                searchText = searchText.Trim();

                departments = departments.Where(d =>
                    d.DepartmentName.Contains(searchText) ||
                    d.DisplayCode.Contains(searchText));
            }

            bool desc =  string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            departments = sortBy switch
            {
                "Status" => desc
                    ? departments.OrderByDescending(d => d.IsActive)
                    : departments.OrderBy(d => d.IsActive),

                _ => desc
                    ? departments.OrderByDescending(d => d.DepartmentName)
                    : departments.OrderBy(d => d.DepartmentName)
            };

            var routeValues = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                routeValues["searchText"] = searchText;
                ViewBag.SortBy = sortBy;
                ViewBag.SortOrder = sortOrder;
            }

            var model = await departments.ToPagedListAsync(
                page,
                pageSize,
                routeValues);

            model.Pagination.ControllerName = "Department";
            model.Pagination.ActionName = nameof(Index);

            ViewBag.SearchText = searchText;

            return View(model);
        }

        [HttpGet]

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DepartmentModel department)
        {
            bool departmentExists = _departmentRepository.Departments.Any(d =>
    d.DepartmentName.ToLower() == department.DepartmentName.ToLower());

            bool displayCodeExists = _departmentRepository.Departments.Any(d =>
                d.DisplayCode.ToLower() == department.DisplayCode.ToLower());

            if (departmentExists)
            {
                ModelState.AddModelError(
                    nameof(department.DepartmentName),
                    "Department already exists.");

                return View(department);
            }

            if (displayCodeExists)
            {
                ModelState.AddModelError(
                    nameof(department.DisplayCode),
                    "Display Code already exists.");

                return View(department);
            }

            if (!ModelState.IsValid)
            {
                return View(department);
            }

            await _departmentRepository.AddAsync(department);
            await _departmentRepository.SaveAsync();

            TempData["Success"] = "Department created successfully.";

            return RedirectToAction(nameof(Index));
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
        [ValidateAntiForgeryToken]
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
        [ValidateAntiForgeryToken]
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

        


        #endregion
    }
}
