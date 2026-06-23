using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class ProjectMemberController : Controller
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IProjectMemberRepository _projectMemberRepository;
        private readonly IEmployeeRepository _employeeRepository;

        public ProjectMemberController(
            IProjectRepository projectRepository,
            IProjectMemberRepository projectMemberRepository,
            IEmployeeRepository employeeRepository)
        {
            _projectRepository = projectRepository;
            _projectMemberRepository = projectMemberRepository;
            _employeeRepository = employeeRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int projectId)
        {
            var project = await _projectRepository.GetDetailsAsync(projectId);

            if (project == null)
                return NotFound();

            var members = await _projectMemberRepository.ProjectMembers
                .Where(pm => pm.ProjectId == projectId)
                .OrderBy(pm => pm.Employee!.FirstName)
                .ThenBy(pm => pm.Employee!.LastName)
                .ToListAsync();

            ViewBag.Project = project;

            return View(members);
        }

        [HttpGet]
        public async Task<IActionResult> Add(int projectId)
        {
            var project = await _projectRepository.GetDetailsAsync(projectId);

            if (project == null)
                return NotFound();

            await LoadAvailableEmployeesAsync(projectId);

            ViewBag.Project = project;

            return View(new ProjectMemberModel
            {
                ProjectId = projectId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(ProjectMemberModel model)
        {
            var project = await _projectRepository.GetByIdAsync(model.ProjectId);

            if (project == null)
                return NotFound();

            var existing =
                await _projectMemberRepository
                    .GetByProjectAndEmployeeAsync(
                        model.ProjectId,
                        model.EmployeeId);

            if (existing != null)
            {
                ModelState.AddModelError(
                    nameof(model.EmployeeId),
                    "This employee is already assigned to the project.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAvailableEmployeesAsync(model.ProjectId);

                ViewBag.Project =
                    await _projectRepository.GetDetailsAsync(model.ProjectId);

                return View(model);
            }

            model.AssignedOn = DateTime.Now;
            model.IsActive = true;

            await _projectMemberRepository.AddAsync(model);
            await _projectMemberRepository.SaveAsync();

            TempData["Success"] =
                "Project member added successfully.";

            return RedirectToAction(
                nameof(Index),
                new { projectId = model.ProjectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var member =
                await _projectMemberRepository.GetByIdAsync(id);

            if (member == null)
                return NotFound();

            var project =
                await _projectRepository.GetByIdAsync(member.ProjectId);

            if (project == null)
                return NotFound();

            if (project.ProjectManagerId == member.EmployeeId)
            {
                TempData["Error"] =
                    "The project manager cannot be removed from project members.";

                return RedirectToAction(
                    nameof(Index),
                    new { projectId = member.ProjectId });
            }

            int projectId = member.ProjectId;

            await _projectMemberRepository.DeleteAsync(member);
            await _projectMemberRepository.SaveAsync();

            TempData["Success"] =
                "Project member removed successfully.";

            return RedirectToAction(
                nameof(Index),
                new { projectId });
        }

        private async Task LoadAvailableEmployeesAsync(int projectId)
        {
            var assignedEmployeeIds =
                await _projectMemberRepository.ProjectMembers
                    .Where(pm => pm.ProjectId == projectId)
                    .Select(pm => pm.EmployeeId)
                    .ToListAsync();

            ViewBag.Employees =
                await _employeeRepository.Employees
                    .Where(e =>
                        e.IsActive &&
                        !assignedEmployeeIds.Contains(e.Id))
                    .OrderBy(e => e.FirstName)
                    .ThenBy(e => e.LastName)
                    .ToListAsync();
        }

        [HttpGet]
        public async Task<IActionResult> AllMembers(
    int? projectId,
    string? searchText,
    int page = 1)
        {
            const int pageSize = 10;

            var members = _projectMemberRepository.ProjectMembers;

            if (projectId.HasValue)
            {
                members = members.Where(pm =>
                    pm.ProjectId == projectId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string search = searchText.Trim();

                members = members.Where(pm =>
                    pm.Employee!.EmployeeCode.Contains(search) ||
                    pm.Employee.FirstName.Contains(search) ||
                    (pm.Employee.LastName != null &&
                     pm.Employee.LastName.Contains(search)) ||
                    (pm.RoleInProject != null &&
                     pm.RoleInProject.Contains(search)) ||
                    pm.Project!.ProjectCode.Contains(search) ||
                    pm.Project.ProjectName.Contains(search));
            }

            int totalRecords = await members.CountAsync();

            var data = await members
                .OrderBy(pm => pm.Project!.ProjectName)
                .ThenBy(pm => pm.Employee!.FirstName)
                .ThenBy(pm => pm.Employee!.LastName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Projects = await _projectRepository.Projects
                .Where(p => p.IsActive)
                .OrderBy(p => p.ProjectName)
                .ToListAsync();

            ViewBag.ProjectId = projectId;
            ViewBag.SearchText = searchText;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages =
                (int)Math.Ceiling(totalRecords / (double)pageSize);

            return View(data);
        }
    }
}