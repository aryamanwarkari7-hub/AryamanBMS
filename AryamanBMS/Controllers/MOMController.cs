using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR,ProjectManager,Employee")]
    public class MOMController : Controller
    {
        private readonly IProjectMeetingRepository _meetingRepository;
        private readonly IProjectMemberRepository _projectMemberRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IProjectTimelineService _projectTimelineService;
        private readonly IProjectAccessService _projectAccessService;

        public MOMController(
          IProjectMeetingRepository meetingRepository,
          IProjectMemberRepository projectMemberRepository,
          IProjectRepository projectRepository,
          IProjectTimelineService projectTimelineService,
          IProjectAccessService projectAccessService)
        {
            _meetingRepository = meetingRepository;
            _projectMemberRepository = projectMemberRepository;
            _projectTimelineService = projectTimelineService;
            _projectRepository = projectRepository;
            _projectAccessService = projectAccessService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int? projectId,
            string? status,
            string? searchText)
        {
            var meetings = _meetingRepository.Meetings;

            var accessibleProjects =  await GetAccessibleMomProjectsAsync();

            if (projectId.HasValue)
            {
                if (!await CanAccessMomProjectAsync(projectId.Value))
                {
                    return Forbid();
                }

                meetings = meetings.Where(m =>
                    m.ProjectId == projectId.Value);
            }
            else
            {
                var accessibleProjectIds =
                    await accessibleProjects
                        .Select(p => p.Id)
                        .ToListAsync();

                meetings = meetings.Where(m =>
                    accessibleProjectIds.Contains(m.ProjectId));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                meetings = meetings.Where(m =>
                    m.MeetingStatus == status);
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string search = searchText.Trim();

                meetings = meetings.Where(m =>
                    m.MeetingTitle.Contains(search) ||
                    m.Agenda.Contains(search) ||
                    m.Project!.ProjectCode.Contains(search) ||
                    m.Project.ProjectName.Contains(search));
            }

            var data = await meetings
                .OrderByDescending(m => m.MeetingDate)
                .ThenByDescending(m => m.StartTime)
                .ToListAsync();

            await LoadProjectsAsync();

            ViewBag.ProjectId = projectId;
            ViewBag.Status = status;
            ViewBag.SearchText = searchText;

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? projectId)
        {
            await LoadProjectsAsync();

            if (projectId.HasValue &&
             !await CanAccessMomProjectAsync(projectId.Value))
            {
                return Forbid();
            }

            return View(new ProjectMeetingModel
            {
                ProjectId = projectId ?? 0,
                MeetingDate = DateTime.Today,
                MeetingStatus = "Scheduled",
                MeetingMode = "In Person",
                StartTime = new TimeSpan(10, 0, 0),
                IsActive = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectMeetingModel model)
        {

            if (model.ProjectId > 0 &&
             !await CanAccessMomProjectAsync(model.ProjectId))
            {
                return Forbid();
            }

            await ValidateMeetingAsync(model);

            if (!await CanCreateMomAsync(model))
            {
                ModelState.AddModelError(
                    nameof(model.MeetingDate),
                    "MOM can be created by the project manager or active project members within " +
                    "24 hours of the meeting. " +  "Contact HR/Admin for late MOM entry.");
            }

            if (!ModelState.IsValid)
            {
                await LoadProjectsAsync();
                return View(model);
            }

            model.MeetingTitle = model.MeetingTitle.Trim();
            model.Agenda = model.Agenda.Trim();
            model.LocationOrLink = model.LocationOrLink?.Trim();
            model.DiscussionSummary =
                model.DiscussionSummary?.Trim();
            model.DecisionsTaken =
                model.DecisionsTaken?.Trim();

            model.MeetingStatus = "Completed";
            model.CreatedOn = DateTime.Now;
            model.IsActive = true;

            await _meetingRepository.AddAsync(model);
            await _meetingRepository.SaveAsync();

            await _projectTimelineService.AddEventAsync(
             projectId: model.ProjectId,
             eventType: "MeetingCreated",
             eventTitle: "Project meeting scheduled",
             eventDescription:
                 $"Meeting scheduled for " +
                 $"{model.MeetingDate:dd MMM yyyy hh:mm tt}.",
             relatedEntityType: "Meeting",
             relatedEntityId: model.Id,
             newValue: model.MeetingStatus);

            TempData["Success"] =
                "Meeting created successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = model.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var meeting =
                await _meetingRepository.GetDetailsAsync(id);

            if (meeting == null)
                return NotFound();

            if (!await CanAccessMomProjectAsync(meeting.ProjectId))
            {
                return Forbid();
            }

            var viewModel = new ProjectMeetingDetailsViewModel
            {
                Meeting = meeting,
                Employees = await GetProjectMembersAsync(meeting.ProjectId)
            };

            ViewBag.CanEditMeeting = await CanEditMeetingAsync(meeting);

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var meeting =
             await _meetingRepository.GetDetailsAsync(id);
 
            if (meeting == null)
                return NotFound();

            if(!await CanAccessMomProjectAsync(meeting.ProjectId))
            {
                return Forbid();
            }

            if (!await CanEditMeetingAsync(meeting))
            {
                return Forbid();
            }

            await LoadProjectsAsync();

            return View(meeting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProjectMeetingModel model)
        {
            await ValidateMeetingAsync(model);

            if (!ModelState.IsValid)
            {
                await LoadProjectsAsync();
                return View(model);
            }

            var existing =
                await _meetingRepository.GetByIdAsync(model.Id);

            if (existing == null)
                return NotFound();

            model.ProjectId = existing.ProjectId;

            if (!await CanAccessMomProjectAsync(existing.ProjectId))
            {
                return Forbid();
            }
            if (!await CanEditMeetingAsync(existing))
            {
                return Forbid();
            }

            string previousMeetingStatus =
                existing.MeetingStatus ?? string.Empty;

            
            existing.MeetingTitle = model.MeetingTitle.Trim();
            existing.MeetingDate = model.MeetingDate;
            existing.StartTime = model.StartTime;
            existing.EndTime = model.EndTime;
            existing.MeetingMode = model.MeetingMode;
            existing.LocationOrLink =
                model.LocationOrLink?.Trim();
            existing.Agenda = model.Agenda.Trim();
            existing.DiscussionSummary =
                model.DiscussionSummary?.Trim();
            existing.DecisionsTaken =
                model.DecisionsTaken?.Trim();
            existing.NextMeetingDate = model.NextMeetingDate;
            existing.MeetingStatus = model.MeetingStatus;
            existing.IsActive = model.IsActive;
            existing.UpdatedOn = DateTime.Now;

            await _meetingRepository.UpdateAsync(existing);
            await _meetingRepository.SaveAsync();

            if (!string.Equals(
              previousMeetingStatus,
              existing.MeetingStatus,
              StringComparison.OrdinalIgnoreCase))
            {
                await _projectTimelineService.AddEventAsync(
                    projectId: existing.ProjectId,
                    eventType: "MeetingStatusChanged",
                    eventTitle: "Meeting status changed",
                    eventDescription:
                        $"Meeting dated " +
                        $"{existing.MeetingDate:dd MMM yyyy} changed from " +
                        $"{previousMeetingStatus} to {existing.MeetingStatus}.",
                    relatedEntityType: "Meeting",
                    relatedEntityId: existing.Id,
                    previousValue: previousMeetingStatus,
                    newValue: existing.MeetingStatus);
            }

            bool wasCompleted =   string.Equals(
                 previousMeetingStatus,
                 "Completed",
                 StringComparison.OrdinalIgnoreCase);

            bool isCompleted =
                string.Equals(
                    existing.MeetingStatus,
                    "Completed",
                    StringComparison.OrdinalIgnoreCase);

            if (!wasCompleted && isCompleted)
            {
                await _projectTimelineService.AddEventAsync(
                    projectId: existing.ProjectId,
                    eventType: "MeetingCompleted",
                    eventTitle: "Project meeting completed",
                    eventDescription:
                        $"Meeting dated " +
                        $"{existing.MeetingDate:dd MMM yyyy} was completed.",
                    relatedEntityType: "Meeting",
                    relatedEntityId: existing.Id,
                    previousValue: previousMeetingStatus,
                    newValue: "Completed");
            }

            TempData["Success"] =
                "Meeting updated successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = existing.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var meeting =
                await _meetingRepository.GetDetailsAsync(id);

            if (meeting == null)
                return NotFound();

            if (!await CanAccessMomProjectAsync(meeting.ProjectId))
            {
                return Forbid();
            }

            return View(meeting);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var meeting =
                await _meetingRepository.GetByIdAsync(id);

            if (meeting == null)
                return NotFound();

            if (!await CanAccessMomProjectAsync(meeting.ProjectId))
            {
                return Forbid();
            }

            int projectId = meeting.ProjectId;

            meeting.IsActive = false;
            meeting.UpdatedOn = DateTime.Now;

            await _meetingRepository.UpdateAsync(meeting);
            await _meetingRepository.SaveAsync();

            TempData["Success"] =
                "Meeting deactivated successfully.";

            return RedirectToAction
                (nameof(Index),new { projectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAttendee(
            int meetingId,
            int employeeId,
            bool isPresent,
            string? remarks)
        {
            var meeting =
                await _meetingRepository.GetByIdAsync(meetingId);

            if (meeting == null)
                return NotFound();

            if(!await CanAccessMomProjectAsync(meeting.ProjectId))
            {
                return Forbid();
            }

            bool employeeExists = await IsActiveProjectMemberAsync(
                     meeting.ProjectId,
                     employeeId);

            if (!employeeExists)
            {
                TempData["Error"] =
                    "The selected employee must be an active member of this project.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = meetingId });
            }

            bool duplicate =
                await _meetingRepository.Meetings
                    .Where(m => m.Id == meetingId)
                    .SelectMany(m => m.Attendees)
                    .AnyAsync(a => a.EmployeeId == employeeId);

            if (duplicate)
            {
                TempData["Error"] =
                    "This employee is already added to the meeting.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = meetingId });
            }

            var attendee = new ProjectMeetingAttendeeModel
            {
                MeetingId = meetingId,
                EmployeeId = employeeId,
                IsPresent = isPresent,
                Remarks = remarks?.Trim(),
                CreatedOn = DateTime.Now
            };

            await _meetingRepository.AddAttendeeAsync(attendee);
            await _meetingRepository.SaveAsync();

            TempData["Success"] =
                "Meeting attendee added successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = meetingId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttendee(int id)
        {
            var attendee =
                await _meetingRepository.GetAttendeeAsync(id);

            if (attendee == null)
                return NotFound();

            var meeting =
              await _meetingRepository.GetByIdAsync(attendee.MeetingId);

            if (meeting == null)
                return NotFound();

            if (!await CanAccessMomProjectAsync(meeting.ProjectId))
            {
                return Forbid();
            }

            int meetingId = attendee.MeetingId;

            await _meetingRepository.DeleteAttendeeAsync(attendee);
            await _meetingRepository.SaveAsync();

            TempData["Success"] =
                "Meeting attendee removed successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = meetingId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddActionItem(
            ProjectMeetingActionItemModel model)
        {
            var meeting =
                await _meetingRepository.GetByIdAsync(model.MeetingId);

            if (meeting == null)
                return NotFound();

            if (!await CanAccessMomProjectAsync(meeting.ProjectId))
            {
                return Forbid();
            }

            await ValidateActionItemAsync(model, meeting);

            if (!ModelState.IsValid)
            {
                TempData["Error"] =
                    string.Join(" ",
                        ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage));

                return RedirectToAction(
                    nameof(Details),
                    new { id = model.MeetingId });
            }

            model.ActionTitle = model.ActionTitle.Trim();
            model.Description = model.Description?.Trim();
            model.ActionStatus = "Pending";
            model.CompletedOn = null;
            model.CreatedOn = DateTime.Now;
            model.IsActive = true;

            await _meetingRepository.AddActionItemAsync(model);
            await _meetingRepository.SaveAsync();

            TempData["Success"] =
                "Meeting action item added successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = model.MeetingId });
        }

        [HttpGet]
        public async Task<IActionResult> EditActionItem(int id)
        {
            var actionItem =
                await _meetingRepository.GetActionItemAsync(id);

            if (actionItem == null)
                return NotFound();

            var meeting =
                await _meetingRepository.GetByIdAsync(
                    actionItem.MeetingId);

            if (meeting == null)
                return NotFound();

            if (!await CanAccessMomProjectAsync(meeting.ProjectId))
            {
                return Forbid();
            }

            ViewBag.Employees =
                await GetProjectMembersAsync(
                    meeting.ProjectId);

            return View(actionItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditActionItem(
            ProjectMeetingActionItemModel model)
        {
            var existing =
                await _meetingRepository.GetActionItemAsync(model.Id);

            if (existing == null)
                return NotFound();

            var meeting =
                await _meetingRepository.GetByIdAsync(
                    existing.MeetingId);

            if (meeting == null)
                return NotFound();

            if (!await CanAccessMomProjectAsync(meeting.ProjectId))
            {
                return Forbid();
            }

            model.MeetingId = existing.MeetingId;

            await ValidateActionItemAsync(model, meeting);

            if (!ModelState.IsValid)
            {
                ViewBag.Employees =
                await GetProjectMembersAsync(meeting.ProjectId);

                return View(model);
            }

            existing.ActionTitle = model.ActionTitle.Trim();
            existing.Description = model.Description?.Trim();
            existing.AssignedEmployeeId =
                model.AssignedEmployeeId;
            existing.DueDate = model.DueDate;
            existing.ActionStatus = model.ActionStatus;
            existing.IsActive = model.IsActive;

            existing.CompletedOn =
                model.ActionStatus == "Completed"
                    ? model.CompletedOn ?? DateTime.Today
                    : null;

            existing.UpdatedOn = DateTime.Now;

            await _meetingRepository.UpdateActionItemAsync(existing);
            await _meetingRepository.SaveAsync();

            TempData["Success"] =
                "Meeting action item updated successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = existing.MeetingId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteActionItem(int id)
        {
            var actionItem =
                await _meetingRepository.GetActionItemAsync(id);

            if (actionItem == null)
                return NotFound();

            var meeting = await _meetingRepository.GetByIdAsync(actionItem.MeetingId);

            if (meeting == null)
                return NotFound();

            if (!await CanAccessMomProjectAsync(meeting.ProjectId))
            {
                return Forbid();
            }

            int meetingId = actionItem.MeetingId;

            actionItem.IsActive = false;
            actionItem.UpdatedOn = DateTime.Now;

            await _meetingRepository.UpdateActionItemAsync(actionItem);
            await _meetingRepository.SaveAsync();

            TempData["Success"] =
                "Meeting action item deactivated successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = meetingId });
        }

        private async Task LoadProjectsAsync()
        {
            var projects = await GetAccessibleMomProjectsAsync();

            ViewBag.Projects =
                await projects
                    .OrderBy(p => p.ProjectName)
                    .ToListAsync();
        }

        private async Task ValidateMeetingAsync(
            ProjectMeetingModel model)
        {
            bool projectExists =
                (await _meetingRepository.GetActiveProjectsAsync())
                .Any(p => p.Id == model.ProjectId);

            if (!projectExists)
            {
                ModelState.AddModelError(
                    nameof(model.ProjectId),
                    "Please select a valid project.");
            }

            string[] allowedModes =
            {
                "In Person",
                "Online",
                "Hybrid"
            };

            if (!allowedModes.Contains(model.MeetingMode))
            {
                ModelState.AddModelError(
                    nameof(model.MeetingMode),
                    "Invalid meeting mode.");
            }

            string[] allowedStatuses =
            {
                "Scheduled",
                "Completed",
                "Cancelled"
            };

            if (!allowedStatuses.Contains(model.MeetingStatus))
            {
                ModelState.AddModelError(
                    nameof(model.MeetingStatus),
                    "Invalid meeting status.");
            }

            if (model.EndTime.HasValue &&
                model.EndTime <= model.StartTime)
            {
                ModelState.AddModelError(
                    nameof(model.EndTime),
                    "End time must be after start time.");
            }

            if (string.Equals(
               model.MeetingStatus,
               "Completed",
               StringComparison.OrdinalIgnoreCase))
            {
                if (!model.EndTime.HasValue)
                {
                    ModelState.AddModelError(
                        nameof(model.EndTime),
                        "End time is required for a completed meeting.");
                }

                if (string.IsNullOrWhiteSpace(model.DiscussionSummary))
                {
                    ModelState.AddModelError(
                        nameof(model.DiscussionSummary),
                        "Discussion summary is required for MOM.");
                }
            }

            if (model.NextMeetingDate.HasValue &&
                model.NextMeetingDate.Value.Date <
                model.MeetingDate.Date)
            {
                ModelState.AddModelError(
                    nameof(model.NextMeetingDate),
                    "Next meeting date cannot be before the meeting date.");
            }
        }

        private async Task ValidateActionItemAsync(
            ProjectMeetingActionItemModel model,
            ProjectMeetingModel meeting)
        {
            string[] allowedStatuses =
            {
                "Pending",
                "In Progress",
                "Completed",
                "Cancelled"
            };

            if (!allowedStatuses.Contains(model.ActionStatus))
            {
                ModelState.AddModelError(
                    nameof(model.ActionStatus),
                    "Invalid action item status.");
            }

            if (model.DueDate.HasValue &&
                model.DueDate.Value.Date <
                meeting.MeetingDate.Date)
            {
                ModelState.AddModelError(
                    nameof(model.DueDate),
                    "Due date cannot be before the meeting date.");
            }

            if (model.AssignedEmployeeId.HasValue)
            {
                bool isProjectMember =
                    await IsActiveProjectMemberAsync(
                        meeting.ProjectId,
                        model.AssignedEmployeeId.Value);

                if (!isProjectMember)
                {
                    ModelState.AddModelError(
                        nameof(model.AssignedEmployeeId),
                        "The assigned employee must be an active member of this project.");
                }
            }
        }

        private async Task<List<EmployeeModel>> GetProjectMembersAsync(
         int projectId)
        {
            return await _projectMemberRepository.ProjectMembers
            .Where(pm =>
                pm.ProjectId == projectId &&
                pm.IsActive &&
                pm.Employee != null &&
                pm.Employee.IsActive)
            .OrderBy(pm => pm.Employee!.FirstName)
            .ThenBy(pm => pm.Employee!.LastName)
            .Select(pm => pm.Employee!)
            .ToListAsync();
        }

        private async Task<bool> IsActiveProjectMemberAsync(
            int projectId,
            int employeeId)
        {
            return await _projectMemberRepository.ProjectMembers
                .AnyAsync(pm =>
                    pm.ProjectId == projectId &&
                    pm.EmployeeId == employeeId &&
                    pm.IsActive);
        }

        private async Task<bool> CanAccessMomProjectAsync(int projectId)
        {
            if (await _projectAccessService.CanAccessProjectAsync(User, projectId))
            {
                return true;
            }

            int? currentEmployeeId =
                await _projectAccessService.GetCurrentEmployeeIdAsync(User);

            if (!currentEmployeeId.HasValue)
            {
                return false;
            }

            return await IsActiveProjectMemberAsync(
                projectId,
                currentEmployeeId.Value);
        }

        private async Task<IQueryable<ProjectModel>> GetAccessibleMomProjectsAsync()
        {
            var activeProjects =
                _projectRepository.Projects.Where(p => p.IsActive);

            if (User.IsInRole("Admin") || User.IsInRole("HR"))
            {
                return activeProjects;
            }

            int? currentEmployeeId =
                await _projectAccessService.GetCurrentEmployeeIdAsync(User);

            if (!currentEmployeeId.HasValue)
            {
                return activeProjects.Where(p => false);
            }

            var memberProjectIds =
                await _projectMemberRepository.ProjectMembers
                    .Where(pm =>
                        pm.EmployeeId == currentEmployeeId.Value &&
                        pm.IsActive)
                    .Select(pm => pm.ProjectId)
                    .ToListAsync();

            return activeProjects.Where(p =>
                p.ProjectManagerId == currentEmployeeId.Value ||
                memberProjectIds.Contains(p.Id));
        }

        private async Task<bool> CanEditMeetingAsync(ProjectMeetingModel meeting)
        {
            int? currentEmployeeId =
                await _projectAccessService.GetCurrentEmployeeIdAsync(User);

            if (!currentEmployeeId.HasValue)
            {
                return false;
            }

            var project =
                await _projectRepository.Projects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == meeting.ProjectId);

            return project != null &&
                   project.ProjectManagerId == currentEmployeeId.Value;
        }

        private async Task<bool> CanCreateMomAsync(ProjectMeetingModel model)
        {
            if (User.IsInRole("Admin") || User.IsInRole("HR"))
            {
                return true;
            }

            DateTime meetingStart =
                model.MeetingDate.Date.Add(model.StartTime);

            DateTime now = DateTime.Now;

            if (meetingStart > now ||
                now - meetingStart > TimeSpan.FromHours(24))
            {
                return false;
            }

            if (await _projectAccessService.CanAccessProjectAsync(
                User,
                model.ProjectId))
            {
                return true;
            }

            int? currentEmployeeId =
                await _projectAccessService.GetCurrentEmployeeIdAsync(User);

            if (!currentEmployeeId.HasValue)
            {
                return false;
            }

            return await IsActiveProjectMemberAsync(
                model.ProjectId,
                currentEmployeeId.Value);
        }
    }
}
