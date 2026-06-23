using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class ProjectFlowController : Controller
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IProjectFlowRepository _projectFlowRepository;

        public ProjectFlowController(
            IProjectRepository projectRepository,
            IProjectFlowRepository projectFlowRepository)
        {
            _projectRepository = projectRepository;
            _projectFlowRepository = projectFlowRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? projectId)
        {
            var flows = _projectFlowRepository.ProjectFlows;

            if (projectId.HasValue)
            {
                flows = flows.Where(pf =>
                    pf.ProjectId == projectId.Value);
            }

            var data = await flows
                .OrderBy(pf => pf.Project!.ProjectName)
                .ThenBy(pf => pf.StageOrder)
                .ToListAsync();

            ViewBag.Projects = await _projectRepository.Projects
                .Where(p => p.IsActive)
                .OrderBy(p => p.ProjectName)
                .ToListAsync();

            ViewBag.ProjectId = projectId;

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? projectId)
        {
            await LoadProjectsAsync();

            return View(new ProjectFlowModel
            {
                ProjectId = projectId ?? 0,
                StageStatus = "Pending",
                StageOrder = await GetNextStageOrderAsync(projectId)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectFlowModel model)
        {
            await ValidateFlowAsync(model);

            if (!ModelState.IsValid)
            {
                await LoadProjectsAsync();
                return View(model);
            }

            model.StageName = model.StageName.Trim();
            model.CreatedOn = DateTime.Now;
            model.IsActive = true;

            await _projectFlowRepository.AddAsync(model);
            await _projectFlowRepository.SaveAsync();

            TempData["Success"] =
                "Project flow stage created successfully.";

            return RedirectToAction(
                nameof(Index),
                new { projectId = model.ProjectId });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var flow =
                await _projectFlowRepository.GetDetailsAsync(id);

            if (flow == null)
                return NotFound();

            return View(flow);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var flow =
                await _projectFlowRepository.GetByIdAsync(id);

            if (flow == null)
                return NotFound();

            await LoadProjectsAsync();

            return View(flow);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProjectFlowModel model)
        {
            await ValidateFlowAsync(model);

            if (!ModelState.IsValid)
            {
                await LoadProjectsAsync();
                return View(model);
            }

            var existing =
                await _projectFlowRepository.GetByIdAsync(model.Id);

            if (existing == null)
                return NotFound();

            existing.ProjectId = model.ProjectId;
            existing.StageName = model.StageName.Trim();
            existing.StageOrder = model.StageOrder;
            existing.StageStatus = model.StageStatus;
            existing.PlannedStartDate = model.PlannedStartDate;
            existing.PlannedEndDate = model.PlannedEndDate;
            existing.ActualStartDate = model.ActualStartDate;
            existing.ActualEndDate = model.ActualEndDate;
            existing.Remarks = model.Remarks;
            existing.IsActive = model.IsActive;
            existing.UpdatedOn = DateTime.Now;

            await _projectFlowRepository.UpdateAsync(existing);
            await _projectFlowRepository.SaveAsync();

            TempData["Success"] =
                "Project flow stage updated successfully.";

            return RedirectToAction(
                nameof(Index),
                new { projectId = existing.ProjectId });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var flow =
                await _projectFlowRepository.GetDetailsAsync(id);

            if (flow == null)
                return NotFound();

            return View(flow);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var flow =
                await _projectFlowRepository.GetByIdAsync(id);

            if (flow == null)
                return NotFound();

            int projectId = flow.ProjectId;

            await _projectFlowRepository.DeleteAsync(flow);
            await _projectFlowRepository.SaveAsync();

            TempData["Success"] =
                "Project flow stage deleted successfully.";

            return RedirectToAction(
                nameof(Index),
                new { projectId });
        }

        private async Task LoadProjectsAsync()
        {
            ViewBag.Projects = await _projectRepository.Projects
                .Where(p => p.IsActive)
                .OrderBy(p => p.ProjectName)
                .ToListAsync();
        }

        private async Task<int> GetNextStageOrderAsync(int? projectId)
        {
            if (!projectId.HasValue)
                return 1;

            int highestOrder =
                await _projectFlowRepository.ProjectFlows
                    .Where(pf => pf.ProjectId == projectId.Value)
                    .Select(pf => (int?)pf.StageOrder)
                    .MaxAsync() ?? 0;

            return highestOrder + 1;
        }

        private async Task ValidateFlowAsync(ProjectFlowModel model)
        {
            bool duplicateOrder =
                await _projectFlowRepository.ProjectFlows
                    .AnyAsync(pf =>
                        pf.ProjectId == model.ProjectId &&
                        pf.StageOrder == model.StageOrder &&
                        pf.Id != model.Id);

            if (duplicateOrder)
            {
                ModelState.AddModelError(
                    nameof(model.StageOrder),
                    "This stage order already exists for the selected project.");
            }

            string[] allowedStatuses =
            {
              "Pending",
              "In Progress",
              "Completed",
              "On Hold"
    };

            if (!allowedStatuses.Contains(model.StageStatus))
            {
                ModelState.AddModelError(
                    nameof(model.StageStatus),
                    "Invalid stage status selected.");
            }

            if (model.PlannedStartDate.HasValue &&
                model.PlannedEndDate.HasValue &&
                model.PlannedEndDate < model.PlannedStartDate)
            {
                ModelState.AddModelError(
                    nameof(model.PlannedEndDate),
                    "Planned end date cannot be before planned start date.");
            }

            if (model.ActualStartDate.HasValue &&
                model.ActualEndDate.HasValue &&
                model.ActualEndDate < model.ActualStartDate)
            {
                ModelState.AddModelError(
                    nameof(model.ActualEndDate),
                    "Actual end date cannot be before actual start date.");
            }
        }
    }
}