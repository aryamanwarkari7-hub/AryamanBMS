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

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class ProposalController : Controller
    {
        private readonly IProposalRepository    _proposalRepo;
        private readonly IClientRepository      _clientRepo;
        private readonly IFileStorageService    _fileStorage;
        private readonly ApplicationDbContext   _context;

        public ProposalController(
            IProposalRepository  proposalRepo,
            IClientRepository    clientRepo,
            IFileStorageService  fileStorage,
            ApplicationDbContext context)
        {
            _proposalRepo = proposalRepo;
            _clientRepo   = clientRepo;
            _fileStorage  = fileStorage;
            _context      = context;
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

            await LoadDropdownsAsync(vm);

            if (!ModelState.IsValid)
                return View(vm);

            if (vm.UploadFile == null)
            {
                ModelState.AddModelError(
                    nameof(vm.UploadFile),
                    "Please upload the proposal document.");

                return View(vm);
            }

            var upload =
                await _fileStorage.UploadAsync(
                    vm.UploadFile,
                    "Proposals");

            if (!upload.Success)
            {
                ModelState.AddModelError(
                    nameof(vm.UploadFile),
                    upload.ErrorMessage);

                return View(vm);
            }

            ApplyFileFields(vm.Proposal, upload);

            vm.Proposal.IsConverted = false;

            try
            {
                await _proposalRepo.CreateWithSequenceAsync(
                    vm.Proposal);
            }
            catch
            {
                await _fileStorage.DeleteAsync(
                    upload.RelativePath);

                throw;
            }

            TempData["Success"] =
                $"Proposal {vm.Proposal.ProposalNumber} created successfully.";

            return RedirectToAction(nameof(Index));
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
        public async Task<IActionResult> Edit( int id, ProposalViewModel vm)
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

            await LoadDropdownsAsync(vm);

            if (!ModelState.IsValid)
                return View(vm);

            string? oldFilePath = existing.FilePath;
            FileUploadResult? uploadedFile = null;

            existing.ClientId = vm.Proposal.ClientId;
            existing.ProjectId = vm.Proposal.ProjectId;
            existing.ProposalTitle = vm.Proposal.ProposalTitle;
            existing.ProposalDate = vm.Proposal.ProposalDate;
            existing.ValidUntil = vm.Proposal.ValidUntil;
            existing.ProposalAmount = vm.Proposal.ProposalAmount;
            existing.Currency = vm.Proposal.Currency;
            existing.Scope = vm.Proposal.Scope;
            existing.Terms = vm.Proposal.Terms;
            existing.Remarks = vm.Proposal.Remarks;
            existing.Status = vm.Proposal.Status;

            if (vm.UploadFile != null)
            {
                uploadedFile =
                    await _fileStorage.UploadAsync(
                        vm.UploadFile,
                        "Proposals");

                if (!uploadedFile.Success)
                {
                    ModelState.AddModelError(
                        nameof(vm.UploadFile),
                        uploadedFile.ErrorMessage);

                    return View(vm);
                }

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
                !string.IsNullOrWhiteSpace(oldFilePath))
            {
                await _fileStorage.DeleteAsync(oldFilePath);
            }

            TempData["Success"] =
                "Proposal updated successfully.";

            return RedirectToAction(nameof(Index));
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
            var proposal = await _proposalRepo.GetByIdAsync(id);
            if (proposal == null) return NotFound();

            var bytes = await _fileStorage.DownloadAsync(proposal.FilePath);
            if (bytes == null)
            {
                TempData["Error"] = "File not found on disk.";
                return RedirectToAction(nameof(Index));
            }

            return File(bytes,
                proposal.ContentType ?? "application/octet-stream",
                proposal.FileName);
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

        #endregion
    }
}
