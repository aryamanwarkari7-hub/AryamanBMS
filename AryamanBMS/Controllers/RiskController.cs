using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
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
        public RiskController(
         IProjectRiskRepository riskRepository,
         IProjectMemberRepository projectMemberRepository)
        {
            _riskRepository = riskRepository;
            _projectMemberRepository = projectMemberRepository;
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

            if (projectId.HasValue)
            {
                risks = risks.Where(r =>
                    r.ProjectId == projectId.Value);
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
        public async Task<IActionResult> Create(
            ProjectRiskModel model)
        {
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

            return View(risk);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var risk =
                await _riskRepository.GetByIdAsync(id);

            if (risk == null)
                return NotFound();

            await LoadDropdownsAsync(risk.ProjectId);

            return View(risk);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            ProjectRiskModel model)
        {
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
            ViewBag.Projects =
                await _riskRepository.GetActiveProjectsAsync();

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