using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR,ProjectManager")]
    public class RiskController : Controller
    {
        private readonly IProjectRiskRepository _riskRepository;
        private readonly IProjectMemberRepository _projectMemberRepository;
        private readonly IProjectRepository _projectRepository;

        private readonly IProjectTimelineService _projectTimelineService;
        private readonly IProjectAccessService _projectAccessService;

        public RiskController(
          IProjectRiskRepository riskRepository,
          IProjectMemberRepository projectMemberRepository,
          IProjectRepository projectRepository,
          IProjectTimelineService projectTimelineService,
          IProjectAccessService projectAccessService)
        {
            _riskRepository = riskRepository;
            _projectMemberRepository = projectMemberRepository;
            _projectRepository = projectRepository;
            _projectTimelineService = projectTimelineService;
            _projectAccessService = projectAccessService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int? projectId,
            string? status,
            string? severity,
            int? ownerId,
            string? searchText)
        {
            var risks = _riskRepository.ProjectRisks;

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

                risks = risks.Where(r =>
                    r.ProjectId == projectId.Value);
            }
            else
            {
                var accessibleProjectIds =
                    await accessibleProjects
                        .Select(p => p.Id)
                        .ToListAsync();

                risks = risks.Where(r =>
                    accessibleProjectIds.Contains(r.ProjectId));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                risks = risks.Where(r =>
                    r.RiskStatus == status);
            }

            if (!string.IsNullOrWhiteSpace(severity))
            {
                risks = risks.Where(r =>
                    r.Severity == severity);
            }

            if (ownerId.HasValue)
            {
                risks = risks.Where(r =>
                    r.RiskOwnerEmployeeId == ownerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string search = searchText.Trim();

                risks = risks.Where(r =>
                    r.RiskTitle.Contains(search) ||
                    (r.RiskDescription != null &&
                     r.RiskDescription.Contains(search)) ||
                    r.Project!.ProjectCode.Contains(search) ||
                    r.Project.ProjectName.Contains(search));
            }

            var data = await risks
                .OrderByDescending(r => r.RiskScore)
                .ThenBy(r => r.TargetResolutionDate)
                .ThenByDescending(r => r.CreatedOn)
                .ToListAsync();

            await LoadDropdownsAsync();

            ViewBag.ProjectId = projectId;
            ViewBag.Status = status;
            ViewBag.Severity = severity;
            ViewBag.OwnerId = ownerId;
            ViewBag.SearchText = searchText;

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? projectId)
        {
            if (projectId.HasValue &&
            !await _projectAccessService.CanAccessProjectAsync(
             User,
             projectId.Value))
            {
                return Forbid();
            }

            await LoadDropdownsAsync(projectId);

            return View(new ProjectRiskModel
            {
                ProjectId = projectId ?? 0,
                Probability = 1,
                Impact = 1,
                RiskScore = 1,
                Severity = "Low",
                RiskStatus = "Open",
                RiskCategory = "Technical",
                IsActive = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectRiskModel model)
        {
            if (model.ProjectId > 0 &&
               !await _projectAccessService.CanAccessProjectAsync(
                   User,
                   model.ProjectId))
            {
                return Forbid();
            }
            CalculateRisk(model);

            await ValidateRiskAsync(model);

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(model.ProjectId);
                return View(model);
            }

            model.RiskTitle = model.RiskTitle.Trim();
            model.RiskDescription =
                model.RiskDescription?.Trim();
            model.MitigationPlan =
                model.MitigationPlan?.Trim();
            model.ContingencyPlan =
                model.ContingencyPlan?.Trim();
            model.ResolutionRemarks =
                model.ResolutionRemarks?.Trim();

            model.CreatedOn = DateTime.Now;
            model.IsActive = true;

            if (model.RiskStatus != "Resolved")
            {
                model.ResolvedOn = null;
            }
            else
            {
                model.ResolvedOn ??= DateTime.Today;
            }

            await _riskRepository.AddAsync(model);
            await _riskRepository.SaveAsync();

            await _projectTimelineService.AddEventAsync(
              projectId: model.ProjectId,
              eventType: "RiskCreated",
              eventTitle: "Project risk recorded",
              eventDescription:
                  $"Risk {model.RiskTitle} was recorded with " +
                  $"{model.Severity} severity.",
              relatedEntityType: "Risk",
              relatedEntityId: model.Id,
              newValue: model.RiskStatus);

            TempData["Success"] =
                "Project risk created successfully.";

            return RedirectToAction(
                nameof(Index),
                new { projectId = model.ProjectId });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var risk =
                await _riskRepository.GetDetailsAsync(id);

            if (risk == null)
                return NotFound();

            if (!await _projectAccessService.CanAccessProjectAsync(
                User,
                risk.ProjectId))
            {
                return Forbid();
            }

            return View(risk);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var risk =
                await _riskRepository.GetByIdAsync(id);

            if (risk == null)
                return NotFound();

            if (!await _projectAccessService.CanAccessProjectAsync(
              User,
              risk.ProjectId))
            {
                return Forbid();
            }

            await LoadDropdownsAsync(risk.ProjectId);

            return View(risk);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProjectRiskModel model)
        {
            if (model.ProjectId > 0 &&
              !await _projectAccessService.CanAccessProjectAsync(
                  User,
                  model.ProjectId))
            {
                return Forbid();
            }

            CalculateRisk(model);

            await ValidateRiskAsync(model);

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(model.ProjectId);
                return View(model);
            }

            var existing =
              await _riskRepository.GetByIdAsync(model.Id);

            if (existing == null)
                return NotFound();

            if (!await _projectAccessService.CanAccessProjectAsync(
              User,
              existing.ProjectId))
            {
                return Forbid();
            }

            string previousStatus =
                existing.RiskStatus ?? string.Empty;

            string previousSeverity =
                existing.Severity ?? string.Empty;

            existing.ProjectId = model.ProjectId;
            existing.RiskOwnerEmployeeId =
                model.RiskOwnerEmployeeId;
            existing.RiskTitle = model.RiskTitle.Trim();
            existing.RiskDescription =
                model.RiskDescription?.Trim();
            existing.RiskCategory = model.RiskCategory;
            existing.Probability = model.Probability;
            existing.Impact = model.Impact;
            existing.RiskScore = model.RiskScore;
            existing.Severity = model.Severity;
            existing.RiskStatus = model.RiskStatus;
            existing.MitigationPlan =
                model.MitigationPlan?.Trim();
            existing.ContingencyPlan =
                model.ContingencyPlan?.Trim();
            existing.TargetResolutionDate =
                model.TargetResolutionDate;
            existing.ResolutionRemarks =
                model.ResolutionRemarks?.Trim();
            existing.IsActive = model.IsActive;
            existing.UpdatedOn = DateTime.Now;

            if (model.RiskStatus == "Resolved")
            {
                existing.ResolvedOn =
                    model.ResolvedOn ?? DateTime.Today;
            }
            else
            {
                existing.ResolvedOn = null;
            }

            await _riskRepository.UpdateAsync(existing);
            await _riskRepository.SaveAsync();

            if (!string.Equals(
              previousStatus,
              existing.RiskStatus,
              StringComparison.OrdinalIgnoreCase))
            {
                await _projectTimelineService.AddEventAsync(
                    projectId: existing.ProjectId,
                    eventType: "RiskStatusChanged",
                    eventTitle: "Risk status changed",
                    eventDescription:
                        $"Risk {existing.RiskTitle} changed from " +
                        $"{previousStatus} to {existing.RiskStatus}.",
                    relatedEntityType: "Risk",
                    relatedEntityId: existing.Id,
                    previousValue: previousStatus,
                    newValue: existing.RiskStatus);
            }
            if (!string.Equals(
                previousSeverity,
                existing.Severity,
                StringComparison.OrdinalIgnoreCase))
            {
                await _projectTimelineService.AddEventAsync(
                    projectId: existing.ProjectId,
                    eventType: "RiskSeverityChanged",
                    eventTitle: "Risk severity changed",
                    eventDescription:
                        $"Risk {existing.RiskTitle} severity changed from " +
                        $"{previousSeverity} to {existing.Severity}.",
                    relatedEntityType: "Risk",
                    relatedEntityId: existing.Id,
                    previousValue: previousSeverity,
                    newValue: existing.Severity);
            }
            bool wasResolved =
                string.Equals(
                    previousStatus,
                    "Resolved",
                    StringComparison.OrdinalIgnoreCase);

            bool isResolved =
                string.Equals(
                    existing.RiskStatus,
                    "Resolved",
                    StringComparison.OrdinalIgnoreCase);

            if (!wasResolved && isResolved)
            {
                await _projectTimelineService.AddEventAsync(
                    projectId: existing.ProjectId,
                    eventType: "RiskResolved",
                    eventTitle: "Project risk resolved",
                    eventDescription:
                        $"Risk {existing.RiskTitle} was resolved.",
                    relatedEntityType: "Risk",
                    relatedEntityId: existing.Id,
                    previousValue: previousStatus,
                    newValue: "Resolved");
            }

            bool wasClosed =
                 string.Equals(
                     previousStatus,
                     "Closed",
                     StringComparison.OrdinalIgnoreCase);

            bool isClosed =
                string.Equals(
                    existing.RiskStatus,
                    "Closed",
                    StringComparison.OrdinalIgnoreCase);

            if (!wasClosed && isClosed)
            {
                await _projectTimelineService.AddEventAsync(
                    projectId: existing.ProjectId,
                    eventType: "RiskClosed",
                    eventTitle: "Project risk closed",
                    eventDescription:
                        $"Risk {existing.RiskTitle} was closed.",
                    relatedEntityType: "Risk",
                    relatedEntityId: existing.Id,
                    previousValue: previousStatus,
                    newValue: "Closed");
            }

            TempData["Success"] =
                "Project risk updated successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = existing.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var risk =
                await _riskRepository.GetDetailsAsync(id);

            if (risk == null)
                return NotFound();

            if (!await _projectAccessService.CanAccessProjectAsync(
              User,
              risk.ProjectId))
            {
                return Forbid();
            }

            return View(risk);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var risk =
                await _riskRepository.GetByIdAsync(id);

            if (risk == null)
                return NotFound();

            if (!await _projectAccessService.CanAccessProjectAsync(
                User,
                risk.ProjectId))
            {
                return Forbid();
            }

            int projectId = risk.ProjectId;

            await _riskRepository.DeleteAsync(risk);
            await _riskRepository.SaveAsync();

            TempData["Success"] =
                "Project risk deleted successfully.";

            return RedirectToAction(
                nameof(Index),
                new { projectId });
        }

        private async Task LoadDropdownsAsync(int? projectId = null)
        {
            var projects =
            await _projectAccessService.ApplyProjectFilterAsync(
                User,
                _projectRepository.Projects.Where(p => p.IsActive));

            ViewBag.Projects =
                await projects
                    .OrderBy(p => p.ProjectName)
                    .ToListAsync();

            if (projectId.HasValue && projectId.Value > 0)
            {
                ViewBag.Employees =
                    await GetProjectMembersAsync(projectId.Value);
            }
            else
            {
                ViewBag.Employees =
                    new List<EmployeeModel>();
            }
        }

        private static void CalculateRisk(
            ProjectRiskModel model)
        {
            model.RiskScore =
                model.Probability * model.Impact;

            model.Severity = model.RiskScore switch
            {
                <= 4 => "Low",
                <= 9 => "Medium",
                <= 16 => "High",
                _ => "Critical"
            };
        }

        private async Task ValidateRiskAsync(
            ProjectRiskModel model)
        {
            var projects =
                await _riskRepository.GetActiveProjectsAsync();

            bool projectExists =
                projects.Any(p => p.Id == model.ProjectId);

            if (!projectExists)
            {
                ModelState.AddModelError(
                    nameof(model.ProjectId),
                    "Please select a valid project.");
            }

            if (model.RiskOwnerEmployeeId.HasValue)
            {
                bool isProjectMember =
                    await _projectMemberRepository.ProjectMembers
                        .AnyAsync(pm =>
                            pm.ProjectId == model.ProjectId &&
                            pm.EmployeeId ==
                                model.RiskOwnerEmployeeId.Value &&
                            pm.IsActive);

                if (!isProjectMember)
                {
                    ModelState.AddModelError(
                        nameof(model.RiskOwnerEmployeeId),
                        "The risk owner must be an active member of the selected project.");
                }
            }

            string[] allowedCategories =
            {
                "Technical",
                "Schedule",
                "Cost",
                "Resource",
                "Quality",
                "Operational",
                "Security",
                "Compliance",
                "External",
                "Other"
            };

            if (!allowedCategories.Contains(model.RiskCategory))
            {
                ModelState.AddModelError(
                    nameof(model.RiskCategory),
                    "Invalid risk category.");
            }

            string[] allowedStatuses =
            {
                "Open",
                "Monitoring",
                "Mitigated",
                "Resolved",
                "Closed"
            };

            if (!allowedStatuses.Contains(model.RiskStatus))
            {
                ModelState.AddModelError(
                    nameof(model.RiskStatus),
                    "Invalid risk status.");
            }

            if (model.TargetResolutionDate.HasValue &&
                model.ResolvedOn.HasValue &&
                model.ResolvedOn.Value.Date <
                model.CreatedOn.Date)
            {
                ModelState.AddModelError(
                    nameof(model.ResolvedOn),
                    "Resolved date cannot be before the risk creation date.");
            }

            if (model.RiskStatus == "Resolved" &&
                string.IsNullOrWhiteSpace(
                    model.ResolutionRemarks))
            {
                ModelState.AddModelError(
                    nameof(model.ResolutionRemarks),
                    "Resolution remarks are required when the risk is resolved.");
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
    }
}