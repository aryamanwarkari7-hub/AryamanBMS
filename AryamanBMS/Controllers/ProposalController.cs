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

        public async Task<IActionResult> Index(
    string? status,
    int? clientId,
    string? search,
    string sortBy = "ProposalDate",
    string sortOrder = "desc")
        {
            var proposals = string.IsNullOrEmpty(status)
                ? await _proposalRepo.GetAllAsync()
                : await _proposalRepo.GetByStatusAsync(status);

            if (clientId.HasValue && clientId.Value > 0)
            {
                proposals = proposals
                    .Where(p => p.ClientId == clientId.Value)
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim().ToLower();

                proposals = proposals
                    .Where(p =>
                        (!string.IsNullOrWhiteSpace(p.ProposalNumber) &&
                            p.ProposalNumber.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(p.ProposalTitle) &&
                            p.ProposalTitle.ToLower().Contains(keyword)) ||
                        (p.Client != null &&
                            !string.IsNullOrWhiteSpace(p.Client.ClientName) &&
                            p.Client.ClientName.ToLower().Contains(keyword)) ||
                        (p.Project != null &&
                            !string.IsNullOrWhiteSpace(p.Project.ProjectName) &&
                            p.Project.ProjectName.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(p.Status) &&
                            p.Status.ToLower().Contains(keyword)))
                    .ToList();
            }

            bool descending = sortOrder == "desc";

            proposals = sortBy switch
            {
                "ProposalNo" => descending
                    ? proposals.OrderByDescending(p => p.ProposalNumber).ToList()
                    : proposals.OrderBy(p => p.ProposalNumber).ToList(),

                "Client" => descending
                    ? proposals.OrderByDescending(p => p.Client?.ClientName).ToList()
                    : proposals.OrderBy(p => p.Client?.ClientName).ToList(),

                "Title" => descending
                    ? proposals.OrderByDescending(p => p.ProposalTitle).ToList()
                    : proposals.OrderBy(p => p.ProposalTitle).ToList(),

                "ValidUntil" => descending
                    ? proposals.OrderByDescending(p => p.ValidUntil).ToList()
                    : proposals.OrderBy(p => p.ValidUntil).ToList(),

                "Amount" => descending
                    ? proposals.OrderByDescending(p => p.ProposalAmount).ToList()
                    : proposals.OrderBy(p => p.ProposalAmount).ToList(),

                "Status" => descending
                    ? proposals.OrderByDescending(p => p.Status).ToList()
                    : proposals.OrderBy(p => p.Status).ToList(),

                "Converted" => descending
                    ? proposals.OrderByDescending(p => p.IsConverted).ToList()
                    : proposals.OrderBy(p => p.IsConverted).ToList(),

                _ => descending
                    ? proposals.OrderByDescending(p => p.ProposalDate).ToList()
                    : proposals.OrderBy(p => p.ProposalDate).ToList(),
            };

            ViewBag.FilterStatus = status;
            ViewBag.FilterClientId = clientId;
            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            ViewBag.Clients = await _clientRepo.GetAllAsync();

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

            string? currentUserId =  User.FindFirstValue(ClaimTypes.NameIdentifier);

            vm.Proposal.Status = "Draft";
            vm.Proposal.IsConverted = false;
            vm.Proposal.IsActive = true;
            vm.Proposal.CreatedByUserId = currentUserId;

            NormalizeProposal(vm.Proposal);

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
            await AddProposalAuditAsync(
               vm.Proposal, "Created",
               null,
               vm.Proposal.Status,
               null,
               vm.Proposal.ProposalAmount,
               null,
               vm.Proposal.RevisionNumber,
               "Proposal created.",
               currentUserId);

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

            string? currentUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

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

            bool protectedProposalChanged = HasProtectedProposalChange(existing, vm.Proposal);

            if (IsAccepted(existing) && protectedProposalChanged)
            {
                if (string.Equals(
                        existing.RevisionNumber,
                        vm.Proposal.RevisionNumber,
                        StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(
                        "Proposal.RevisionNumber",
                        "Revision number must change when an accepted proposal is revised.");
                }

                if (string.IsNullOrWhiteSpace(vm.Proposal.RevisionReason))
                {
                    ModelState.AddModelError(
                        "Proposal.RevisionReason",
                        "Revision reason is required when an accepted proposal is revised.");
                }

                if (string.IsNullOrWhiteSpace(vm.Proposal.CustomerApprovalReference))
                {
                    ModelState.AddModelError(
                        "Proposal.CustomerApprovalReference",
                        "Customer approval reference is required when an accepted proposal is revised.");
                }
            }

            if (vm.Proposal.Status == "Rejected" &&
                string.IsNullOrWhiteSpace(vm.Proposal.RejectionReason))
            {
                ModelState.AddModelError(
                    "Proposal.RejectionReason",
                    "Rejection reason is required.");
            }

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

            string oldStatus = existing.Status;
            decimal? oldAmount = existing.ProposalAmount;
            string oldRevisionNumber = existing.RevisionNumber;
            bool statusChanged =
                !string.Equals(
                    existing.Status,
                    vm.Proposal.Status,
                    StringComparison.OrdinalIgnoreCase);

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

            ApplyStatusAuditFields(
                existing,
                oldStatus,
                vm.Proposal.Status,
                currentUserId);

            existing.RevisionNumber =  vm.Proposal.RevisionNumber;

            existing.PreparedBy = vm.Proposal.PreparedBy;

            existing.PreparedByDesignation =vm.Proposal.PreparedByDesignation;

            existing.ProblemStatement = vm.Proposal.ProblemStatement;

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

            existing.RejectionReason = CleanText(vm.Proposal.RejectionReason);

            existing.RevisionReason =   CleanText(vm.Proposal.RevisionReason);

            existing.CustomerApprovalReference = CleanText(vm.Proposal.CustomerApprovalReference);

            if (protectedProposalChanged)
            {
                existing.RevisedByUserId = currentUserId;
                existing.RevisedOn = DateTime.Now;
            }

            existing.UpdatedByUserId = currentUserId;

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
                if (statusChanged || protectedProposalChanged)
                {
                    await AddProposalAuditAsync(
                        existing,
                        protectedProposalChanged ? "Revised" : "Status Changed",
                        oldStatus,
                        existing.Status,
                        oldAmount,
                        existing.ProposalAmount,
                        oldRevisionNumber,
                        existing.RevisionNumber,
                        protectedProposalChanged
                            ? existing.RevisionReason
                            : $"Status changed from {oldStatus} to {existing.Status}.",
                        currentUserId);
                }
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
            var proposal = await _proposalRepo.GetByIdAsync(id);

            if (proposal == null)
                return NotFound();

            if (proposal.IsConverted)
            {
                TempData["Error"] =
                    "Converted proposals cannot be activated or deactivated.";

                return RedirectToAction(nameof(Index));
            }

            bool newActiveStatus = !proposal.IsActive;

            proposal.IsActive = newActiveStatus;
            proposal.UpdatedOn = DateTime.Now;
            proposal.UpdatedByUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!newActiveStatus)
            {
                proposal.CancelledByUserId = proposal.UpdatedByUserId;
                proposal.CancelledOn = DateTime.Now;
            }

            await AddProposalAuditAsync(
                proposal,
                newActiveStatus ? "Activated" : "Deactivated",
                proposal.Status,
                proposal.Status,
                proposal.ProposalAmount,
                proposal.ProposalAmount,
                proposal.RevisionNumber,
                proposal.RevisionNumber,
                newActiveStatus
                    ? "Proposal activated."
                    : "Proposal deactivated.",
                proposal.UpdatedByUserId);

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

        private static bool IsAccepted(ProposalModel proposal)
        {
            return string.Equals(
                proposal.Status,
                "Accepted",
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasProtectedProposalChange(
            ProposalModel existing,
            ProposalModel incoming)
        {
            return existing.ProposalAmount != incoming.ProposalAmount ||
                   !string.Equals(existing.Scope, incoming.Scope, StringComparison.Ordinal) ||
                   !string.Equals(existing.PaymentTerms, incoming.PaymentTerms, StringComparison.Ordinal) ||
                   !string.Equals(existing.CommercialDescription, incoming.CommercialDescription, StringComparison.Ordinal);
        }

        private static void ApplyStatusAuditFields(
            ProposalModel proposal,
            string oldStatus,
            string newStatus,
            string? currentUserId)
        {
            if (string.Equals(oldStatus, newStatus, StringComparison.OrdinalIgnoreCase))
                return;

            DateTime now = DateTime.Now;

            if (newStatus == "Sent")
            {
                proposal.SubmittedByUserId = currentUserId;
                proposal.SubmittedOn = now;
                proposal.IssuedByUserId = currentUserId;
                proposal.IssuedOn = now;
            }
            else if (newStatus == "Accepted")
            {
                proposal.AcceptedByUserId = currentUserId;
                proposal.AcceptedOn = now;
                proposal.ApprovedByUserId = currentUserId;
                proposal.ApprovedOn = now;
            }
            else if (newStatus == "Rejected")
            {
                proposal.RejectedByUserId = currentUserId;
                proposal.RejectedOn = now;
            }
            else if (newStatus == "Expired")
            {
                proposal.ExpiredOn = now;
            }
        }

        private async Task AddProposalAuditAsync(
            ProposalModel proposal,
            string actionType,
            string? oldStatus,
            string? newStatus,
            decimal? oldAmount,
            decimal? newAmount,
            string? oldRevisionNumber,
            string? newRevisionNumber,
            string? remarks,
            string? changedByUserId)
        {
            await _context.ProposalAudits.AddAsync(
                new ProposalAuditModel
                {
                    ProposalId = proposal.ProposalId,
                    ActionType = actionType,
                    OldStatus = oldStatus,
                    NewStatus = newStatus,
                    OldAmount = oldAmount,
                    NewAmount = newAmount,
                    OldRevisionNumber = oldRevisionNumber,
                    NewRevisionNumber = newRevisionNumber,
                    Remarks = CleanText(remarks),
                    ChangedByUserId = changedByUserId,
                    ChangedOn = DateTime.Now
                });

            await _context.SaveChangesAsync();
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
