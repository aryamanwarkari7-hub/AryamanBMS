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
    [Authorize(Roles = "Admin,Finance,Master")]
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

        public async Task<IActionResult> Index(
           string? type,
           string? status,
           int? clientId,
           string? search,
           string sortBy = "OrderDate",
           string sortOrder = "desc")
        {
            var orders = await _orderRepo.GetAllAsync();

            orders = orders.Where(o => o.IsActive).ToList();

            if (!string.IsNullOrEmpty(type))
                orders = orders.Where(o => o.OrderType == type).ToList();

            if (!string.IsNullOrEmpty(status))
                orders = orders.Where(o => o.Status == status).ToList();

            if (clientId.HasValue)
                orders = orders.Where(o => o.ClientId == clientId.Value).ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim().ToLower();

                orders = orders
                    .Where(o =>
                        (!string.IsNullOrWhiteSpace(o.OrderNumber) &&
                            o.OrderNumber.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(o.OrderTitle) &&
                            o.OrderTitle.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(o.VendorReference) &&
                            o.VendorReference.ToLower().Contains(keyword)) ||
                        (o.Client != null &&
                            !string.IsNullOrWhiteSpace(o.Client.ClientName) &&
                            o.Client.ClientName.ToLower().Contains(keyword)) ||
                        (o.Proposal != null &&
                            !string.IsNullOrWhiteSpace(o.Proposal.ProposalNumber) &&
                            o.Proposal.ProposalNumber.ToLower().Contains(keyword)))
                    .ToList();
            }

            bool desc =
    string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            orders = sortBy switch
            {
                "OrderNumber" => desc
                    ? orders.OrderByDescending(o => o.OrderNumber).ToList()
                    : orders.OrderBy(o => o.OrderNumber).ToList(),

                "Type" => desc
                    ? orders.OrderByDescending(o => o.OrderType).ToList()
                    : orders.OrderBy(o => o.OrderType).ToList(),

                "Client" => desc
                    ? orders.OrderByDescending(o => o.Client?.ClientName).ToList()
                    : orders.OrderBy(o => o.Client?.ClientName).ToList(),

                "Title" => desc
                    ? orders.OrderByDescending(o => o.OrderTitle).ToList()
                    : orders.OrderBy(o => o.OrderTitle).ToList(),

                "DueDate" => desc
                    ? orders.OrderByDescending(o => o.DeliveryDueDate).ToList()
                    : orders.OrderBy(o => o.DeliveryDueDate).ToList(),

                "Amount" => desc
                    ? orders.OrderByDescending(o => o.OrderAmount).ToList()
                    : orders.OrderBy(o => o.OrderAmount).ToList(),

                "Status" => desc
                    ? orders.OrderByDescending(o => o.Status).ToList()
                    : orders.OrderBy(o => o.Status).ToList(),

                _ => desc
                    ? orders.OrderByDescending(o => o.OrderDate).ToList()
                    : orders.OrderBy(o => o.OrderDate).ToList()
            };

            ViewBag.FilterType     = type;
            ViewBag.FilterStatus   = status;
            ViewBag.FilterClientId = clientId;
            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

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

            if (order == null)
                return NotFound();

            if (!order.IsActive)
            {
                TempData["Error"] =
                    "Inactive orders cannot be edited.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (IsFinalOrderStatus(order.Status))
            {
                TempData["Error"] =
                    "Delivered, closed, or cancelled orders cannot be edited.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var vm = new PurchaseOrderViewModel
            {
                Order = order
            };

            await LoadDropdownsAsync(vm);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
          PurchaseOrderViewModel vm)
        {
            if (id != vm.Order.PurchaseOrderId)
                return NotFound();

            var existing =
                await _orderRepo.GetByIdAsync(id);

            if (existing == null)
                return NotFound();

            if (!existing.IsActive)
            {
                TempData["Error"] =
                    "Inactive orders cannot be edited.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (IsFinalOrderStatus(existing.Status))
            {
                TempData["Error"] =
                    "Delivered, closed, or cancelled orders cannot be edited.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (!IsAllowedStatusTransition(
                    existing.Status,
                    vm.Order.Status))
            {
                ModelState.AddModelError(
                    "Order.Status",
                    $"Status cannot change from {existing.Status} " +
                    $"to {vm.Order.Status}.");
            }

            if (vm.Order.DeliveryDueDate.HasValue &&
                vm.Order.DeliveryDueDate.Value.Date <
                vm.Order.OrderDate.Date)
            {
                ModelState.AddModelError(
                    "Order.DeliveryDueDate",
                    "Delivery due date cannot be before order date.");
            }

            if (vm.Order.OrderAmount.HasValue &&
                vm.Order.OrderAmount.Value < 0)
            {
                ModelState.AddModelError(
                    "Order.OrderAmount",
                    "Order amount cannot be negative.");
            }

            bool linkedToInvoice =
                await HasLinkedInvoiceAsync(id);

            if (existing.ProposalId.HasValue)
            {
                vm.Order.ProposalId = existing.ProposalId;
                vm.Order.ClientId = existing.ClientId;
            }

            if (linkedToInvoice)
            {
                vm.Order.ClientId = existing.ClientId;
                vm.Order.ProposalId = existing.ProposalId;
            }

            await LoadDropdownsAsync(vm);

            if (!ModelState.IsValid)
                return View(vm);

            string? oldFilePath = existing.FilePath;
            FileUploadResult? uploadedFile = null;

            existing.ClientId = vm.Order.ClientId;
            existing.ProposalId = vm.Order.ProposalId;
            existing.OrderTitle = vm.Order.OrderTitle;
            existing.OrderType = vm.Order.OrderType;
            existing.OrderDate = vm.Order.OrderDate;
            existing.DeliveryDueDate = vm.Order.DeliveryDueDate;
            existing.OrderAmount = vm.Order.OrderAmount;
            existing.Currency = vm.Order.Currency;
            existing.Status = vm.Order.Status;
            existing.VendorReference = vm.Order.VendorReference;
            existing.Remarks = vm.Order.Remarks;

            if (vm.UploadFile != null)
            {
                string folder =
                    $"Orders/{existing.OrderType}";

                uploadedFile =
                    await _fileStorage.UploadAsync(
                        vm.UploadFile,
                        folder);

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
            }

            try
            {
                await _orderRepo.UpdateAsync(existing);
                await _orderRepo.SaveAsync();
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
                "Order updated successfully.";

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
            var order =
                await _orderRepo.GetByIdAsync(id);

            if (order == null)
                return NotFound();

            if (await HasLinkedInvoiceAsync(id))
            {
                TempData["Error"] =
                    "Orders linked to invoices cannot be activated or deactivated.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order =
                await _orderRepo.GetByIdAsync(id);

            if (order == null)
                return NotFound();

            if (await HasLinkedInvoiceAsync(id))
            {
                TempData["Error"] =
                    "Orders linked to invoices cannot be activated or deactivated.";

                return RedirectToAction(nameof(Index));
            }

            if (order.Status == "Closed" ||
                order.Status == "Cancelled")
            {
                TempData["Error"] =
                    "Closed or cancelled orders cannot be deactivated.";

                return RedirectToAction(nameof(Index));
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
        public async Task<IActionResult> UpdateStatus(
    int id,
    string status)
        {
            var order =
                await _orderRepo.GetByIdAsync(id);

            if (order == null)
                return NotFound();

            if (!order.IsActive)
            {
                TempData["Error"] =
                    "Inactive orders cannot change status.";

                return RedirectToAction(nameof(Index));
            }

            if (!IsAllowedStatusTransition(
                    order.Status,
                    status))
            {
                TempData["Error"] =
                    $"Status cannot change from {order.Status} to {status}.";

                return RedirectToAction(nameof(Index));
            }

            order.Status = status;
            order.UpdatedOn = DateTime.Now;

            await _orderRepo.UpdateAsync(order);
            await _orderRepo.SaveAsync();

            TempData["Success"] =
                $"Status updated to '{status}'.";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Helpers

        private static bool IsFinalOrderStatus(string status)
        {
            return status is "Delivered" or "Closed" or "Cancelled";
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
                "Open" => newStatus is
                    "Acknowledged" or
                    "Cancelled",

                "Acknowledged" => newStatus is
                    "InProgress" or
                    "Cancelled",

                "InProgress" => newStatus is
                    "Delivered" or
                    "Cancelled",

                "Delivered" => newStatus is "Closed",

                "Closed" => false,

                "Cancelled" => false,

                _ => false
            };
        }

        private async Task<bool> HasLinkedInvoiceAsync(int purchaseOrderId)
        {
            return await _context.Invoices.AnyAsync(x =>
                x.PurchaseWorkOrderId == purchaseOrderId &&
                !x.IsDeleted);
        }


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
