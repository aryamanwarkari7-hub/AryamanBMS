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
    public class PurchaseOrderController : Controller
    {
        private readonly IPurchaseOrderRepository _orderRepo;
        private readonly IProposalRepository      _proposalRepo;
        private readonly IClientRepository        _clientRepo;
        private readonly IFileStorageService      _fileStorage;
        private readonly ApplicationDbContext     _context;

        public PurchaseOrderController(
            IPurchaseOrderRepository orderRepo,
            IProposalRepository      proposalRepo,
            IClientRepository        clientRepo,
            IFileStorageService      fileStorage,
            ApplicationDbContext     context)
        {
            _orderRepo    = orderRepo;
            _proposalRepo = proposalRepo;
            _clientRepo   = clientRepo;
            _fileStorage  = fileStorage;
            _context      = context;
        }

        #region Index

        public async Task<IActionResult> Index(string? type, string? status, int? clientId)
        {
            var orders = await _orderRepo.GetAllAsync();

            orders = orders.Where(o => o.IsActive).ToList();

            if (!string.IsNullOrEmpty(type))
                orders = orders.Where(o => o.OrderType == type).ToList();

            if (!string.IsNullOrEmpty(status))
                orders = orders.Where(o => o.Status == status).ToList();

            if (clientId.HasValue)
                orders = orders.Where(o => o.ClientId == clientId.Value).ToList();

            ViewBag.FilterType     = type;
            ViewBag.FilterStatus   = status;
            ViewBag.FilterClientId = clientId;

            return View(orders);
        }

        #endregion

        #region Create

        /// <param name="proposalId">Pre-fill from a proposal (optional).</param>
        public async Task<IActionResult> Create(int? proposalId)
        {
            var vm = new PurchaseOrderViewModel();

            if (proposalId.HasValue)
            {
                var proposal =
                    await _proposalRepo.GetByIdAsync(
                        proposalId.Value);

                if (proposal == null)
                    return NotFound();

                if (!proposal.IsActive)
                {
                    TempData["Error"] =
                        "Inactive proposals cannot be converted.";

                    return RedirectToAction(
                        "Details",
                        "Proposal",
                        new { id = proposalId.Value });
                }

                if (proposal.IsConverted)
                {
                    TempData["Error"] =
                        "This proposal has already been converted.";

                    return RedirectToAction(
                        "Details",
                        "Proposal",
                        new { id = proposalId.Value });
                }

                if (!string.Equals(
                        proposal.Status,
                        "Accepted",
                        StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Error"] =
                        "Only accepted proposals can be converted.";

                    return RedirectToAction(
                        "Details",
                        "Proposal",
                        new { id = proposalId.Value });
                }

                if (proposal.ValidUntil.HasValue &&
                    proposal.ValidUntil.Value.Date < DateTime.Today)
                {
                    TempData["Error"] =
                        "Expired proposals cannot be converted.";

                    return RedirectToAction(
                        "Details",
                        "Proposal",
                        new { id = proposalId.Value });
                }

                vm.Order.ProposalId = proposal.ProposalId;
                vm.Order.ClientId = proposal.ClientId;
                vm.Order.OrderTitle = proposal.ProposalTitle;
                vm.Order.OrderAmount = proposal.ProposalAmount;
                vm.Order.Currency = proposal.Currency;
            }

            await LoadDropdownsAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseOrderViewModel vm)
        {
            ModelState.Remove("Order.OrderNumber");

            await LoadDropdownsAsync(vm);

            if (!ModelState.IsValid)
                return View(vm);

            ProposalModel? sourceProposal = null;

            if (vm.Order.ProposalId.HasValue)
            {
                sourceProposal = await _proposalRepo.GetByIdAsync(
                    vm.Order.ProposalId.Value);

                if (sourceProposal == null)
                {
                    ModelState.AddModelError(
                        "Order.ProposalId",
                        "Selected proposal was not found.");

                    return View(vm);
                }

                if (!sourceProposal.IsActive)
                {
                    ModelState.AddModelError(
                        "Order.ProposalId",
                        "Inactive proposals cannot be converted.");

                    return View(vm);
                }

                if (sourceProposal.IsConverted)
                {
                    ModelState.AddModelError(
                        "Order.ProposalId",
                        "This proposal has already been converted.");

                    return View(vm);
                }

                if (!string.Equals(
                        sourceProposal.Status,
                        "Accepted",
                        StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(
                        "Order.ProposalId",
                        "Only accepted proposals can be converted.");

                    return View(vm);
                }

                if (sourceProposal.ValidUntil.HasValue &&
                    sourceProposal.ValidUntil.Value.Date < DateTime.Today)
                {
                    ModelState.AddModelError(
                        "Order.ProposalId",
                        "Expired proposals cannot be converted.");

                    return View(vm);
                }

                // Preserve trusted proposal values
                vm.Order.ClientId = sourceProposal.ClientId;
                vm.Order.OrderTitle = sourceProposal.ProposalTitle;
                vm.Order.OrderAmount = sourceProposal.ProposalAmount;
                vm.Order.Currency = sourceProposal.Currency;
            }

            if (vm.UploadFile == null)
            {
                ModelState.AddModelError(
                    nameof(vm.UploadFile),
                    "Please upload the order document.");

                return View(vm);
            }

            string folder = $"Orders/{vm.Order.OrderType}";

            var upload = await _fileStorage.UploadAsync(
                vm.UploadFile,
                folder);

            if (!upload.Success)
            {
                ModelState.AddModelError(
                    nameof(vm.UploadFile),
                    upload.ErrorMessage);

                return View(vm);
            }

            ApplyFileFields(vm.Order, upload);

            try
            {
                await _orderRepo.CreateFromProposalWithSequenceAsync
                    (vm.Order,sourceProposal);
            }
            catch
            {
                await _fileStorage.DeleteAsync(upload.RelativePath);
                throw;
            }

            TempData["Success"] =
                $"{vm.Order.OrderType} {vm.Order.OrderNumber} created successfully.";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Edit

        public async Task<IActionResult> Edit(int id)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null) return NotFound();

            var vm = new PurchaseOrderViewModel { Order = order };
            await LoadDropdownsAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PurchaseOrderViewModel vm)
        {
            if (id != vm.Order.PurchaseOrderId) return NotFound();

            await LoadDropdownsAsync(vm);

            if (!ModelState.IsValid)
                return View(vm);

            var existing = await _orderRepo.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.ClientId         = vm.Order.ClientId;
            existing.ProposalId       = vm.Order.ProposalId;
            existing.OrderTitle       = vm.Order.OrderTitle;
            existing.OrderType        = vm.Order.OrderType;
            existing.OrderDate        = vm.Order.OrderDate;
            existing.DeliveryDueDate  = vm.Order.DeliveryDueDate;
            existing.OrderAmount      = vm.Order.OrderAmount;
            existing.Currency         = vm.Order.Currency;
            existing.Status           = vm.Order.Status;
            existing.VendorReference  = vm.Order.VendorReference;
            existing.Remarks          = vm.Order.Remarks;

            if (vm.UploadFile != null)
            {
                string folder = $"Orders/{existing.OrderType}";
                var upload    = await _fileStorage.UploadAsync(vm.UploadFile, folder);
                if (!upload.Success)
                {
                    ModelState.AddModelError("", upload.ErrorMessage);
                    return View(vm);
                }

                // Safe: upload succeeded before deleting old file
                await _fileStorage.DeleteAsync(existing.FilePath);
                ApplyFileFields(existing, upload);
            }

            await _orderRepo.UpdateAsync(existing);
            await _orderRepo.SaveAsync();

            TempData["Success"] = "Order updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Details

        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null) return NotFound();

            return View(order);
        }

        #endregion

        #region Download

        public async Task<IActionResult> Download(int id)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null) return NotFound();

            var bytes = await _fileStorage.DownloadAsync(order.FilePath);
            if (bytes == null)
            {
                TempData["Error"] = "File not found on disk.";
                return RedirectToAction(nameof(Index));
            }

            return File(bytes,
                order.ContentType ?? "application/octet-stream",
                order.FileName);
        }

        #endregion

        #region Delete

        public async Task<IActionResult> Delete(int id)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.IsActive = !order.IsActive;
            order.UpdatedOn = DateTime.Now;

            await _orderRepo.UpdateAsync(order);
            await _orderRepo.SaveAsync();

            TempData["Success"] = order.IsActive
                ? "Order activated successfully."
                : "Order deactivated successfully.";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Update Status (AJAX-friendly)

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null) return NotFound();

            order.Status    = status;
            order.UpdatedOn = DateTime.Now;

            await _orderRepo.UpdateAsync(order);
            await _orderRepo.SaveAsync();

            TempData["Success"] = $"Status updated to '{status}'.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Helpers

        private async Task LoadDropdownsAsync(PurchaseOrderViewModel vm)
        {
            var clients = await _clientRepo.GetAllAsync();
            vm.Clients = clients
                .Where(c => c.IsActive)
                .Select(c => new SelectListItem(c.ClientName, c.ClientId.ToString()));

            var accepted = await _proposalRepo.GetByStatusAsync("Accepted");
            vm.AcceptedProposals = accepted.Select(p =>
                new SelectListItem(
                    $"{p.ProposalNumber} — {p.ProposalTitle}",
                    p.ProposalId.ToString()));
        }

        private static void ApplyFileFields(PurchaseOrderModel target, FileUploadResult upload)
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
