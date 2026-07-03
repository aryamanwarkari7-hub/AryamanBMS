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
            var proposal = await _proposalRepo.GetByIdAsync(id);
            if (proposal == null) return NotFound();

            var vm = new ProposalViewModel { Proposal = proposal };
            await LoadDropdownsAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProposalViewModel vm)
        {
            if (id != vm.Proposal.ProposalId) return NotFound();

            await LoadDropdownsAsync(vm);

            if (!ModelState.IsValid)
                return View(vm);

            var existing = await _proposalRepo.GetByIdAsync(id);
            if (existing == null) return NotFound();

            // Update metadata fields
            existing.ClientId       = vm.Proposal.ClientId;
            existing.ProjectId      = vm.Proposal.ProjectId;
            existing.ProposalTitle  = vm.Proposal.ProposalTitle;
            existing.ProposalDate   = vm.Proposal.ProposalDate;
            existing.ValidUntil     = vm.Proposal.ValidUntil;
            existing.ProposalAmount = vm.Proposal.ProposalAmount;
            existing.Currency       = vm.Proposal.Currency;
            existing.Scope          = vm.Proposal.Scope;
            existing.Terms          = vm.Proposal.Terms;
            existing.Remarks        = vm.Proposal.Remarks;
            existing.Status         = vm.Proposal.Status;

            

            // Replace file only if a new one is uploaded
            if (vm.UploadFile != null)
            {
                var upload = await _fileStorage.UploadAsync(vm.UploadFile, "Proposals");
                if (!upload.Success)
                {
                    ModelState.AddModelError("", upload.ErrorMessage);
                    return View(vm);
                }

                // Delete old file after successful upload
                await _fileStorage.DeleteAsync(existing.FilePath);
                ApplyFileFields(existing, upload);
            }

            await _proposalRepo.UpdateAsync(existing);
            await _proposalRepo.SaveAsync();

            TempData["Success"] = "Proposal updated successfully.";
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
            var proposal = await _proposalRepo.GetByIdAsync(id);
            if (proposal == null) return NotFound();

            return View(proposal);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var proposal = await _proposalRepo.GetByIdAsync(id);
            if (proposal == null) return NotFound();

            if (proposal.IsConverted && proposal.IsActive)
            {
                TempData["Error"] = "Converted proposals cannot be deactivated.";
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
            var proposal = await _proposalRepo.GetByIdAsync(id);
            if (proposal == null) return NotFound();

            return RedirectToAction(
                "Create",
                "PurchaseOrder",
                new { proposalId = proposal.ProposalId });
        }

        #endregion

        #region Helpers

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
