using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR,Employee,Master")]
    public class ProjectMemberController : Controller
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IProjectMemberRepository _projectMemberRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IProjectTaskRepository _projectTaskRepository;
        private readonly IProjectTimelineService _projectTimelineService;
        private readonly IProjectAccessService _projectAccessService;
        private readonly INotificationService _notificationService;

        public ProjectMemberController(
            IProjectRepository projectRepository,
            IProjectMemberRepository projectMemberRepository,
            IEmployeeRepository employeeRepository,
            IProjectTaskRepository projectTaskRepository,

            IProjectTimelineService projectTimelineService,
            IProjectAccessService projectAccessService,
            INotificationService notificationService)
        {
            _projectRepository = projectRepository;
            _projectMemberRepository = projectMemberRepository;
            _employeeRepository = employeeRepository;
            _projectTaskRepository = projectTaskRepository;

            _projectTimelineService = projectTimelineService;
            _projectAccessService = projectAccessService;
            _notificationService = notificationService;
        }


        [HttpGet]
        public IActionResult Index(int? projectId)
        {
            return RedirectToAction(
                nameof(AllMembers),
                new { projectId });
        }

        [HttpGet]
        public async Task<IActionResult> Add(int projectId)
        {
            var project = await _projectRepository.GetDetailsAsync(projectId);

            if (project == null)
                return NotFound();

            var employeeId =    await _projectAccessService.GetCurrentEmployeeIdAsync(User);

            bool canManage =
                User.IsInRole("Admin") ||
                User.IsInRole("HR") ||
                (employeeId.HasValue &&
                 await _projectRepository.Projects.AnyAsync(p =>
                     p.Id == projectId &&
                     p.ProjectManagerId == employeeId.Value));

            if (!canManage)
            {
                return Forbid();
            }

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

            var employeeId = await _projectAccessService.GetCurrentEmployeeIdAsync(User);

            bool canManage =
                User.IsInRole("Admin") ||
                User.IsInRole("HR") ||
                (employeeId.HasValue &&
                 await _projectRepository.Projects.AnyAsync(p =>
                     p.Id == model.ProjectId &&
                     p.ProjectManagerId == employeeId.Value));

            if (!canManage)
            {
                return Forbid();
            }

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

            var employee =
               await _employeeRepository.Employees
                   .AsNoTracking()
                   .FirstOrDefaultAsync(e =>
                       e.Id == model.EmployeeId);

            await _projectTimelineService.AddEventAsync(
                projectId: model.ProjectId,
                eventType: "MemberAdded",
                eventTitle: "Project member added",
                eventDescription:
                    $"{employee?.FullName ?? "Employee"} was added as " +
                    $"{model.RoleInProject}.",
                relatedEntityType: "Member",
                relatedEntityId: model.Id,
                newValue: model.RoleInProject);

            await NotifyProjectMemberChangeAsync(
                project,
                employee,
                "Project Member Added",
                $"You were added to project {project.ProjectName}.",
                "ProjectMemberAdded",
                notifyManager: true);

            TempData["Success"] =
                "Project member added successfully.";

            return RedirectToAction(
                nameof(AllMembers),
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

            var employeeId = await _projectAccessService.GetCurrentEmployeeIdAsync(User);

            bool canManage =
                User.IsInRole("Admin") ||
                User.IsInRole("HR") ||
                (employeeId.HasValue &&
                 await _projectRepository.Projects.AnyAsync(p =>
                     p.Id == member.ProjectId &&
                     p.ProjectManagerId == employeeId.Value));

            if (!canManage)
            {
                return Forbid();
            }

            if (project.ProjectManagerId == member.EmployeeId)
            {
                TempData["Error"] =
                    "The project manager cannot be removed from project members.";

                return RedirectToAction(
                    nameof(AllMembers),
                    new { projectId = member.ProjectId });
            }

            bool hasAssignedTasks = await _projectTaskRepository.ProjectTasks
            .AnyAsync(t =>
            t.ProjectId == member.ProjectId &&
            t.AssignedEmployeeId == member.EmployeeId &&
            t.IsActive);

            if (hasAssignedTasks)
            {
                TempData["Error"] =
                    "This member has active assigned tasks. Reassign those tasks before removing the member.";

                return RedirectToAction(
                   nameof(AllMembers),
                   new { projectId = member.ProjectId });
            }

            int projectId = member.ProjectId;

            var employee =
               await _employeeRepository.Employees
                   .AsNoTracking()
                   .FirstOrDefaultAsync(e =>
            e.Id == member.EmployeeId);


            int memberId = member.Id;
            string roleName =
                member.RoleInProject ?? "Project Member";
            string employeeName =
                employee?.FullName ?? "Employee";

            member.IsActive = false;
            //member.UpdatedOn = DateTime.Now;

            await _projectMemberRepository.UpdateAsync(member);
            await _projectMemberRepository.SaveAsync();

            await _projectTimelineService.AddEventAsync(
              projectId: projectId,
              eventType: "MemberRemoved",
              eventTitle: "Project member removed",
              eventDescription:
                  $"{employeeName} was removed from the project.",
              relatedEntityType: "Member",
              relatedEntityId: memberId,
              previousValue: roleName);

            await NotifyProjectMemberChangeAsync(
                project,
                employee,
                "Project Member Removed",
                $"You were removed from project {project.ProjectName}.",
                "ProjectMemberRemoved",
                notifyManager: true);

            TempData["Success"] =
                "Project member removed successfully.";

            return RedirectToAction(
                  nameof(AllMembers),
                  new { projectId });
        }

        private async Task LoadAvailableEmployeesAsync(int projectId)
        {
            var assignedEmployeeIds =
                await _projectMemberRepository.ProjectMembers
                    .Where(pm => pm.ProjectId == projectId && pm.IsActive)
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

        private async Task NotifyProjectMemberChangeAsync(
            ProjectModel project,
            EmployeeModel? memberEmployee,
            string title,
            string memberMessage,
            string notificationType,
            bool notifyManager)
        {
            if (!string.IsNullOrWhiteSpace(memberEmployee?.ApplicationUserId))
            {
                await _notificationService.CreateAsync(
                    memberEmployee.ApplicationUserId,
                    title,
                    memberMessage,
                    notificationType,
                    "Project",
                    project.Id,
                    $"/Project/Details/{project.Id}");
            }

            if (!notifyManager ||
                project.ProjectManagerId == memberEmployee?.Id)
            {
                return;
            }

            var managerUserId = await _employeeRepository.Employees
                .AsNoTracking()
                .Where(x => x.Id == project.ProjectManagerId)
                .Select(x => x.ApplicationUserId)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(managerUserId))
            {
                return;
            }

            await _notificationService.CreateAsync(
                managerUserId,
                title,
                $"{memberEmployee?.FullName ?? "A member"} change was made in project {project.ProjectName}.",
                $"{notificationType}Manager",
                "Project",
                project.Id,
                $"/Project/Details/{project.Id}");
        }

        [HttpGet]
        public async Task<IActionResult> AllMembers(
                  int? projectId,
                  string? searchText,
                  int page = 1)
        {
            const int pageSize = 10;
            page = Math.Max(page, 1);
            var members = _projectMemberRepository.ProjectMembers
                .Where(pm => pm.IsActive);

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

                members = members.Where(pm =>
                    pm.ProjectId == projectId.Value);
            }
            else
            {
                var accessibleProjectIds =
                    await accessibleProjects
                        .Select(p => p.Id)
                        .ToListAsync();

                members = members.Where(pm =>
                    accessibleProjectIds.Contains(pm.ProjectId));
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string search = searchText.Trim();

                members = members.Where(pm =>
                  (pm.Employee!.EmployeeCode != null &&
                   pm.Employee!.EmployeeCode.Contains(search)) ||
                  (pm.Employee!.FirstName != null &&
                   pm.Employee!.FirstName.Contains(search)) ||
                  (pm.Employee!.LastName != null &&
                   pm.Employee!.LastName.Contains(search)) ||
                  (pm.RoleInProject != null &&
                   pm.RoleInProject.Contains(search)) ||
                  pm.Project!.ProjectCode.Contains(search) ||
                  pm.Project!.ProjectName.Contains(search));
            }

            int totalRecords = await members.CountAsync();
            int totalPages =
             (int)Math.Ceiling(totalRecords / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var data = await members
                .OrderBy(pm => pm.Project!.ProjectName)
                .ThenBy(pm => pm.Employee!.FirstName)
                .ThenBy(pm => pm.Employee!.LastName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Projects =
             await accessibleProjects
                 .OrderBy(p => p.ProjectName)
                 .ToListAsync();

            ViewBag.ProjectId = projectId;
            ViewBag.SearchText = searchText;
            ViewBag.CurrentPage = page;

            bool canManageProject =
              User.IsInRole("Admin") ||
              User.IsInRole("HR");

            if (!canManageProject && projectId.HasValue)
            {
                var employeeId =
                    await _projectAccessService.GetCurrentEmployeeIdAsync(User);

                if (employeeId.HasValue)
                {
                    canManageProject =
                        await _projectRepository.Projects.AnyAsync(p =>
                            p.Id == projectId.Value &&
                            p.ProjectManagerId == employeeId.Value);
                }
            }

            ViewBag.CanManageProject = canManageProject;

            return View(data);
        }
    }
}
