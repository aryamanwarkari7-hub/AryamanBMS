using AryamanBMS.Extensions;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            string sortBy = "DesignationName",
            string sortOrder = "asc",
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

            bool desc =
                string.Equals(
                    sortOrder,
                    "desc",
                    StringComparison.OrdinalIgnoreCase);

            designations = sortBy switch
            {
                "DisplayCode" => desc
                    ? designations.OrderByDescending(d => d.DisplayCode)
                    : designations.OrderBy(d => d.DisplayCode),

                "Department" => desc
                    ? designations.OrderByDescending(d =>
                        d.Department != null
                            ? d.Department.DepartmentName
                            : string.Empty)
                    : designations.OrderBy(d =>
                        d.Department != null
                            ? d.Department.DepartmentName
                            : string.Empty),

                "Status" => desc
                    ? designations.OrderByDescending(d => d.IsActive)
                    : designations.OrderBy(d => d.IsActive),

                _ => desc
                    ? designations.OrderByDescending(d => d.DesignationName)
                    : designations.OrderBy(d => d.DesignationName)
            };

            var routeValues = new Dictionary<string, string>();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                routeValues["searchText"] = searchText;
            }

            routeValues["sortBy"] = sortBy;
            routeValues["sortOrder"] = sortOrder;

            var model = await designations.ToPagedListAsync(
                page,
                pageSize,
                routeValues);

            model.Pagination.ControllerName = "Designation";
            model.Pagination.ActionName = nameof(Index);

            ViewBag.SearchText = searchText;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            LoadDepartments();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DesignationModel designation)
        {

            Console.WriteLine($"DesignationName = '{designation.DesignationName}'");
            Console.WriteLine($"DisplayCode = '{designation.DisplayCode}'");
            Console.WriteLine($"DepartmentId = '{designation.DepartmentId}'");
            Console.WriteLine($"ModelState Valid = {ModelState.IsValid}");
            bool designationExists = _designationRepository.Designations.Any(d =>
    d.DesignationName.Trim().ToLower() ==
    (designation.DesignationName ?? "").Trim().ToLower());

            bool displayCodeExists = _designationRepository.Designations.Any(d =>
                d.DisplayCode.Trim().ToLower() ==
                (designation.DisplayCode ?? "").Trim().ToLower());

            var all = _designationRepository.Designations.ToList();

            Console.WriteLine("===== Existing Designations =====");

            foreach (var d in all)
            {
                Console.WriteLine($"Name='{d.DesignationName}', Code='{d.DisplayCode}'");
            }

            if (designationExists)
            {
                ModelState.AddModelError(
                    nameof(designation.DesignationName),
                    "Designation already exists.");

                LoadDepartments();
                return View(designation);
            }

            if (displayCodeExists)
            {
                ModelState.AddModelError(
                    nameof(designation.DisplayCode),
                    "Display Code already exists.");

                LoadDepartments();
                return View(designation);
            }

            if (!ModelState.IsValid)
            {
                LoadDepartments();

                return View(designation);
            }

            await _designationRepository.AddAsync(designation);
            await _designationRepository.SaveAsync();

            TempData["Success"] =
                "Designation created successfully.";

            return RedirectToAction(nameof(Index));
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
        [ValidateAntiForgeryToken]
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
        //public async Task<IActionResult> Delete(int id)
        //{
        //    var designation =
        //        await _designationRepository.GetByIdAsync(id);

        //    if (designation == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(designation);
        //}

        

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteConfirmed(int id)
        //{
        //    var designation =
        //        await _designationRepository.GetByIdAsync(id);

        //    if (designation == null)
        //    {
        //        return NotFound();
        //    }

        //    await _designationRepository.DeleteAsync(designation);
        //    await _designationRepository.SaveAsync();

        //    TempData["Success"] =
        //        "Designation deleted successfully.";

        //    return RedirectToAction(nameof(Index));
        //}



        private void LoadDepartments()
        {
            ViewBag.Departments =
                _departmentRepository.Departments
                .OrderBy(d => d.DepartmentName)
                .ToList();
        }
    }
}
