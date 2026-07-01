using AryamanBMS.Data;
using AryamanBMS.Models;
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
                var proposal = await _proposalRepo.GetByIdAsync(proposalId.Value);
                if (proposal != null)
                {
                    vm.Order.ProposalId   = proposal.ProposalId;
                    vm.Order.ClientId     = proposal.ClientId;
                    vm.Order.OrderTitle   = proposal.ProposalTitle;
                    vm.Order.OrderAmount  = proposal.ProposalAmount;
                    vm.Order.Currency     = proposal.Currency;
                }
            }

            await LoadDropdownsAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseOrderViewModel vm)
        {
            await LoadDropdownsAsync(vm);

            if (!ModelState.IsValid)
                return View(vm);

            // File is required on Create
            if (vm.UploadFile == null)
            {
                ModelState.AddModelError(nameof(vm.UploadFile), "Please upload the order document.");
                return View(vm);
            }

            string folder = $"Orders/{vm.Order.OrderType}";
            var upload    = await _fileStorage.UploadAsync(vm.UploadFile, folder);
            if (!upload.Success)
            {
                ModelState.AddModelError("", upload.ErrorMessage);
                return View(vm);
            }

            vm.Order.OrderNumber = await GenerateOrderNumberAsync(vm.Order.OrderType);
            ApplyFileFields(vm.Order, upload);
            vm.Order.CreatedOn = DateTime.Now;
            vm.Order.IsActive  = true;

            await _orderRepo.AddAsync(vm.Order);

            // Mark the source proposal as converted if linked
            if (vm.Order.ProposalId.HasValue)
            {
                var proposal = await _proposalRepo.GetByIdAsync(vm.Order.ProposalId.Value);
                if (proposal != null)
                {
                    proposal.IsConverted = true;
                    await _proposalRepo.UpdateAsync(proposal);
                }
            }

            await _orderRepo.SaveAsync();

            TempData["Success"] = $"{vm.Order.OrderType} {vm.Order.OrderNumber} created successfully.";
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

        private async Task<string> GenerateOrderNumberAsync(string orderType)
        {
            // PO-2526-0001  or  WO-2526-0001
            string fy = DateTime.Now.Month >= 4
                ? $"{DateTime.Now.Year % 100}{(DateTime.Now.Year + 1) % 100}"
                : $"{(DateTime.Now.Year - 1) % 100}{DateTime.Now.Year % 100}";

            int count = await _context.PurchaseOrders
                .CountAsync(o => o.OrderType == orderType) + 1;

            return $"{orderType}-{fy}-{count:D4}";
        }

        #endregion
    }
}
