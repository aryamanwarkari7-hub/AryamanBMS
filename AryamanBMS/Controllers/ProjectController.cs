using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class ProjectController : Controller
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IEmployeeRepository _employeeRepository;

        public ProjectController(
            IProjectRepository projectRepository,
            IEmployeeRepository employeeRepository)
        {
            _projectRepository = projectRepository;
            _employeeRepository = employeeRepository;
        }

        public async Task<IActionResult> Index(
            string? searchText,
            string? status,
            string? priority,
            int page = 1)
        {
            const int pageSize = 5;

            var projects = _projectRepository.Projects;

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string search = searchText.Trim();

                projects = projects.Where(p =>
                    p.ProjectCode.Contains(search) ||
                    p.ProjectName.Contains(search) ||
                    (p.ClientName != null &&
                     p.ClientName.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                projects = projects.Where(p => p.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                projects = projects.Where(p => p.Priority == priority);
            }

            int totalRecords = await projects.CountAsync();

            var data = await projects
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.SearchText = searchText;
            ViewBag.Status = status;
            ViewBag.Priority = priority;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages =
                (int)Math.Ceiling(totalRecords / (double)pageSize);

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadEmployeesAsync();
            return View(new ProjectModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectModel model)
        {
            bool codeExists = await _projectRepository.Projects
                .AnyAsync(p => p.ProjectCode == model.ProjectCode);

            if (codeExists)
            {
                ModelState.AddModelError(
                    nameof(model.ProjectCode),
                    "Project code already exists.");
            }

            ValidateDates(model);

            if (!ModelState.IsValid)
            {
                await LoadEmployeesAsync();
                return View(model);
            }

            model.ProjectCode = model.ProjectCode.Trim().ToUpper();
            model.ProjectName = model.ProjectName.Trim();
            model.CreatedOn = DateTime.Now;
            model.IsActive = true;

            await _projectRepository.AddAsync(model);
            await _projectRepository.SaveAsync();

            TempData["Success"] = "Project created successfully.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var project =
                await _projectRepository.GetDetailsAsync(id);

            if (project == null)
                return NotFound();

            return View(project);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var project =
                await _projectRepository.GetByIdAsync(id);

            if (project == null)
                return NotFound();

            await LoadEmployeesAsync();

            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProjectModel model)
        {
            bool codeExists = await _projectRepository.Projects
                .AnyAsync(p =>
                    p.ProjectCode == model.ProjectCode &&
                    p.Id != model.Id);

            if (codeExists)
            {
                ModelState.AddModelError(
                    nameof(model.ProjectCode),
                    "Project code already exists.");
            }

            ValidateDates(model);

            if (!ModelState.IsValid)
            {
                await LoadEmployeesAsync();
                return View(model);
            }

            var existing =
                await _projectRepository.GetByIdAsync(model.Id);

            if (existing == null)
                return NotFound();

            existing.ProjectCode =
                model.ProjectCode.Trim().ToUpper();

            existing.ProjectName =
                model.ProjectName.Trim();

            existing.ProjectType = model.ProjectType;
            existing.ClientName = model.ClientName;
            existing.BusinessObjective = model.BusinessObjective;
            existing.Scope = model.Scope;
            existing.StartDate = model.StartDate;
            existing.EndDate = model.EndDate;
            existing.Budget = model.Budget;
            existing.Priority = model.Priority;
            existing.Status = model.Status;
            existing.ProjectManagerId = model.ProjectManagerId;
            existing.IsActive = model.IsActive;
            existing.UpdatedOn = DateTime.Now;

            await _projectRepository.UpdateAsync(existing);
            await _projectRepository.SaveAsync();

            TempData["Success"] = "Project updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var project =
                await _projectRepository.GetDetailsAsync(id);

            if (project == null)
                return NotFound();

            return View(project);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project =
                await _projectRepository.GetByIdAsync(id);

            if (project == null)
                return NotFound();

            await _projectRepository.DeleteAsync(project);
            await _projectRepository.SaveAsync();

            TempData["Success"] = "Project deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadEmployeesAsync()
        {
            ViewBag.ProjectManagers =
                await _employeeRepository.Employees
                    .Where(e => e.IsActive)
                    .OrderBy(e => e.FirstName)
                    .ThenBy(e => e.LastName)
                    .ToListAsync();
        }

        private void ValidateDates(ProjectModel model)
        {
            if (model.StartDate.HasValue &&
                model.EndDate.HasValue &&
                model.EndDate < model.StartDate)
            {
                ModelState.AddModelError(
                    nameof(model.EndDate),
                    "End date cannot be before start date.");
            }
        }
    }
}