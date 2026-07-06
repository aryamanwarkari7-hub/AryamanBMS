using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class ProposalController : Controller
    {
        private readonly IProposalRepository    _proposalRepo;
        private readonly IClientRepository      _clientRepo;
        private readonly IFileStorageService    _fileStorage;
        private readonly ApplicationDbContext   _context;
        private readonly IProposalDocumentService _proposalDocumentService;

        public ProposalController(
    IProposalRepository proposalRepo,
    IClientRepository clientRepo,
    IFileStorageService fileStorage,
    ApplicationDbContext context,
    IProposalDocumentService proposalDocumentService)
        {
            _proposalRepo = proposalRepo;
            _clientRepo = clientRepo;
            _fileStorage = fileStorage;
            _context = context;
            _proposalDocumentService = proposalDocumentService;
        }

        #region Index

        public async Task<IActionResult> Index(string? status, int? clientId)
        {
            var proposals = string.IsNullOrEmpty(status)
                ? await _proposalRepo.GetAllAsync()
                : await _proposalRepo.GetByStatusAsync(status);

            if (clientId.HasValue)
                proposals = proposals.Where(p => p.ClientId == clientId.Value).ToList();

            ViewBag.FilterStatus   = status;
            ViewBag.FilterClientId = clientId;

            return View(proposals);
        }

        #endregion

        #region Create

        public async Task<IActionResult> Create(int? clientId)
        {
            var vm = new ProposalViewModel();

            if (clientId.HasValue)
            {
                vm.Proposal.ClientId = clientId.Value;
            }

            await LoadDropdownsAsync(vm);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProposalViewModel vm)
        {
            ModelState.Remove("Proposal.ProposalNumber");
            ModelState.Remove("Proposal.Status");
            ModelState.Remove(nameof(vm.UploadFile));

            vm.Proposal.Status = "Draft";
            vm.Proposal.IsConverted = false;
            vm.Proposal.IsActive = true;

            NormalizeProposal(vm.Proposal);

            if (vm.Proposal.ValidUntil.HasValue &&
                vm.Proposal.ValidUntil.Value.Date <
                vm.Proposal.ProposalDate.Date)
            {
                ModelState.AddModelError(
                    "Proposal.ValidUntil",
                    "Valid Until cannot be before Proposal Date.");
            }

            if (vm.Proposal.ProposalAmount < 0)
            {
                ModelState.AddModelError(
                    "Proposal.ProposalAmount",
                    "Proposal amount cannot be negative.");
            }

            if (!vm.Proposal.ProposalTemplateId.HasValue)
            {
                ModelState.AddModelError(
                    "Proposal.ProposalTemplateId",
                    "Please select a proposal template.");
            }
            else
            {
                bool templateAvailable =
                    await _context.ProposalTemplates
                        .AnyAsync(x =>
                            x.ProposalTemplateId ==
                                vm.Proposal.ProposalTemplateId.Value &&
                            x.IsActive);

                if (!templateAvailable)
                {
                    ModelState.AddModelError(
                        "Proposal.ProposalTemplateId",
                        "The selected proposal template is unavailable.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(vm);
                return View(vm);
            }

            await _proposalRepo.CreateWithSequenceAsync(vm.Proposal);

            string? currentUserId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                TempData["Warning"] =
                    $"Proposal {vm.Proposal.ProposalNumber} was created, " +
                    "but the current user could not be identified for document generation.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = vm.Proposal.ProposalId });
            }

            try
            {
                await _proposalDocumentService.GenerateAsync(
                    vm.Proposal,
                    currentUserId);

                TempData["Success"] =
                    $"Proposal {vm.Proposal.ProposalNumber} created " +
                    "and Word document generated successfully.";
            }
            catch (Exception ex)
            {
                TempData["Warning"] =
                    $"Proposal {vm.Proposal.ProposalNumber} was created, " +
                    $"but document generation failed: {ex.Message}";
            }

            return RedirectToAction(
                nameof(Details),
                new { id = vm.Proposal.ProposalId });
        }

        #endregion

        #region Edit

        public async Task<IActionResult> Edit(int id)
        {
            var proposal =
                await _proposalRepo.GetByIdAsync(id);

            if (proposal == null)
                return NotFound();

            if (proposal.IsConverted)
            {
                TempData["Error"] =
                    "Converted proposals cannot be edited.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (!proposal.IsActive)
            {
                TempData["Error"] =
                    "Inactive proposals cannot be edited.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var vm = new ProposalViewModel
            {
                Proposal = proposal
            };

            await LoadDropdownsAsync(vm);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,ProposalViewModel vm)
        {
            if (id != vm.Proposal.ProposalId)
                return NotFound();

            var existing =
                await _proposalRepo.GetByIdAsync(id);

            if (existing == null)
                return NotFound();

            if (existing.IsConverted)
            {
                TempData["Error"] =
                    "Converted proposals cannot be edited.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (!existing.IsActive)
            {
                TempData["Error"] =
                    "Inactive proposals cannot be edited.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            ModelState.Remove("Proposal.ProposalNumber");
            ModelState.Remove("Proposal.FileName");
            ModelState.Remove("Proposal.StoredFileName");
            ModelState.Remove("Proposal.FilePath");
            ModelState.Remove("Proposal.FileExtension");
            ModelState.Remove("Proposal.ContentType");
            ModelState.Remove("Proposal.FileSize");

            vm.Proposal.ProposalNumber =
                existing.ProposalNumber;

            vm.Proposal.FileName =
                existing.FileName;

            vm.Proposal.StoredFileName =
                existing.StoredFileName;

            vm.Proposal.FilePath =
                existing.FilePath;

            vm.Proposal.FileExtension =
                existing.FileExtension;

            vm.Proposal.ContentType =
                existing.ContentType;

            vm.Proposal.FileSize =
                existing.FileSize;

            vm.Proposal.VersionNo =
                existing.VersionNo;

            NormalizeProposal(vm.Proposal);

            if (!IsAllowedStatusTransition(
                    existing.Status,
                    vm.Proposal.Status))
            {
                ModelState.AddModelError(
                    "Proposal.Status",
                    $"Status cannot change from {existing.Status} " +
                    $"to {vm.Proposal.Status}.");
            }

            if (vm.Proposal.ValidUntil.HasValue &&
                vm.Proposal.ValidUntil.Value.Date <
                vm.Proposal.ProposalDate.Date)
            {
                ModelState.AddModelError(
                    "Proposal.ValidUntil",
                    "Valid Until cannot be before Proposal Date.");
            }

            if (vm.Proposal.ProposalAmount < 0)
            {
                ModelState.AddModelError(
                    "Proposal.ProposalAmount",
                    "Proposal amount cannot be negative.");
            }

            if (!vm.Proposal.ProposalTemplateId.HasValue)
            {
                ModelState.AddModelError(
                    "Proposal.ProposalTemplateId",
                    "Please select a proposal template.");
            }
            else
            {
                int selectedTemplateId =
                    vm.Proposal.ProposalTemplateId.Value;

                bool templateAvailable =
                    await _context.ProposalTemplates
                        .AnyAsync(x =>
                            x.ProposalTemplateId ==
                                selectedTemplateId &&
                            (
                                x.IsActive ||
                                x.ProposalTemplateId ==
                                    existing.ProposalTemplateId
                            ));

                if (!templateAvailable)
                {
                    ModelState.AddModelError(
                        "Proposal.ProposalTemplateId",
                        "The selected proposal template is unavailable.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(vm);
                return View(vm);
            }

            string? oldFilePath =
                existing.FilePath;

            FileUploadResult? uploadedFile = null;

            if (vm.UploadFile != null &&
                vm.UploadFile.Length > 0)
            {
                uploadedFile =
                    await _fileStorage.UploadAsync(
                        vm.UploadFile,
                        "Proposals");

                if (!uploadedFile.Success)
                {
                    ModelState.AddModelError(
                        nameof(vm.UploadFile),
                        uploadedFile.ErrorMessage ??
                        "Proposal document could not be uploaded.");

                    await LoadDropdownsAsync(vm);
                    return View(vm);
                }
            }

            existing.ClientId =   vm.Proposal.ClientId;

            existing.ProjectId = vm.Proposal.ProjectId;

            existing.ProposalTitle = vm.Proposal.ProposalTitle;

            existing.ProposalDate = vm.Proposal.ProposalDate;

            existing.ValidUntil = vm.Proposal.ValidUntil;

            existing.ProposalAmount =   vm.Proposal.ProposalAmount;

            existing.Currency = vm.Proposal.Currency;

            existing.Scope =  vm.Proposal.Scope;

            existing.Terms =  vm.Proposal.Terms;

            existing.Remarks = vm.Proposal.Remarks;

            existing.Status = vm.Proposal.Status;

            existing.RevisionNumber =  vm.Proposal.RevisionNumber;

            existing.PreparedBy = vm.Proposal.PreparedBy;

            existing.PreparedByDesignation =
                vm.Proposal.PreparedByDesignation;

            existing.ProblemStatement =
                vm.Proposal.ProblemStatement;

            existing.Timeline =  vm.Proposal.Timeline;

            existing.TechnicalSolution = vm.Proposal.TechnicalSolution;

            existing.OutOfScope = vm.Proposal.OutOfScope;

            existing.CustomerResponsibilities =
                vm.Proposal.CustomerResponsibilities;

            existing.Deliverables =  vm.Proposal.Deliverables;

            existing.Dependencies = vm.Proposal.Dependencies;

            existing.Assumptions =  vm.Proposal.Assumptions;

            existing.Risks = vm.Proposal.Risks;

            existing.Warranty =  vm.Proposal.Warranty;

            existing.CommercialDescription = vm.Proposal.CommercialDescription;

            existing.PaymentTerms =   vm.Proposal.PaymentTerms;

            existing.ProposalTemplateId =    vm.Proposal.ProposalTemplateId;

            if (uploadedFile != null)
            {
                ApplyFileFields(
                    existing,
                    uploadedFile);

                existing.VersionNo++;
            }

            try
            {
                await _proposalRepo.UpdateAsync(existing);
                await _proposalRepo.SaveAsync();
            }
            catch
            {
                if (uploadedFile != null)
                {
                    await _fileStorage.DeleteAsync(
                        uploadedFile.RelativePath);
                }

                throw;
            }

            if (uploadedFile != null &&
                !string.IsNullOrWhiteSpace(oldFilePath) &&
                !string.Equals(
                    oldFilePath,
                    uploadedFile.RelativePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                await _fileStorage.DeleteAsync(
                    oldFilePath);
            }

            TempData["Success"] =
                "Proposal updated successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        #endregion

        #region Details

        public async Task<IActionResult> Details(int id)
        {
            var proposal = await _proposalRepo.GetByIdAsync(id);
            if (proposal == null) return NotFound();

            return View(proposal);
        }

        #endregion

        #region Download

        public async Task<IActionResult> Download(int id)
        {
            var proposal =
                await _proposalRepo.GetByIdAsync(id);

            if (proposal == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(
                    proposal.FilePath))
            {
                TempData["Error"] =
                    "No generated proposal document is available.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var bytes =
                await _fileStorage.DownloadAsync(
                    proposal.FilePath);

            if (bytes == null)
            {
                TempData["Error"] =
                    "File not found on disk.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            return File(
                bytes,
                proposal.ContentType ??
                "application/octet-stream",
                proposal.FileName ??
                $"Proposal-{proposal.ProposalNumber}.docx");
        }

        #endregion

        #region Delete

        public async Task<IActionResult> Delete(int id)
        {
            var proposal =
                await _proposalRepo.GetByIdAsync(id);

            if (proposal == null)
                return NotFound();

            if (proposal.IsConverted)
            {
                TempData["Error"] =
                    "Converted proposals cannot be activated or deactivated.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            return View(proposal);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var proposal =
                await _proposalRepo.GetByIdAsync(id);

            if (proposal == null)
                return NotFound();

            if (proposal.IsConverted)
            {
                TempData["Error"] =
                    "Converted proposals cannot be activated or deactivated.";

                return RedirectToAction(nameof(Index));
            }

            proposal.IsActive = !proposal.IsActive;
            proposal.UpdatedOn = DateTime.Now;

            await _proposalRepo.UpdateAsync(proposal);
            await _proposalRepo.SaveAsync();

            TempData["Success"] = proposal.IsActive
                ? "Proposal activated successfully."
                : "Proposal deactivated successfully.";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Convert to PO/WO (shortcut redirect)

        /// <summary>
        /// Redirects to PurchaseOrder/Create pre-filled with this proposal's data.
        /// </summary>
        public async Task<IActionResult> ConvertToOrder(int id)
        {
            var proposal =
                await _proposalRepo.GetByIdAsync(id);

            if (proposal == null)
                return NotFound();

            if (!proposal.IsActive)
            {
                TempData["Error"] =
                    "Inactive proposals cannot be converted.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (proposal.IsConverted)
            {
                TempData["Error"] =
                    "This proposal has already been converted.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (!string.Equals(
                    proposal.Status,
                    "Accepted",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Only accepted proposals can be converted.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (IsExpired(proposal))
            {
                TempData["Error"] =
                    "Expired proposals cannot be converted.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            return RedirectToAction(
                "Create",
                "PurchaseOrder",
                new { proposalId = proposal.ProposalId });
        }

        #endregion

        #region Document Generation

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateDocument(
            int id)
        {
            var proposal =
                await _proposalRepo.GetByIdAsync(id);

            if (proposal == null)
                return NotFound();

            if (!proposal.IsActive)
            {
                TempData["Error"] =
                    "Inactive proposals cannot generate new documents.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (!proposal.ProposalTemplateId.HasValue)
            {
                TempData["Error"] =
                    "No proposal template is assigned.";

                return RedirectToAction(
                    nameof(Edit),
                    new { id });
            }

            string? currentUserId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                TempData["Error"] =
                    "Current user could not be identified.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            try
            {
                var generatedVersion =
                    await _proposalDocumentService
                        .GenerateAsync(
                            proposal,
                            currentUserId);

                TempData["Success"] =
                    $"Proposal document version " +
                    $"{generatedVersion.VersionNumber} generated successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    $"Document generation failed: {ex.Message}";
            }

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        [HttpGet]
        public async Task<IActionResult> DocumentHistory(int id)
        {
            var proposal =
                await _proposalRepo.GetByIdAsync(id);

            if (proposal == null)
                return NotFound();

            var versions =
                await _context.ProposalDocumentVersions
                    .AsNoTracking()
                    .Include(x => x.ProposalTemplate)
                    .Where(x =>
                        x.ProposalId == id)
                    .OrderByDescending(x =>
                        x.VersionNumber)
                    .ToListAsync();

            ViewBag.ProposalId =
                proposal.ProposalId;

            ViewBag.ProposalNumber =
                proposal.ProposalNumber;

            ViewBag.ProposalTitle =
                proposal.ProposalTitle;

            return View(versions);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocumentVersion(int id)
        {
            var version =
                await _context.ProposalDocumentVersions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.ProposalDocumentVersionId == id);

            if (version == null)
                return NotFound();

            var bytes =
                await _fileStorage.DownloadAsync(
                    version.StoredFilePath);

            if (bytes == null)
            {
                TempData["Error"] =
                    "The selected document version was not found on disk.";

                return RedirectToAction(
                    nameof(DocumentHistory),
                    new { id = version.ProposalId });
            }

            return File(
                bytes,
                version.ContentType,
                version.OriginalFileName);
        }

        #endregion


        #region Helpers

        private static bool IsExpired(ProposalModel proposal)
        {
            return proposal.ValidUntil.HasValue &&
                   proposal.ValidUntil.Value.Date < DateTime.Today;
        }

        private static bool IsAllowedStatusTransition(
            string currentStatus,
            string newStatus)
        {
            if (string.Equals(
                    currentStatus,
                    newStatus,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return currentStatus switch
            {
                "Draft" => newStatus is "Sent" or "Rejected",

                "Sent" => newStatus is
                    "UnderReview" or
                    "Accepted" or
                    "Rejected" or
                    "Expired",

                "UnderReview" => newStatus is
                    "Accepted" or
                    "Rejected" or
                    "Expired",

                "Accepted" => newStatus is "Expired",

                "Rejected" => false,

                "Expired" => false,

                _ => false
            };
        }

        private async Task LoadDropdownsAsync(ProposalViewModel vm)
        {
            var clients = await _clientRepo.GetAllAsync();
            vm.Clients = clients
                .Where(c => c.IsActive)
                .Select(c => new SelectListItem(c.ClientName, c.ClientId.ToString()));

            var projects = await _context.Projects.OrderBy(p => p.ProjectName).ToListAsync();
            vm.Projects = projects.Select(p =>
                new SelectListItem(p.ProjectName, p.Id.ToString()));

            int? currentTemplateId = vm.Proposal.ProposalTemplateId;

            var templates =
                await _context.ProposalTemplates
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive ||
                        (
                            currentTemplateId.HasValue &&
                            x.ProposalTemplateId ==
                                currentTemplateId.Value
                        ))
                    .OrderBy(x => x.TemplateName)
                    .ThenByDescending(x => x.VersionNumber)
                    .ToListAsync();

            vm.ProposalTemplates =
                templates.Select(x =>
                    new SelectListItem
                    {
                        Value =
                            x.ProposalTemplateId.ToString(),

                        Text =
                            x.IsActive
                                ? $"{x.TemplateName} — Rev {x.VersionNumber}"
                                : $"{x.TemplateName} — Rev {x.VersionNumber} (Archived)",

                        Selected =
                            currentTemplateId.HasValue &&
                            x.ProposalTemplateId ==
                                currentTemplateId.Value
                    });
        }

        private static void ApplyFileFields(ProposalModel target, FileUploadResult upload)
        {
            target.FileName       = upload.OriginalFileName;
            target.StoredFileName = upload.StoredFileName;
            target.FileExtension  = upload.FileExtension;
            target.ContentType    = upload.ContentType;
            target.FileSize       = upload.FileSize;
            target.FilePath       = upload.RelativePath;
        }

        private static void NormalizeProposal(ProposalModel proposal)
        {
            proposal.RevisionNumber =
                string.IsNullOrWhiteSpace(
                    proposal.RevisionNumber)
                    ? "00"
                    : proposal.RevisionNumber.Trim();

            proposal.PreparedBy =
                proposal.PreparedBy?.Trim() ??
                string.Empty;

            proposal.PreparedByDesignation =
                CleanText(
                    proposal.PreparedByDesignation);

            proposal.ProposalTitle =
                proposal.ProposalTitle?.Trim() ??
                string.Empty;

            proposal.Currency =
                string.IsNullOrWhiteSpace(
                    proposal.Currency)
                    ? "INR"
                    : proposal.Currency.Trim()
                        .ToUpperInvariant();

            proposal.ProblemStatement =
                CleanText(proposal.ProblemStatement);

            proposal.Timeline =
                CleanText(proposal.Timeline);

            proposal.TechnicalSolution =
                CleanText(proposal.TechnicalSolution);

            proposal.Scope =
                CleanText(proposal.Scope);

            proposal.Terms =
                CleanText(proposal.Terms);

            proposal.OutOfScope =
                CleanText(proposal.OutOfScope);

            proposal.CustomerResponsibilities =
                CleanText(
                    proposal.CustomerResponsibilities);

            proposal.Deliverables =
                CleanText(proposal.Deliverables);

            proposal.Dependencies =
                CleanText(proposal.Dependencies);

            proposal.Assumptions =
                CleanText(proposal.Assumptions);

            proposal.Risks =
                CleanText(proposal.Risks);

            proposal.Warranty =
                CleanText(proposal.Warranty);

            proposal.CommercialDescription =
                CleanText(
                    proposal.CommercialDescription);

            proposal.PaymentTerms =
                CleanText(proposal.PaymentTerms);

            proposal.Remarks =
                CleanText(proposal.Remarks);
        }

        private static string? CleanText(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
        #endregion
    }
}
