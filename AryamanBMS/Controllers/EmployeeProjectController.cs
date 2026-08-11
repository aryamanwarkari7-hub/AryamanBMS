using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Employee,Admin,HR,Master")]
    public class EmployeeProjectController : Controller
    {
        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IProjectTaskRepository _projectTaskRepository;
        private readonly IProjectTimelineService _projectTimelineService;
        private readonly IProjectTaskProgressRepository _progressRepository;

        public EmployeeProjectController(
           UserManager<ApplicationUserModel> userManager,
           IEmployeeRepository employeeRepository,
           IProjectTaskRepository projectTaskRepository,
           IProjectTimelineService projectTimelineService,           
        IProjectTaskProgressRepository progressRepository)
        {
            _userManager = userManager;
            _employeeRepository = employeeRepository;
            _projectTaskRepository = projectTaskRepository;
            _projectTimelineService = projectTimelineService;
            _progressRepository = progressRepository;
        }

        [HttpGet]
        public async Task<IActionResult> MyTasks(
            string? searchText,
            string? status,
            string? priority)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _employeeRepository.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);

            if (employee == null)
            {
                TempData["Error"] = "Employee profile not found.";
                return RedirectToAction("MyDashboard", "Employee");
            }

            var query = _projectTaskRepository.ProjectTasks
                .AsNoTracking()
                .Include(x => x.Project)
                .Include(p => p.UpdatedByEmployee)
                .Include(x => x.AssignedEmployee)
                .Where(x =>
                    x.IsActive &&
                    x.AssignedEmployeeId == employee.Id);

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string search = searchText.Trim();

                query = query.Where(x =>
                    x.TaskCode.Contains(search) ||
                    x.TaskTitle.Contains(search) ||
                    (x.Project != null &&
                     x.Project.ProjectName.Contains(search)) ||
                    (x.Project != null &&
                     x.Project.ProjectCode.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                query = query.Where(x => x.Priority == priority);
            }

            var tasks = await query
                .OrderBy(x => x.Status == "Completed")
                .ThenBy(x => x.DueDate ?? DateTime.MaxValue)
                .ThenByDescending(x => x.Priority == "Critical")
                .ThenByDescending(x => x.Priority == "High")
                .ToListAsync();

            ViewBag.SearchText = searchText;
            ViewBag.Status = status;
            ViewBag.Priority = priority;

            return View(tasks);
        }

        [HttpGet]
        public async Task<IActionResult> TaskDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _employeeRepository.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);

            if (employee == null)
            {
                TempData["Error"] = "Employee profile not found.";
                return RedirectToAction("MyDashboard", "Employee");
            }

            var task = await _projectTaskRepository.ProjectTasks
                .AsNoTracking()
                .Include(x => x.Project)
                .Include(x => x.AssignedEmployee)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive &&
                    x.AssignedEmployeeId == employee.Id);

            if (task == null)
            {
                return Forbid();
            }

            return View(task);
        }

        #region Update Progress
        [HttpGet]
        [Authorize(Roles = "Employee,Admin,HR,Master")]
        public async Task<IActionResult> UpdateProgress(int id)
        {
            var task = await _projectTaskRepository.GetDetailsAsync(id);

            if (task == null)
                return NotFound();

            var currentUserId =
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var employee = await _employeeRepository.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.ApplicationUserId == currentUserId);

            if (employee == null || task.AssignedEmployeeId != employee.Id)
            {
                return Forbid();
            }

            if (task.Status == "Completed")
            {
                TempData["Error"] = "Completed tasks cannot be updated.";
                return RedirectToAction(nameof(TaskDetails), new { id });
            }

            return View(task);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee,Admin,HR,Master")]
        public async Task<IActionResult> UpdateProgress(ProjectTaskModel model)
        {
            var existing =
                await _projectTaskRepository.GetByIdAsync(model.Id);

            if (existing == null)
                return NotFound();

            var currentUserId =
                User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var employee = await _employeeRepository.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.ApplicationUserId == currentUserId);

            if (employee == null ||existing.AssignedEmployeeId != employee.Id)
            {
                return Forbid();
            }

            if (existing.Status == "Completed")
            {
                TempData["Error"] = "Completed tasks cannot be modified.";
                return RedirectToAction(nameof(TaskDetails), new { id = existing.Id });
            }

            string previousStatus = existing.Status;
            int previousProgress = existing.ProgressPercent;

            // Update editable fields
            existing.Status = model.Status;
            existing.ActualHours = model.ActualHours;
            existing.WorkUpdate = model.WorkUpdate;

            // Status-based business rules
            switch (existing.Status)
            {
                case "Not Started":

                    existing.ProgressPercent = 0;
                    existing.ActualStartOn = null;
                    existing.CompletedOn = null;
                    break;

                case "In Progress":

                    existing.ProgressPercent = 50;

                    existing.ActualStartOn ??= DateTime.Now;
                    existing.CompletedOn = null;
                    break;

                case "On Hold":

                    // Preserve current progress
                    existing.ActualStartOn ??= DateTime.Now;
                    existing.CompletedOn = null;
                    break;

                case "Completed":

                    existing.ProgressPercent = 100;

                    existing.ActualStartOn ??= DateTime.Now;
                    existing.CompletedOn ??= DateTime.Now;
                    break;
            }

            existing.UpdatedOn = DateTime.Now;
            existing.UpdatedByEmployeeId = employee.Id;

            await _projectTaskRepository.UpdateAsync(existing);
            await _projectTaskRepository.SaveAsync();

            var taskUpdate = new ProjectTaskProgressModel
            {
                ProjectTaskId = existing.Id,

                UpdatedByEmployeeId = employee.Id,

                ProgressDate = DateTime.Now,

                HoursWorked = existing.ActualHours,

                CompletionPercentage = existing.ProgressPercent,

                TaskStatus = existing.Status,

                ProgressNotes = string.IsNullOrWhiteSpace(existing.WorkUpdate)
                 ? "Task updated."
                 : existing.WorkUpdate.Trim(),

                CreatedOn = DateTime.Now,

                IsActive = true
            };

            await _progressRepository.AddAsync(taskUpdate);
            await _progressRepository.SaveAsync();

            if (previousStatus != existing.Status)
            {
                await _projectTimelineService.AddEventAsync(
                    projectId: existing.ProjectId,
                    eventType: "TaskStatusChanged",
                    eventTitle: "Task status updated",
                    eventDescription:
                        $"Task {existing.TaskCode} status changed from {previousStatus} to {existing.Status}.",
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
                        $"Progress changed from {previousProgress}% to {existing.ProgressPercent}%.",
                    relatedEntityType: "Task",
                    relatedEntityId: existing.Id,
                    previousValue: $"{previousProgress}%",
                    newValue: $"{existing.ProgressPercent}%");
            }

            TempData["Success"] = "Task progress updated successfully.";

            return RedirectToAction(nameof(TaskDetails), new { id = existing.Id });
        }
        #endregion
    }
}
