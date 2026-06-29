using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR,ProjectManager")]
    public class TaskTrackerController : Controller
    {
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
            int? projectTaskId)
        {
            var progressRecords = _progressRepository.ProjectTaskProgresses
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

                progressRecords = progressRecords.Where(p =>
                    p.ProjectTask!.ProjectId == projectId.Value);
            }
            else
            {
                var accessibleProjectIds =
                    await accessibleProjects
                        .Select(p => p.Id)
                        .ToListAsync();

                progressRecords = progressRecords.Where(p =>
                    accessibleProjectIds.Contains(
                        p.ProjectTask!.ProjectId));
            }

            if (projectTaskId.HasValue)
            {
                if (!await CanAccessTaskAsync(projectTaskId.Value))
                {
                    return Forbid();
                }

                progressRecords = progressRecords.Where(p =>
                    p.ProjectTaskId == projectTaskId.Value);
            }

            var data = await progressRecords
                .OrderByDescending(p => p.ProgressDate)
                .ThenByDescending(p => p.Id)
                .ToListAsync();

            await LoadTasksAsync();

            ViewBag.ProjectId = projectId;
            ViewBag.ProjectTaskId = projectTaskId;

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? projectTaskId)
        {
            await LoadTasksAsync();

            if (projectTaskId.HasValue &&
                !await CanAccessTaskAsync(projectTaskId.Value))
            {
                return Forbid();
            }

            return View(new ProjectTaskProgressModel
            {
                ProjectTaskId = projectTaskId ?? 0,
                ProgressDate = DateTime.Today,
                IsActive = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ProjectTaskProgressModel model)
        {
            if (model.ProjectTaskId > 0 &&
                !await CanAccessTaskAsync(model.ProjectTaskId))
            {
                return Forbid();
            }

            await ValidateProgressAsync(model);

            if (!ModelState.IsValid)
            {
                await LoadTasksAsync();
                return View(model);
            }

            model.ProgressNotes = model.ProgressNotes.Trim();
            model.CreatedOn = DateTime.Now;
            model.IsActive = true;

            await _progressRepository.AddAsync(model);
            await _progressRepository.SaveAsync();

            await SyncProjectTaskAsync(model.ProjectTaskId);

            TempData["Success"] =
                "Task progress added and project task updated successfully.";

            return RedirectToAction(
                nameof(Index),
                new { projectTaskId = model.ProjectTaskId });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var progress =
                await _progressRepository.GetDetailsAsync(id);

            if (progress == null)
                return NotFound();

            if (!await CanAccessTaskAsync(progress.ProjectTaskId))
            {
                return Forbid();
            }

            return View(progress);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var progress =
                await _progressRepository.GetByIdAsync(id);

            if (progress == null)
                return NotFound();

            if (!await CanAccessTaskAsync(progress.ProjectTaskId))
            {
                return Forbid();
            }

            await LoadTasksAsync();

            return View(progress);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            ProjectTaskProgressModel model)
        {
            if (model.ProjectTaskId > 0 &&
                !await CanAccessTaskAsync(model.ProjectTaskId))
            {
                return Forbid();
            }

            await ValidateProgressAsync(model);

            if (!ModelState.IsValid)
            {
                await LoadTasksAsync();
                return View(model);
            }

            var existing =
                await _progressRepository.GetByIdAsync(model.Id);

            if (existing == null)
                return NotFound();

            if (!await CanAccessTaskAsync(existing.ProjectTaskId))
            {
                return Forbid();
            }

            int previousProjectTaskId = existing.ProjectTaskId;

            existing.ProjectTaskId = model.ProjectTaskId;
            existing.ProgressDate = model.ProgressDate;
            existing.HoursWorked = model.HoursWorked;
            existing.CompletionPercentage =
                model.CompletionPercentage;
            existing.ProgressNotes =
                model.ProgressNotes.Trim();
            existing.IsActive = model.IsActive;
            existing.UpdatedOn = DateTime.Now;

            await _progressRepository.UpdateAsync(existing);
            await _progressRepository.SaveAsync();

            await SyncProjectTaskAsync(existing.ProjectTaskId);

            if (previousProjectTaskId != existing.ProjectTaskId)
            {
                await SyncProjectTaskAsync(previousProjectTaskId);
            }

            TempData["Success"] =
                "Task progress and project task updated successfully.";

            return RedirectToAction(
                nameof(Index),
                new { projectTaskId = existing.ProjectTaskId });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var progress =
                await _progressRepository.GetDetailsAsync(id);

            if (progress == null)
                return NotFound();

            if (!await CanAccessTaskAsync(progress.ProjectTaskId))
            {
                return Forbid();
            }

            return View(progress);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var progress =
                await _progressRepository.GetByIdAsync(id);

            if (progress == null)
                return NotFound();

            if (!await CanAccessTaskAsync(progress.ProjectTaskId))
            {
                return Forbid();
            }

            int projectTaskId = progress.ProjectTaskId;

            progress.IsActive = false;
            progress.UpdatedOn = DateTime.Now;

            await _progressRepository.UpdateAsync(progress);
            await _progressRepository.SaveAsync();

            await SyncProjectTaskAsync(projectTaskId);

            TempData["Success"] =
               "Task progress deleted and project task recalculated successfully.";

            return RedirectToAction(
                nameof(Index),
                new { projectTaskId });
        }

        private async Task LoadTasksAsync()
        {
            var accessibleProjects =
                await _projectAccessService.ApplyProjectFilterAsync(
                    User,
                    _projectRepository.Projects.Where(p => p.IsActive));

            var accessibleProjectIds =
                await accessibleProjects
                    .Select(p => p.Id)
                    .ToListAsync();

            ViewBag.Tasks =
                await _projectTaskRepository.ProjectTasks
                    .Include(t => t.Project)
                    .Where(t => t.IsActive &&
                          accessibleProjectIds.Contains(t.ProjectId))
                    .OrderBy(t => t.Project!.ProjectName)
                    .ThenBy(t => t.TaskCode)
                    .ToListAsync();
        }

        private async Task ValidateProgressAsync(
            ProjectTaskProgressModel model)
        {
            bool taskExists =
                await _projectTaskRepository.ProjectTasks
                    .AnyAsync(t => t.Id == model.ProjectTaskId);

            if (!taskExists)
            {
                ModelState.AddModelError(
                    nameof(model.ProjectTaskId),
                    "Please select a valid project task.");
            }

            if (model.ProgressDate.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.ProgressDate),
                    "Progress date cannot be in the future.");
            }
        }

        private async Task SyncProjectTaskAsync(int projectTaskId)
        {
            var task =
                await _projectTaskRepository.GetByIdAsync(projectTaskId);

            if (task == null)
                return;

            var activeProgressRecords =
                _progressRepository.ProjectTaskProgresses
                    .Where(p =>
                        p.ProjectTaskId == projectTaskId &&
                        p.IsActive);

            task.ActualHours =
                await activeProgressRecords
                    .SumAsync(p => (decimal?)p.HoursWorked)
                ?? 0;

            var latestProgress =
                await activeProgressRecords
                    .OrderByDescending(p => p.ProgressDate)
                    .ThenByDescending(p => p.Id)
                    .FirstOrDefaultAsync();

            task.ProgressPercent =
                latestProgress?.CompletionPercentage ?? 0;

            task.Status = task.ProgressPercent switch
            {
                >= 100 => "Completed",
                > 0 => "In Progress",
                _ => "Not Started"
            };

            task.UpdatedOn = DateTime.Now;

            await _projectTaskRepository.UpdateAsync(task);
            await _projectTaskRepository.SaveAsync();
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
    }
}