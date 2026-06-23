using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class TaskTrackerController : Controller
    {
        private readonly IProjectTaskRepository _projectTaskRepository;
        private readonly IProjectTaskProgressRepository _progressRepository;

        public TaskTrackerController(
            IProjectTaskRepository projectTaskRepository,
            IProjectTaskProgressRepository progressRepository)
        {
            _projectTaskRepository = projectTaskRepository;
            _progressRepository = progressRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int? projectId,
            int? projectTaskId)
        {
            var progressRecords =
                _progressRepository.ProjectTaskProgresses;

            if (projectId.HasValue)
            {
                progressRecords = progressRecords.Where(p =>
                    p.ProjectTask!.ProjectId == projectId.Value);
            }

            if (projectTaskId.HasValue)
            {
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

            TempData["Success"] =
                "Task progress added successfully.";

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

            return View(progress);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var progress =
                await _progressRepository.GetByIdAsync(id);

            if (progress == null)
                return NotFound();

            await LoadTasksAsync();

            return View(progress);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            ProjectTaskProgressModel model)
        {
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

            TempData["Success"] =
                "Task progress updated successfully.";

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

            int projectTaskId = progress.ProjectTaskId;

            await _progressRepository.DeleteAsync(progress);
            await _progressRepository.SaveAsync();

            TempData["Success"] =
                "Task progress deleted successfully.";

            return RedirectToAction(
                nameof(Index),
                new { projectTaskId });
        }

        private async Task LoadTasksAsync()
        {
            ViewBag.Tasks =
                await _projectTaskRepository.ProjectTasks
                    .Include(t => t.Project)
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
    }
}