using System.Security.Claims;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize]
    public class ProjectCommunicationController : Controller
    {
        #region Actions

        private readonly IProjectCommunicationRepository _communicationRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IProjectAccessService _projectAccessService;
        private readonly IProjectTimelineRepository _timelineRepository;

        public ProjectCommunicationController(
           IProjectCommunicationRepository communicationRepository,
           IProjectRepository projectRepository,
           IEmployeeRepository employeeRepository,
           IProjectAccessService projectAccessService,
           IProjectTimelineRepository timelineRepository)
        {
            _communicationRepository = communicationRepository;
            _projectRepository = projectRepository;
            _employeeRepository = employeeRepository;
            _projectAccessService = projectAccessService;
            _timelineRepository = timelineRepository;
        }

        // ==========================================
        // INDEX
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index(int projectId)
        {
            var project =
                await _projectRepository.GetDetailsAsync(projectId);

            if (project == null)
                return NotFound();

            if (!await _projectAccessService
                .CanAccessProjectAsync(User, projectId))
            {
                return Forbid();
            }

            var communications =
                await _communicationRepository
                    .GetByProjectIdAsync(projectId);

            ViewBag.Project = project;
            
            return View(communications);
        }

        // ==========================================
        // ADD MESSAGE
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Create(int projectId)
        {
            var project =
                await _projectRepository.GetDetailsAsync(projectId);

            if (project == null)
                return NotFound();

            if (!await _projectAccessService
                .CanAccessProjectAsync(User, projectId))
            {
                return Forbid();
            }

            ViewBag.Project = project;

            return View(
                new ProjectCommunicationModel
                {
                    ProjectId = projectId
                });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
        int projectId,
        string subject,
        string message)
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                TempData["Error"] = "Subject is required.";

                return RedirectToAction(nameof(Index),
                    new { projectId });
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Message is required.";

                return RedirectToAction(nameof(Index),
                    new { projectId });
            }

            if (!await _projectAccessService
                .CanAccessProjectAsync(User, projectId))
            {
                return Forbid();
            }

            var userId =     User.FindFirstValue(ClaimTypes.NameIdentifier);

            int? employeeId = null;

            if (!_projectAccessService.IsAdminOrHR(User))
            {
                var employee =
                    await _employeeRepository.Employees
                        .AsNoTracking()
                        .FirstOrDefaultAsync(e =>
                            e.ApplicationUserId == userId);

                if (employee == null)
                    return Forbid();

                employeeId = employee.Id;
            }

            var communication =
                new ProjectCommunicationModel
                {
                    ProjectId = projectId,

                    CreatedByEmployeeId = employeeId,

                    CreatedByUserId = userId,

                    CommunicationType = "Internal",

                    Subject = subject.Trim(),

                    Message = message.Trim(),

                    Status = "Open",

                    IsSystemGenerated = false,

                    CreatedOn = DateTime.Now,

                    IsActive = true
                };

            await _communicationRepository.AddAsync(communication);

            TempData["Success"] =
                "Project communication posted successfully.";

            return RedirectToAction(nameof(Index),
                new { projectId });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var communication =
                await _communicationRepository.GetByIdAsync(id);

            if (communication == null)
                return NotFound();

            if (!await _projectAccessService
                .CanAccessProjectAsync(User, communication.ProjectId))
            {
                return Forbid();
            }

            return View(communication);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var communication =
                await _communicationRepository.GetByIdAsync(id);

            if (communication == null)
                return NotFound();

            if (!await _projectAccessService
                .CanAccessProjectAsync(User, communication.ProjectId))
            {
                return Forbid();
            }

            if (communication.IsSystemGenerated)
            {
                return RedirectToAction(
                    "Edit",
                    "ClientCommunication",
                    new { id = communication.ClientCommunicationId });
            }

            return View(communication);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProjectCommunicationModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var communication =
                await _communicationRepository.GetByIdAsync(model.Id);

            if (communication == null)
                return NotFound();

            if (!await _projectAccessService
                .CanAccessProjectAsync(User, communication.ProjectId))
            {
                return Forbid();
            }

            if (communication.IsSystemGenerated)
            {
                return RedirectToAction(
                    "Edit",
                    "ClientCommunication",
                    new { id = communication.ClientCommunicationId });
            }

            communication.Subject = model.Subject.Trim();

            communication.Message = model.Message.Trim();

            communication.Status = model.Status;

            communication.IsEdited = true;

            communication.UpdatedOn = DateTime.Now;

            await _communicationRepository.UpdateAsync(communication);

            TempData["Success"] =
                "Project communication updated successfully.";

            return RedirectToAction(
                nameof(Index),
                new { projectId = communication.ProjectId });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var communication =
                await _communicationRepository.GetByIdAsync(id);

            if (communication == null)
                return NotFound();

            if (!await _projectAccessService
                .CanAccessProjectAsync(User, communication.ProjectId))
            {
                return Forbid();
            }

            if (communication.IsSystemGenerated)
            {
                return RedirectToAction(
                    "Details",
                    "ClientCommunication",
                    new { id = communication.ClientCommunicationId });
            }

            return View(communication);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var communication =
                await _communicationRepository.GetByIdAsync(id);

            if (communication == null)
                return NotFound();

            if (!await _projectAccessService
                .CanAccessProjectAsync(User, communication.ProjectId))
            {
                return Forbid();
            }

            if (communication.IsSystemGenerated)
            {
                return RedirectToAction(
                    "Details",
                    "ClientCommunication",
                    new { id = communication.ClientCommunicationId });
            }

            await _communicationRepository.DeleteAsync(communication);

            TempData["Success"] =
                "Project communication deleted successfully.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    projectId = communication.ProjectId
                });
        }
        #endregion
    }
}
