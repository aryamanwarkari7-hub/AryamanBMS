using System.Security.Claims;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using AryamanBMS.ViewModels;
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
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProjectCommunicationController(
           IProjectCommunicationRepository communicationRepository,
           IProjectRepository projectRepository,
           IEmployeeRepository employeeRepository,
           IProjectAccessService projectAccessService,
           IProjectTimelineRepository timelineRepository,
           IWebHostEnvironment webHostEnvironment)
        {
            _communicationRepository = communicationRepository;
            _projectRepository = projectRepository;
            _employeeRepository = employeeRepository;
            _projectAccessService = projectAccessService;
            _timelineRepository = timelineRepository;
            _webHostEnvironment = webHostEnvironment;
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
        string message,
        List<ProjectCommunicationDocumentInputViewModel>? uploadedDocuments)
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

            var documentUploads = GetDocumentUploads(uploadedDocuments);
            var documentError = ValidateDocuments(documentUploads);
            if (documentError != null)
            {
                TempData["Error"] = documentError;
                return RedirectToAction(nameof(Create), new { projectId });
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

            foreach (var document in documentUploads)
            {
                await SaveDocumentAsync(communication, document.File!, document.DocumentName!);
            }

            TempData["Success"] = documentUploads.Count == 0
                ? "Project communication posted successfully."
                : $"Project communication posted successfully with {documentUploads.Count} document(s).";

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
        public async Task<IActionResult> Edit(
            ProjectCommunicationModel model,
            List<ProjectCommunicationDocumentInputViewModel>? uploadedDocuments)
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

            var documentUploads = GetDocumentUploads(uploadedDocuments);
            var documentError = ValidateDocuments(documentUploads);
            if (documentError != null)
            {
                ModelState.AddModelError(string.Empty, documentError);
                return View(model);
            }

            communication.Subject = model.Subject.Trim();

            communication.Message = model.Message.Trim();

            communication.Status = model.Status;

            communication.IsEdited = true;

            communication.UpdatedOn = DateTime.Now;

            await _communicationRepository.UpdateAsync(communication);

            foreach (var document in documentUploads)
            {
                await SaveDocumentAsync(communication, document.File!, document.DocumentName!);
            }

            TempData["Success"] = documentUploads.Count == 0
                ? "Project communication updated successfully."
                : $"Project communication updated successfully with {documentUploads.Count} document(s).";

            return RedirectToAction(
                nameof(Index),
                new { projectId = communication.ProjectId });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var document = await _communicationRepository.GetDocumentByIdAsync(id);
            if (document?.ProjectCommunication == null)
            {
                return NotFound();
            }

            var projectId = document.ProjectCommunication.ProjectId;
            if (!await _projectAccessService.CanAccessProjectAsync(User, projectId))
            {
                return Forbid();
            }

            var physicalPath = Path.Combine(
                _webHostEnvironment.ContentRootPath,
                "App_Data",
                "ProjectCommunicationDocuments",
                projectId.ToString(),
                document.ProjectCommunicationId.ToString(),
                Path.GetFileName(document.StoredFileName));

            if (!System.IO.File.Exists(physicalPath))
            {
                TempData["Error"] = "The communication document file could not be found.";
                return RedirectToAction(nameof(Index), new { projectId });
            }

            return PhysicalFile(physicalPath, document.ContentType, document.OriginalFileName);
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

        private List<ProjectCommunicationDocumentInputViewModel> GetDocumentUploads(
            IEnumerable<ProjectCommunicationDocumentInputViewModel>? documents)
        {
            var uploads = (documents ?? [])
                .Where(document => document.File?.Length > 0)
                .ToList();

            if (uploads.Any())
            {
                return uploads;
            }

            foreach (var file in Request.Form.Files.Where(file => file.Length > 0))
            {
                const string fileProperty = ".File";
                if (!file.Name.EndsWith(fileProperty, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var nameField = $"{file.Name[..^fileProperty.Length]}.DocumentName";
                uploads.Add(new ProjectCommunicationDocumentInputViewModel
                {
                    File = file,
                    DocumentName = Request.Form[nameField].ToString()
                });
            }

            return uploads;
        }

        private static string? ValidateDocuments(
            IEnumerable<ProjectCommunicationDocumentInputViewModel> documents)
        {
            foreach (var document in documents)
            {
                if (string.IsNullOrWhiteSpace(document.DocumentName))
                {
                    return "Enter a name for each uploaded document.";
                }

                if (document.DocumentName.Trim().Length > 200)
                {
                    return "Document name must not exceed 200 characters.";
                }

                var file = document.File!;
                const long maxFileSize = 20 * 1024 * 1024;
                if (file.Length > maxFileSize)
                {
                    return "Document must not exceed 20 MB.";
                }

                if (GetAllowedDocumentContentType(file.FileName) == null)
                {
                    return "Only PDF, DOC, DOCX, and XLSX documents are allowed.";
                }
            }

            return null;
        }

        private async Task SaveDocumentAsync(
            ProjectCommunicationModel communication,
            IFormFile document,
            string documentName)
        {
            var contentType = GetAllowedDocumentContentType(document.FileName)
                ?? throw new InvalidOperationException("Unsupported document type.");
            var extension = Path.GetExtension(document.FileName).ToLowerInvariant();
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var folderPath = Path.Combine(
                _webHostEnvironment.ContentRootPath,
                "App_Data",
                "ProjectCommunicationDocuments",
                communication.ProjectId.ToString(),
                communication.Id.ToString());

            Directory.CreateDirectory(folderPath);
            var physicalPath = Path.Combine(folderPath, storedFileName);

            try
            {
                await using var stream = new FileStream(
                    physicalPath,
                    FileMode.CreateNew,
                    FileAccess.Write);
                await document.CopyToAsync(stream);

                await _communicationRepository.AddDocumentAsync(
                    new ProjectCommunicationDocumentModel
                    {
                        ProjectCommunicationId = communication.Id,
                        OriginalFileName = Path.GetFileName(document.FileName),
                        DocumentName = documentName.Trim(),
                        StoredFileName = storedFileName,
                        ContentType = contentType,
                        FileSize = document.Length,
                        UploadedOn = DateTime.Now,
                        UploadedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                        IsActive = true
                    });
            }
            catch
            {
                if (System.IO.File.Exists(physicalPath))
                {
                    System.IO.File.Delete(physicalPath);
                }

                throw;
            }
        }

        private static string? GetAllowedDocumentContentType(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => null
            };
        }
        #endregion
    }
}
