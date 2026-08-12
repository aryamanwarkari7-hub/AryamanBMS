using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize]
    public class TaskTrackerController : Controller
    {
        #region Actions

        private readonly IProjectRepository _projectRepository;
        private readonly IProjectTaskRepository _projectTaskRepository;
        private readonly IProjectTaskProgressRepository _progressRepository;
        private readonly IProjectAccessService _projectAccessService;

        public TaskTrackerController(
            IProjectRepository projectRepository,
            IProjectTaskRepository projectTaskRepository,
            IProjectTaskProgressRepository progressRepository,
            IProjectAccessService projectAccessService)
        {
            _projectRepository = projectRepository;
            _projectTaskRepository = projectTaskRepository;
            _progressRepository = progressRepository;
            _projectAccessService = projectAccessService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int? projectId,
            int? projectTaskId,
            string? search,
            string sortBy = "ProgressDate",
            string sortOrder = "desc")
        {
            var taskUpdates = _progressRepository.ProjectTaskProgresses
                .Where(p => p.IsActive);

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

                taskUpdates  = taskUpdates.Where(p =>
                    p.ProjectTask!.ProjectId == projectId.Value);
            }
            else
            {
                var accessibleProjectIds =
                    await accessibleProjects
                        .Select(p => p.Id)
                        .ToListAsync();

                taskUpdates  = taskUpdates.Where(p =>
                    accessibleProjectIds.Contains(
                        p.ProjectTask!.ProjectId));
            }

            if (projectTaskId.HasValue)
            {
                if (!await CanAccessTaskAsync(projectTaskId.Value))
                {
                    return Forbid();
                }

                taskUpdates  = taskUpdates.Where(p =>
                    p.ProjectTaskId == projectTaskId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim();

                taskUpdates = taskUpdates.Where(p =>
                    p.ProgressNotes.Contains(keyword) ||
                    p.ProjectTask!.TaskCode.Contains(keyword) ||
                    p.ProjectTask.TaskTitle.Contains(keyword) ||
                    p.ProjectTask.Project!.ProjectCode.Contains(keyword) ||
                    p.ProjectTask.Project.ProjectName.Contains(keyword));
            }

            bool desc =
                string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            taskUpdates = sortBy switch
            {
                "Project" => desc
                    ? taskUpdates.OrderByDescending(p => p.ProjectTask!.Project!.ProjectCode)
                    : taskUpdates.OrderBy(p => p.ProjectTask!.Project!.ProjectCode),

                "Task" => desc
                    ? taskUpdates.OrderByDescending(p => p.ProjectTask!.TaskCode)
                    : taskUpdates.OrderBy(p => p.ProjectTask!.TaskCode),

                "Hours" => desc
                    ? taskUpdates.OrderByDescending(p => p.HoursWorked)
                    : taskUpdates.OrderBy(p => p.HoursWorked),

                "Completion" => desc
                    ? taskUpdates.OrderByDescending(p => p.CompletionPercentage)
                    : taskUpdates.OrderBy(p => p.CompletionPercentage),

                "Status" => desc
                    ? taskUpdates.OrderByDescending(p => p.IsActive)
                    : taskUpdates.OrderBy(p => p.IsActive),

                _ => desc
                    ? taskUpdates.OrderByDescending(p => p.ProgressDate)
                        .ThenByDescending(p => p.Id)
                    : taskUpdates.OrderBy(p => p.ProgressDate)
                        .ThenBy(p => p.Id)
            };

            var data = await taskUpdates.ToListAsync();

            await LoadProjectTasksAsync();

            ViewBag.ProjectId = projectId;
            ViewBag.ProjectTaskId = projectTaskId;
            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

            return View(data);
        }                      

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var taskUpdate =
                await _progressRepository.GetDetailsAsync(id);

            if (taskUpdate == null)
                return NotFound();

            if (!await CanAccessTaskAsync(taskUpdate.ProjectTaskId))
            {
                return Forbid();
            }

            return View(taskUpdate);
        }             
        

        [HttpGet]
        public async Task<IActionResult> GetProjectTasks(int projectId)
        {
            if (!await _projectAccessService.CanAccessProjectAsync(User, projectId))
            {
                return Forbid();
            }

            var tasks =
                await _projectTaskRepository.ProjectTasks
                    .AsNoTracking()
                    .Where(t => t.IsActive && t.ProjectId == projectId)
                    .OrderBy(t => t.TaskCode)
                    .Select(t => new
                    {
                        id = t.Id,
                        name = t.TaskCode + " - " + t.TaskTitle
                    })
                    .ToListAsync();

            return Json(tasks);
        }

        private async Task LoadProjectTasksAsync(int? projectId = null)
        {
            var accessibleProjects =
                await _projectAccessService.ApplyProjectFilterAsync(
                    User,
                    _projectRepository.Projects.Where(p => p.IsActive));

            var accessibleProjectIds =
                await accessibleProjects
                    .Select(p => p.Id)
                    .ToListAsync();

            var tasks = _projectTaskRepository.ProjectTasks
                .Include(t => t.Project)
                .Where(t => t.IsActive &&
                      accessibleProjectIds.Contains(t.ProjectId));

            if (projectId.HasValue)
            {
                tasks = tasks.Where(t => t.ProjectId == projectId.Value);
            }

            ViewBag.Tasks =
                await tasks
                    .OrderBy(t => t.Project!.ProjectName)
                    .ThenBy(t => t.TaskCode)
                    .ToListAsync();
        }       

        
        private async Task<bool> CanAccessTaskAsync(int projectTaskId)
        {
            var task =
                await _projectTaskRepository.ProjectTasks
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t =>
                        t.Id == projectTaskId);

            if (task == null)
            {
                return false;
            }

            return await _projectAccessService.CanAccessProjectAsync(
                User,
                task.ProjectId);
        }



        
        #endregion
    }
}
