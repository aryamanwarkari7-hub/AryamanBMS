using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR,ProjectManager")]
    public class ProjectTimelineController : Controller
    {
        private readonly IProjectRepository
            _projectRepository;

        private readonly IProjectTimelineRepository
            _timelineRepository;

        private readonly IEmployeeRepository
            _employeeRepository;

        public ProjectTimelineController(
            IProjectRepository projectRepository,
            IProjectTimelineRepository timelineRepository,
            IEmployeeRepository employeeRepository)
        {
            _projectRepository = projectRepository;
            _timelineRepository = timelineRepository;
            _employeeRepository = employeeRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int projectId,
            string? eventType,
            DateTime? fromDate,
            DateTime? toDate,
            string sortOrder = "oldest")
        {
            var project =
                await _projectRepository.GetDetailsAsync(projectId);

            if (project == null)
                return NotFound();

            var timelineQuery =
                _timelineRepository.ProjectTimelines
                    .AsNoTracking()
                    .Where(t =>
                        t.ProjectId == projectId &&
                        t.IsActive);

            if (!string.IsNullOrWhiteSpace(eventType))
            {
                timelineQuery =
                    timelineQuery.Where(t =>
                        t.EventType == eventType);
            }

            if (fromDate.HasValue)
            {
                timelineQuery =
                    timelineQuery.Where(t =>
                        t.EventDate.Date >=
                        fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                timelineQuery =
                    timelineQuery.Where(t =>
                        t.EventDate.Date <=
                        toDate.Value.Date);
            }

            timelineQuery =
                string.Equals(
                    sortOrder,
                    "newest",
                    StringComparison.OrdinalIgnoreCase)
                ? timelineQuery
                    .OrderByDescending(t => t.EventDate)
                    .ThenByDescending(t => t.Id)
                : timelineQuery
                    .OrderBy(t => t.EventDate)
                    .ThenBy(t => t.Id);

            var events =
                await timelineQuery.ToListAsync();

            var eventTypes =
                await _timelineRepository.ProjectTimelines
                    .AsNoTracking()
                    .Where(t =>
                        t.ProjectId == projectId &&
                        t.IsActive)
                    .Select(t => t.EventType)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToListAsync();

            string projectManagerName =
                "Not Assigned";

            if (project.ProjectManagerId > 0)
            {
                var manager =
                    await _employeeRepository.Employees
                        .AsNoTracking()
                        .FirstOrDefaultAsync(e =>
                            e.Id == project.ProjectManagerId);

                projectManagerName =
                    manager?.FullName
                    ?? "Not Assigned";
            }

            var viewModel =
                new ProjectTimelineViewModel
                {
                    Project = project,
                    Events = events,
                    ProjectManagerName =
                        projectManagerName,
                    SelectedEventType = eventType,
                    FromDate = fromDate,
                    ToDate = toDate,
                    SortOrder = sortOrder,
                    EventTypes = eventTypes
                };

            return View(viewModel);
        }
    }
}