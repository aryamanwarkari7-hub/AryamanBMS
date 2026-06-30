using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR,ProjectManager")]
    public class ProjectTaskController : Controller
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IProjectTaskRepository _projectTaskRepository;
        private readonly IProjectMemberRepository _projectMemberRepository;
        private readonly IProjectTimelineService _projectTimelineService;
        private readonly IProjectAccessService _projectAccessService;
        private readonly IEmployeeRepository _employeeRepository;

        public ProjectTaskController(
            IProjectRepository projectRepository,
            IProjectTaskRepository projectTaskRepository,
            IProjectMemberRepository projectMemberRepository,
            IProjectTimelineService projectTimelineService,
            IProjectAccessService projectAccessService,
            IEmployeeRepository employeeRepository)
        {
            _projectRepository = projectRepository;
            _projectTaskRepository = projectTaskRepository;
            _projectMemberRepository = projectMemberRepository;
            _projectAccessService = projectAccessService;
            _projectTimelineService = projectTimelineService;
            _employeeRepository = employeeRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int? projectId,
            string? searchText,
            string? status,
            string? priority,
            int page = 1)
        {
            const int pageSize = 10;

            var tasks = _projectTaskRepository.ProjectTasks.Where(t => t.IsActive);

            var accessibleProjects =
                await _projectAccessService.ApplyProjectFilterAsync(
                    User,
                    _projectRepository.Projects.Where(p => p.IsActive));

            if (projectId.HasValue)
            {
                if (!await _projectAccessService.CanAccessProjectAsync(
                    User,
                    projectId.Value))
                {
                    return Forbid();
                }

                tasks = tasks.Where(t => t.ProjectId == projectId.Value);
            }
            else
            {
                var accessibleProjectIds =
                    await accessibleProjects
                        .Select(p => p.Id)
                        .ToListAsync();

                tasks = tasks.Where(t =>
                    accessibleProjectIds.Contains(t.ProjectId));
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string search = searchText.Trim();

                tasks = tasks.Where(t =>
                    t.TaskCode.Contains(search) ||
                    t.TaskTitle.Contains(search) ||
                    (t.Description != null &&
                     t.Description.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                tasks = tasks.Where(t => t.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                tasks = tasks.Where(t => t.Priority == priority);
            }

            int totalRecords = await tasks.CountAsync();

            var data = await tasks
                .OrderByDescending(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Projects =
            await accessibleProjects
                .OrderBy(p => p.ProjectName)
                .ToListAsync();

            ViewBag.ProjectId = projectId;
            ViewBag.SearchText = searchText;
            ViewBag.Status = status;
            ViewBag.Priority = priority;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages =
                (int)Math.Ceiling(totalRecords / (double)pageSize);

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? projectId)
        {
            await LoadProjectsAsync();

            var model = new ProjectTaskModel();

            if (projectId.HasValue)
            {
                if (!await _projectAccessService.CanAccessProjectAsync(
                    User,
                    projectId.Value))
                {
                    return Forbid();
                }

                model.ProjectId = projectId.Value;
                await LoadProjectMembersAsync(projectId.Value);
            }
            else
            {
                ViewBag.ProjectMembers = new List<ProjectMemberModel>();
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectTaskModel model)
        {
            if (!await _projectAccessService.CanAccessProjectAsync(
              User,
              model.ProjectId))
            {
                return Forbid();
            }

            bool codeExists = await _projectTaskRepository.ProjectTasks
                .AnyAsync(t =>
                    t.ProjectId == model.ProjectId &&
                    t.TaskCode == model.TaskCode);

            if (codeExists)
            {
                ModelState.AddModelError(
                    nameof(model.TaskCode),
                    "Task code already exists for this project.");
            }

            ValidateTaskDates(model);

            if (model.AssignedEmployeeId.HasValue)
            {
                bool isProjectMember =
                    await _projectMemberRepository.ProjectMembers
                        .AnyAsync(pm =>
                            pm.ProjectId == model.ProjectId &&
                            pm.EmployeeId == model.AssignedEmployeeId.Value &&
                            pm.IsActive);

                if (!isProjectMember)
                {
                    ModelState.AddModelError(
                        nameof(model.AssignedEmployeeId),
                        "The assigned employee must be an active project member.");
                }
            }

            //if (string.Equals(
            //   model.Status,
            //   "Completed",
            //   StringComparison.OrdinalIgnoreCase))
            //{
            //    model.ProgressPercent = 100;
            //}

            //if (model.ProgressPercent == 100 &&
            //  !string.Equals(
            //      model.Status,
            //      "Completed",
            //      StringComparison.OrdinalIgnoreCase))
            //{
            //    model.Status = "Completed";
            //}

            if (!ModelState.IsValid)
            {
                await LoadProjectsAsync();
                await LoadProjectMembersAsync(model.ProjectId);

                return View(model);
            }

            model.TaskCode = model.TaskCode.Trim().ToUpper();
            model.TaskTitle = model.TaskTitle.Trim();
            model.Status = "Not Started";
            model.ProgressPercent = 0;
            model.ActualHours = 0;
            model.CreatedOn = DateTime.Now;
            model.IsActive = true;

            await _projectTaskRepository.AddAsync(model);
            await _projectTaskRepository.SaveAsync();

            TempData["Success"] =
                "Project task created successfully.";

            await _projectTimelineService.AddEventAsync(
              projectId: model.ProjectId,
              eventType: "TaskCreated",
              eventTitle: "Project task created",
              eventDescription:
                  $"Task {model.TaskCode} - {model.TaskTitle} was created.",
              relatedEntityType: "Task",
              relatedEntityId: model.Id,
              newValue: model.Status);

            return RedirectToAction(
                nameof(Index),
                new { projectId = model.ProjectId });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var task =
                await _projectTaskRepository.GetDetailsAsync(id);

            if (task == null)
                return NotFound();

            if (!await _projectAccessService.CanAccessProjectAsync(
              User,
              task.ProjectId))
            {
                return Forbid();
            }

            return View(task);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var task =
                await _projectTaskRepository.GetByIdAsync(id);

            if (task == null)
                return NotFound();

            if (!await _projectAccessService.CanAccessProjectAsync(
               User,
               task.ProjectId))
            {
                return Forbid();
            }

            await LoadProjectsAsync();
            await LoadProjectMembersAsync(task.ProjectId);

            return View(task);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProjectTaskModel model)
        {

            if (!await _projectAccessService.CanAccessProjectAsync(
              User,
              model.ProjectId))
            {
                return Forbid();
            }

            bool codeExists = await _projectTaskRepository.ProjectTasks
                .AnyAsync(t =>
                    t.ProjectId == model.ProjectId &&
                    t.TaskCode == model.TaskCode &&
                    t.Id != model.Id);

            if (codeExists)
            {
                ModelState.AddModelError(
                    nameof(model.TaskCode),
                    "Task code already exists for this project.");
            }

            ValidateTaskDates(model);

            if (model.AssignedEmployeeId.HasValue)
            {
                bool isProjectMember =
                    await _projectMemberRepository.ProjectMembers
                        .AnyAsync(pm =>
                            pm.ProjectId == model.ProjectId &&
                            pm.EmployeeId == model.AssignedEmployeeId.Value &&
                            pm.IsActive);

                if (!isProjectMember)
                {
                    ModelState.AddModelError(
                        nameof(model.AssignedEmployeeId),
                        "The assigned employee must be an active project member.");
                }
            }

            

            if (!ModelState.IsValid)
            {
                await LoadProjectsAsync();
                await LoadProjectMembersAsync(model.ProjectId);

                return View(model);
            }

            var existing =
                await _projectTaskRepository.GetByIdAsync(model.Id);

            if (existing == null)
                return NotFound();

            if (!await _projectAccessService.CanAccessProjectAsync(
                User,
                existing.ProjectId))
            {
                return Forbid();
            }

            string previousStatus = existing.Status ?? string.Empty;

            int previousProgress =
                existing.ProgressPercent;

            int? previousAssignedEmployeeId =
                existing.AssignedEmployeeId;


            existing.ProjectId = model.ProjectId;
            existing.AssignedEmployeeId = model.AssignedEmployeeId;
            existing.TaskCode = model.TaskCode.Trim().ToUpper();
            existing.TaskTitle = model.TaskTitle.Trim();
            existing.Description = model.Description;
            existing.StartDate = model.StartDate;
            existing.DueDate = model.DueDate;
            existing.Priority = model.Priority;
            existing.EstimatedHours = model.EstimatedHours;
            existing.IsActive = model.IsActive;
            existing.UpdatedOn = DateTime.Now;

            await _projectTaskRepository.UpdateAsync(existing);
            await _projectTaskRepository.SaveAsync();

            if (!string.Equals(
               previousStatus,
               existing.Status,
               StringComparison.OrdinalIgnoreCase))
            {
                await _projectTimelineService.AddEventAsync(
                    projectId: existing.ProjectId,
                    eventType: "TaskStatusChanged",
                    eventTitle: "Task status changed",
                    eventDescription:
                        $"Task {existing.TaskCode} - {existing.TaskTitle} changed " +
                        $"from {previousStatus} to {existing.Status}.",
                    relatedEntityType: "Task",
                    relatedEntityId: existing.Id,
                    previousValue: previousStatus,
                    newValue: existing.Status);
            }

            if (previousProgress != existing.ProgressPercent)
            {
                await _projectTimelineService.AddEventAsync(
                    projectId: existing.ProjectId,
                    eventType: "TaskProgressChanged",
                    eventTitle: "Task progress updated",
                    eventDescription:
                        $"Task {existing.TaskCode} - {existing.TaskTitle} progress " +
                        $"changed from {previousProgress}% to " +
                        $"{existing.ProgressPercent}%.",
                    relatedEntityType: "Task",
                    relatedEntityId: existing.Id,
                    previousValue: $"{previousProgress}%",
                    newValue: $"{existing.ProgressPercent}%");
            }

            if (previousAssignedEmployeeId !=
                   existing.AssignedEmployeeId)
            {
                string previousEmployeeName = "Unassigned";
                string newEmployeeName = "Unassigned";

                if (previousAssignedEmployeeId.HasValue)
                {
                    var previousEmployee =
                        await _employeeRepository.Employees
                            .AsNoTracking()
                            .FirstOrDefaultAsync(e =>
                                e.Id == previousAssignedEmployeeId.Value);

                    previousEmployeeName =
                        previousEmployee?.FullName ?? "Unassigned";
                }

                if (existing.AssignedEmployeeId.HasValue)
                {
                    var newEmployee =
                        await _employeeRepository.Employees
                            .AsNoTracking()
                            .FirstOrDefaultAsync(e =>
                                e.Id == existing.AssignedEmployeeId.Value);

                    newEmployeeName =
                        newEmployee?.FullName ?? "Unassigned";
                }

                await _projectTimelineService.AddEventAsync(
                    projectId: existing.ProjectId,
                    eventType: "TaskReassigned",
                    eventTitle: "Task assignee changed",
                    eventDescription:
                        $"Task {existing.TaskCode} - {existing.TaskTitle} was reassigned " +
                        $"from {previousEmployeeName} to {newEmployeeName}.",
                    relatedEntityType: "Task",
                    relatedEntityId: existing.Id,
                    previousValue: previousEmployeeName,
                    newValue: newEmployeeName);
            }

            bool wasPreviouslyCompleted =
              string.Equals(
                 previousStatus,
                 "Completed",
                 StringComparison.OrdinalIgnoreCase);

            bool isNowCompleted =
                string.Equals(
                    existing.Status,
                    "Completed",
                    StringComparison.OrdinalIgnoreCase);

            if (!wasPreviouslyCompleted && isNowCompleted)
            {
                await _projectTimelineService.AddEventAsync(
                    projectId: existing.ProjectId,
                    eventType: "TaskCompleted",
                    eventTitle: "Task completed",
                    eventDescription:
                        $"Task {existing.TaskCode} - {existing.TaskTitle} was completed.",
                    relatedEntityType: "Task",
                    relatedEntityId: existing.Id,
                    previousValue: previousStatus,
                    newValue: "Completed");
            }


            TempData["Success"] =
                "Project task updated successfully.";

            return RedirectToAction(
                nameof(Index),
                new { projectId = existing.ProjectId });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var task =
                await _projectTaskRepository.GetDetailsAsync(id);

            if (task == null)
                return NotFound();

            if (!await _projectAccessService.CanAccessProjectAsync(
              User,
              task.ProjectId))
            {
                return Forbid();
            }

            return View(task);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task =
                await _projectTaskRepository.GetByIdAsync(id);

            if (task == null)
                return NotFound();

            if (!await _projectAccessService.CanAccessProjectAsync(
               User,
               task.ProjectId))
            {
                return Forbid();
            }

            int projectId = task.ProjectId;

            await _projectTaskRepository.DeleteAsync(task);
            await _projectTaskRepository.SaveAsync();

            TempData["Success"] =
                "Project task deleted successfully.";

            return RedirectToAction(
                nameof(Index),
                new { projectId });
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectMembers(int projectId)
        {

            if (!await _projectAccessService.CanAccessProjectAsync(
                 User,
                 projectId))
            {
                return Forbid();
            }

            var members =
                await _projectMemberRepository.ProjectMembers
                    .Where(pm =>
                        pm.ProjectId == projectId &&
                        pm.IsActive)
                    .OrderBy(pm => pm.Employee!.FirstName)
                    .ThenBy(pm => pm.Employee!.LastName)
                    .Select(pm => new
                    {
                        id = pm.EmployeeId,
                        name =
                            pm.Employee!.EmployeeCode +
                            " - " +
                            pm.Employee.FullName
                    })
                    .ToListAsync();

            return Json(members);
        }

        private async Task LoadProjectsAsync()
        {
            var projects =
                await _projectAccessService.ApplyProjectFilterAsync(
                    User,
                    _projectRepository.Projects.Where(p => p.IsActive));

            ViewBag.Projects =
                await projects
                    .OrderBy(p => p.ProjectName)
                    .ToListAsync();
        }

        private async Task LoadProjectMembersAsync(int projectId)
        {
            ViewBag.ProjectMembers =
                await _projectMemberRepository.ProjectMembers
                    .Where(pm =>
                        pm.ProjectId == projectId &&
                        pm.IsActive)
                    .OrderBy(pm => pm.Employee!.FirstName)
                    .ThenBy(pm => pm.Employee!.LastName)
                    .ToListAsync();
        }

        private void ValidateTaskDates(ProjectTaskModel model)
        {
            if (model.StartDate.HasValue &&
                model.DueDate.HasValue &&
                model.DueDate < model.StartDate)
            {
                ModelState.AddModelError(
                    nameof(model.DueDate),
                    "Due date cannot be before start date.");
            }
        }
    }
}