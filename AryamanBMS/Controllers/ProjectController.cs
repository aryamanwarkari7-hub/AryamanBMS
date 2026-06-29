using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
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

        private readonly IProjectTimelineService _projectTimelineService;
        private readonly IProjectAccessService _projectAccessService;

        public ProjectController(
           IProjectRepository projectRepository,
           IEmployeeRepository employeeRepository,
           IProjectMemberRepository projectMemberRepository,
           IProjectTaskRepository projectTaskRepository,
           IProjectRiskRepository projectRiskRepository,
           IProjectMeetingRepository projectMeetingRepository,
           IProjectFlowRepository projectFlowRepository,

           IProjectTimelineService projectTimelineService,
           IProjectAccessService projectAccessService)
        {
            _projectRepository = projectRepository;
            _employeeRepository = employeeRepository;
            _projectMemberRepository = projectMemberRepository;
            _projectTaskRepository = projectTaskRepository;
            _projectRiskRepository = projectRiskRepository;
            _projectMeetingRepository = projectMeetingRepository;
            _projectFlowRepository = projectFlowRepository;

            _projectTimelineService = projectTimelineService;
            _projectAccessService = projectAccessService;
        }

        public async Task<IActionResult> Index(
            string? searchText,
            string? status,
            string? priority,
            int page = 1)
        {
            const int pageSize = 5;

            var projects = _projectRepository.Projects;

            projects =
             await _projectAccessService.ApplyProjectFilterAsync(
                 User,
                 projects);

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
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create()
        {
            await LoadEmployeesAsync();
            return View(new ProjectModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
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

            await _projectTimelineService.AddEventAsync(
             projectId: model.Id,
             eventType: "ProjectCreated",
             eventTitle: "Project created",
             eventDescription:
                 $"Project {model.ProjectCode} - {model.ProjectName} was created.",
             relatedEntityType: "Project",
             relatedEntityId: model.Id,
             newValue: model.Status);

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

            if (!await _projectAccessService.CanAccessProjectAsync(User, id))
            {
                return Forbid();
            }

            return View(project);
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard(int id)
        {
            var project =
                await _projectRepository.GetDetailsAsync(id);

            if (project == null)
                return NotFound();

            if (!await _projectAccessService.CanAccessProjectAsync(User, id))
            {
                return Forbid();
            }

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

            var ganttTaskRecords =
              await tasks
                  .Include(t => t.AssignedEmployee)
                  .Where(t =>
                      t.StartDate.HasValue &&
                      t.DueDate.HasValue)
                  .OrderBy(t => t.StartDate)
                  .ThenBy(t => t.DueDate)
                  .ToListAsync();

            DateTime timelineStart;
            DateTime timelineEnd;

            if (ganttTaskRecords.Any())
            {
                timelineStart =
                    ganttTaskRecords
                        .Min(t => t.StartDate!.Value.Date);

                timelineEnd =
                    ganttTaskRecords
                        .Max(t => t.DueDate!.Value.Date);
            }
            else
            {
                timelineStart = project.StartDate.HasValue
                    ? project.StartDate.Value.Date
                    : DateTime.Today;

                timelineEnd = project.EndDate.HasValue
                    ? project.EndDate.Value.Date
                    : timelineStart;
            }

            if (timelineEnd < timelineStart)
            {
                timelineEnd = timelineStart;
            }

            int totalTimelineDays =
                Math.Max(
                    1,
                    (timelineEnd - timelineStart).Days + 1);

            string projectManagerName = "Not Assigned";

            // Primary source: Project.ProjectManagerId
            if (project.ProjectManagerId > 0)
            {
                var managerEmployee =
                    await _employeeRepository.Employees
                        .AsNoTracking()
                        .FirstOrDefaultAsync(e =>
                            e.Id == project.ProjectManagerId);

                if (managerEmployee != null)
                {
                    projectManagerName =
                        managerEmployee.FullName;
                }
            }

            // Fallback: Project Member role
            if (projectManagerName == "Not Assigned")
            {
                var managerMember =
                    await _projectMemberRepository.ProjectMembers
                        .AsNoTracking()
                        .Include(pm => pm.Employee)
                        .Where(pm =>
                            pm.ProjectId == id &&
                            pm.IsActive &&
                            pm.RoleInProject != null)
                        .ToListAsync();

                var matchedManager =
                    managerMember.FirstOrDefault(pm =>
                        pm.RoleInProject!.Equals(
                            "Project Manager",
                            StringComparison.OrdinalIgnoreCase));

                projectManagerName =
                    matchedManager?.Employee?.FullName
                    ?? "Not Assigned";
            }

            var ganttModel = new ProjectGanttViewModel
            {
                TimelineStart = timelineStart,
                TimelineEnd = timelineEnd,
                TotalTimelineDays = totalTimelineDays,

                ProjectManagerName = projectManagerName
            };

            // Create timeline date labels.
            int labelInterval =
                totalTimelineDays <= 14
                    ? 1
                    : totalTimelineDays <= 45
                        ? 5
                        : 7;

            for (int dayOffset = 0;
                 dayOffset < totalTimelineDays;
                 dayOffset += labelInterval)
            {
                DateTime labelDate =
                    timelineStart.AddDays(dayOffset);

                decimal positionPercent =
                    Math.Round(
                        (decimal)dayOffset /
                        totalTimelineDays * 100,
                        2);

                ganttModel.TimelineDates.Add(
                    new ProjectGanttDateViewModel
                    {
                        Date = labelDate,
                        PositionPercent = positionPercent
                    });
            }

            // Ensure the final timeline date is visible.
            if (!ganttModel.TimelineDates.Any(x =>
                    x.Date.Date == timelineEnd.Date))
            {
                ganttModel.TimelineDates.Add(
                    new ProjectGanttDateViewModel
                    {
                        Date = timelineEnd,
                        PositionPercent = 100
                    });
            }

            // Calculate today's marker position.
            // Calculate today's marker position.
            if (DateTime.Today.Date >= timelineStart.Date &&
                DateTime.Today.Date <= timelineEnd.Date)
            {
                int todayOffset =
                    (DateTime.Today.Date -
                     timelineStart.Date).Days;

                int timelineSpanDays =
                    Math.Max(
                        1,
                        (timelineEnd.Date -
                         timelineStart.Date).Days);

                ganttModel.TodayPositionPercent =
                    Math.Round(
                        (decimal)todayOffset /
                        timelineSpanDays * 100,
                        2);
            }
            else
            {
                ganttModel.TodayPositionPercent = null;
            }

            foreach (var task in ganttTaskRecords)
            {
                DateTime taskStart =
                    task.StartDate!.Value.Date;

                DateTime taskEnd =
                    task.DueDate!.Value.Date;

                if (taskEnd < taskStart)
                {
                    taskEnd = taskStart;
                }

                int startOffsetDays =
                    Math.Max(
                        0,
                        (taskStart - timelineStart).Days);

                int durationDays =
                    Math.Max(
                        1,
                        (taskEnd - taskStart).Days + 1);

                decimal leftPercent =
                    Math.Round(
                        (decimal)startOffsetDays /
                        totalTimelineDays * 100,
                        2);

                decimal widthPercent =
                    Math.Round(
                        (decimal)durationDays /
                        totalTimelineDays * 100,
                        2);

                // Prevent a task bar from crossing the timeline boundary.
                if (leftPercent + widthPercent > 100)
                {
                    widthPercent =
                        Math.Max(
                            0,
                            100 - leftPercent);
                }

                bool isCompleted = string.Equals(
                   task.Status,
                   "Completed",
                   StringComparison.OrdinalIgnoreCase);

                bool isOverdue =
                    taskEnd < DateTime.Today &&
                    !isCompleted;

                int displayProgress =
                    isCompleted
                        ? 100
                        : Math.Clamp(
                            task.ProgressPercent,
                            0,
                            100);

                int overdueDays =
                    isOverdue
                        ? (DateTime.Today - taskEnd).Days
                        : 0;

                ganttModel.Tasks.Add(new ProjectGanttTaskViewModel
                {
                    TaskId = task.Id,
                    TaskCode = task.TaskCode ?? string.Empty,
                    TaskTitle = task.TaskTitle ?? string.Empty,
                    AssignedEmployeeId =
                          task.AssignedEmployeeId,

                    AssignedEmployeeName =
                          task.AssignedEmployee?.FullName
                          ?? "Unassigned",

                    StartDate = taskStart,
                    DueDate = taskEnd,

                    Status = task.Status ?? string.Empty,

                    ProgressPercent =
                            Math.Clamp(
                                task.ProgressPercent,
                                0,
                                100),

                    StartOffsetDays = startOffsetDays,
                    DurationDays = durationDays,

                    LeftPercent = leftPercent,
                    WidthPercent = widthPercent,

                    IsOverdue = isOverdue,

                    IsMilestone = durationDays == 1,

                    DisplayProgressPercent = displayProgress,

                    OverdueDays = overdueDays,
                });
            }

            var viewModel = new ProjectDashboardViewModel
            {
                Project = project,

                Gantt = ganttModel,

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
        [Authorize(Roles = "Admin,HR")]
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
        [Authorize(Roles = "Admin,HR")]
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

            string previousStatus =
                 existing.Status ?? string.Empty;

            int previousManagerId =
                existing.ProjectManagerId;

            existing.ProjectCode =
                model.ProjectCode.Trim().ToUpper();

            existing.ProjectName =
                model.ProjectName.Trim();

            //existing.ProjectManagerId = model.ProjectManagerId;

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

            // Record project status change.
            if (!string.Equals(
                previousStatus,
                existing.Status,
                StringComparison.OrdinalIgnoreCase))
            {
                await _projectTimelineService.AddEventAsync(
                    projectId: existing.Id,
                    eventType: "ProjectStatusChanged",
                    eventTitle: "Project status changed",
                    eventDescription:
                        $"Project status changed from " +
                        $"{previousStatus} to {existing.Status}.",
                    relatedEntityType: "Project",
                    relatedEntityId: existing.Id,
                    previousValue: previousStatus,
                    newValue: existing.Status);
            }

            // Record project manager change.
            if (previousManagerId != existing.ProjectManagerId)
            {
                string previousManagerName = "Not Assigned";
                string newManagerName = "Not Assigned";

                if (previousManagerId > 0)
                {
                    var previousManager =
                        await _employeeRepository.Employees
                            .AsNoTracking()
                            .FirstOrDefaultAsync(e =>
                                e.Id == previousManagerId);

                    previousManagerName =
                        previousManager?.FullName
                        ?? "Not Assigned";
                }

                if (existing.ProjectManagerId > 0)
                {
                    var newManager =
                        await _employeeRepository.Employees
                            .AsNoTracking()
                            .FirstOrDefaultAsync(e =>
                                e.Id == existing.ProjectManagerId);

                    newManagerName =
                        newManager?.FullName
                        ?? "Not Assigned";
                }

                await _projectTimelineService.AddEventAsync(
                    projectId: existing.Id,
                    eventType: "ProjectManagerChanged",
                    eventTitle: "Project manager changed",
                    eventDescription:
                        $"Project manager changed from " +
                        $"{previousManagerName} to {newManagerName}.",
                    relatedEntityType: "Project",
                    relatedEntityId: existing.Id,
                    previousValue: previousManagerName,
                    newValue: newManagerName);
            }



            // Ensure the newly selected project manager is an active project member.
            // Synchronize project manager with project members.
            if (previousManagerId != model.ProjectManagerId)
            {
                ProjectMemberModel? previousManagerMember = null;
                ProjectMemberModel? newManagerMember = null;

                string previousManagerRole = "Project Manager";
                string newManagerPreviousRole = "Not Assigned";

                // Change the previous manager back to Project Member.
                if (previousManagerId > 0)
                {
                    previousManagerMember =
                        await _projectMemberRepository.ProjectMembers
                            .FirstOrDefaultAsync(pm =>
                                pm.ProjectId == existing.Id &&
                                pm.EmployeeId == previousManagerId);

                    if (previousManagerMember != null)
                    {
                        previousManagerRole =
                            previousManagerMember.RoleInProject
                            ?? "Project Manager";

                        await _projectMemberRepository.UpdateMemberRoleAsync(
                            existing.Id,
                            previousManagerId,
                            "Project Member");
                    }
                }

                // Add or promote the newly selected manager.
                if (model.ProjectManagerId > 0)
                {
                    newManagerMember =
                        await _projectMemberRepository.ProjectMembers
                            .FirstOrDefaultAsync(pm =>
                                pm.ProjectId == existing.Id &&
                                pm.EmployeeId == model.ProjectManagerId);

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
                        newManagerPreviousRole =
                            newManagerMember.RoleInProject
                            ?? "Project Member";

                        await _projectMemberRepository.UpdateMemberRoleAsync(
                            existing.Id,
                            model.ProjectManagerId,
                            "Project Manager");
                    }
                }

                // Save all member changes together.
                await _projectMemberRepository.SaveAsync();

                // Record previous manager role change.
                if (previousManagerMember != null)
                {
                    var previousManagerEmployee =
                        await _employeeRepository.Employees
                            .AsNoTracking()
                            .FirstOrDefaultAsync(e =>
                                e.Id == previousManagerId);

                    await _projectTimelineService.AddEventAsync(
                        projectId: existing.Id,
                        eventType: "MemberRoleChanged",
                        eventTitle: "Project member role changed",
                        eventDescription:
                            $"{previousManagerEmployee?.FullName ?? "Employee"} " +
                            $"changed from {previousManagerRole} to Project Member.",
                        relatedEntityType: "Member",
                        relatedEntityId: previousManagerMember.Id,
                        previousValue: previousManagerRole,
                        newValue: "Project Member");
                }

                // Record new manager member/role change.
                if (newManagerMember != null)
                {
                    var newManagerEmployee =
                        await _employeeRepository.Employees
                            .AsNoTracking()
                            .FirstOrDefaultAsync(e =>
                                e.Id == model.ProjectManagerId);

                    if (newManagerPreviousRole == "Not Assigned")
                    {
                        await _projectTimelineService.AddEventAsync(
                            projectId: existing.Id,
                            eventType: "MemberAdded",
                            eventTitle: "Project member added",
                            eventDescription:
                                $"{newManagerEmployee?.FullName ?? "Employee"} " +
                                $"was added as Project Manager.",
                            relatedEntityType: "Member",
                            relatedEntityId: newManagerMember.Id,
                            newValue: "Project Manager");
                    }
                    else if (!string.Equals(
                        newManagerPreviousRole,
                        "Project Manager",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        await _projectTimelineService.AddEventAsync(
                            projectId: existing.Id,
                            eventType: "MemberRoleChanged",
                            eventTitle: "Project member role changed",
                            eventDescription:
                                $"{newManagerEmployee?.FullName ?? "Employee"} " +
                                $"changed from {newManagerPreviousRole} " +
                                $"to Project Manager.",
                            relatedEntityType: "Member",
                            relatedEntityId: newManagerMember.Id,
                            previousValue: newManagerPreviousRole,
                            newValue: "Project Manager");
                    }
                }
            }

            TempData["Success"] = "Project updated successfully.";

            return RedirectToAction(nameof(Details), new { id = existing.Id });
        }

        [HttpGet]
        [Authorize(Roles = "Admin,HR")]
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
        [Authorize(Roles = "Admin,HR")]
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