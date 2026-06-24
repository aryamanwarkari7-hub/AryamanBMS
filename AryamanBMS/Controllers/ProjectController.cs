using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR,ProjectManager")]
    public class ProjectController : Controller
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IProjectMemberRepository _projectMemberRepository;
        private readonly IProjectTaskRepository _projectTaskRepository;
        private readonly IProjectRiskRepository _projectRiskRepository;
        private readonly IProjectMeetingRepository _projectMeetingRepository;
        private readonly IProjectFlowRepository _projectFlowRepository;

        public ProjectController(
           IProjectRepository projectRepository,
           IEmployeeRepository employeeRepository,
           IProjectMemberRepository projectMemberRepository,
           IProjectTaskRepository projectTaskRepository,
           IProjectRiskRepository projectRiskRepository,
           IProjectMeetingRepository projectMeetingRepository,
           IProjectFlowRepository projectFlowRepository)
        {
            _projectRepository = projectRepository;
            _employeeRepository = employeeRepository;
            _projectMemberRepository = projectMemberRepository;
            _projectTaskRepository = projectTaskRepository;
            _projectRiskRepository = projectRiskRepository;
            _projectMeetingRepository = projectMeetingRepository;
            _projectFlowRepository = projectFlowRepository;
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

            if (model.ProjectManagerId > 0)
            {
                var existingMember =
                    await _projectMemberRepository
                        .GetByProjectAndEmployeeAsync(
                            model.Id,
                            model.ProjectManagerId);

                if (existingMember == null)
                {
                    var managerMember = new ProjectMemberModel
                    {
                        ProjectId = model.Id,
                        EmployeeId = model.ProjectManagerId,
                        RoleInProject = "Project Manager",
                        AssignedOn = DateTime.Now,
                        IsActive = true
                    };

                    await _projectMemberRepository.AddAsync(managerMember);
                    await _projectMemberRepository.SaveAsync();
                }
            }

            TempData["Success"] = "Project created successfully.";

            return RedirectToAction(nameof(Details), new { id = model.Id });
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
        public async Task<IActionResult> Dashboard(int id)
        {
            var project =
                await _projectRepository.GetDetailsAsync(id);

            if (project == null)
                return NotFound();

            var tasks =
                _projectTaskRepository.ProjectTasks
                    .Where(t =>
                        t.ProjectId == id &&
                        t.IsActive);

            int totalTaskCount =
                await tasks.CountAsync();

            decimal overallProgress = 0;

            if (totalTaskCount > 0)
            {
                overallProgress =
                    await tasks.AverageAsync(t =>
                        (decimal)t.ProgressPercent);
            }

            var currentFlow =
                await _projectFlowRepository.ProjectFlows
                    .Where(f =>
                        f.ProjectId == id &&
                        f.IsActive &&
                        f.StageStatus == "In Progress")
                    .OrderBy(f => f.StageOrder)
                    .FirstOrDefaultAsync();

            if (currentFlow == null)
            {
                currentFlow =
                    await _projectFlowRepository.ProjectFlows
                        .Where(f =>
                            f.ProjectId == id &&
                            f.IsActive &&
                            f.StageStatus == "Pending")
                        .OrderBy(f => f.StageOrder)
                        .FirstOrDefaultAsync();
            }

            if (currentFlow == null)
            {
                currentFlow =
                    await _projectFlowRepository.ProjectFlows
                        .Where(f =>
                            f.ProjectId == id &&
                            f.IsActive)
                        .OrderByDescending(f => f.StageOrder)
                        .FirstOrDefaultAsync();
            }

            var viewModel = new ProjectDashboardViewModel
            {
                Project = project,

                ActiveMemberCount =
                    await _projectMemberRepository.ProjectMembers
                        .CountAsync(pm =>
                            pm.ProjectId == id &&
                            pm.IsActive),

                TotalTaskCount = totalTaskCount,

                NotStartedTaskCount =
                    await tasks.CountAsync(t =>
                        t.Status == "Not Started"),

                InProgressTaskCount =
                    await tasks.CountAsync(t =>
                        t.Status == "In Progress"),

                CompletedTaskCount =
                    await tasks.CountAsync(t =>
                        t.Status == "Completed"),

                OverdueTaskCount =
                    await tasks.CountAsync(t =>
                        t.DueDate.HasValue &&
                        t.DueDate.Value.Date < DateTime.Today &&
                        t.Status != "Completed"),

                EstimatedHours =
                    await tasks.SumAsync(t =>
                        (decimal?)t.EstimatedHours) ?? 0,

                ActualHours =
                    await tasks.SumAsync(t =>
                        (decimal?)t.ActualHours) ?? 0,

                OverallProgress =
                    Math.Round(overallProgress, 2),

                OpenRiskCount =
                    await _projectRiskRepository.ProjectRisks
                        .CountAsync(r =>
                            r.ProjectId == id &&
                            r.IsActive &&
                            r.RiskStatus != "Resolved" &&
                            r.RiskStatus != "Closed"),

                CriticalRiskCount =
                    await _projectRiskRepository.ProjectRisks
                        .CountAsync(r =>
                            r.ProjectId == id &&
                            r.IsActive &&
                            r.Severity == "Critical" &&
                            r.RiskStatus != "Resolved" &&
                            r.RiskStatus != "Closed"),

                UpcomingMeetingCount =
                    await _projectMeetingRepository.Meetings
                        .CountAsync(m =>
                            m.ProjectId == id &&
                            m.IsActive &&
                            m.MeetingDate.Date >= DateTime.Today &&
                            m.MeetingStatus == "Scheduled"),

                CurrentFlowStage =
                    currentFlow?.StageName ?? "Not Started",

                CurrentFlowStatus =
                    currentFlow?.StageStatus ?? "-"
            };

            return View(viewModel);
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

            int previousManagerId = existing.ProjectManagerId;

            existing.ProjectCode =
                model.ProjectCode.Trim().ToUpper();

            existing.ProjectName =
                model.ProjectName.Trim();

            existing.ProjectManagerId = model.ProjectManagerId;

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

            // Ensure the newly selected project manager is an active project member.
            if (model.ProjectManagerId > 0)
            {
                var newManagerMember =
                    await _projectMemberRepository
                        .GetByProjectAndEmployeeAsync(
                            existing.Id,
                            model.ProjectManagerId);

                if (newManagerMember == null)
                {
                    newManagerMember = new ProjectMemberModel
                    {
                        ProjectId = existing.Id,
                        EmployeeId = model.ProjectManagerId,
                        RoleInProject = "Project Manager",
                        AssignedOn = DateTime.Now,
                        IsActive = true
                    };

                    await _projectMemberRepository.AddAsync(
                        newManagerMember);
                }
                else
                {
                    newManagerMember.RoleInProject =
                        "Project Manager";

                    newManagerMember.IsActive = true;

                    await _projectMemberRepository.UpdateAsync(
                        newManagerMember);
                }
            }

            if (previousManagerId > 0 &&
                 previousManagerId != model.ProjectManagerId)
            {
                var previousManagerMember =
                    await _projectMemberRepository
                        .GetByProjectAndEmployeeAsync(
                            existing.Id,
                            previousManagerId);

                if (previousManagerMember != null)
                {
                    previousManagerMember.RoleInProject =
                        "Project Member";

                    await _projectMemberRepository.UpdateAsync(
                        previousManagerMember);
                }
            }

            TempData["Success"] = "Project updated successfully.";

            return RedirectToAction(nameof(Details), new { id = existing.Id });
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