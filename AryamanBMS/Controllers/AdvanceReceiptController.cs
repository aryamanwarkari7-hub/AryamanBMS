using AryamanBMS.Data;
using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Master")]
    public class AdvanceReceiptController : Controller
    {
        #region Actions

        private readonly IAdvanceReceiptQueryService _queryService;
        private readonly IAdvanceReceiptService _advanceReceiptService;
        private readonly IAdvanceReceiptRepository _advanceReceiptRepository;

        public AdvanceReceiptController(IAdvanceReceiptQueryService queryService, IAdvanceReceiptService advanceReceiptService, IAdvanceReceiptRepository advanceReceiptRepository)
        {
            _queryService = queryService;
            _advanceReceiptService = advanceReceiptService;
            _advanceReceiptRepository = advanceReceiptRepository;
        }

        public async Task<IActionResult> Index(string? search, string sortBy = "ReceiptDate", string sortOrder = "desc")
        {
            var receipts = await _queryService.GetAllAsync(search, sortBy, sortOrder);
            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            return View(receipts);
        }

#if false // Superseded by IAdvanceReceiptQueryService
        private async Task<IActionResult> LegacyIndex(
    string? search,
    string sortBy = "ReceiptDate",
    string sortOrder = "desc")
        {
            var query =
                _context.AdvanceReceipts
                    .AsNoTracking()
                    .Include(x => x.Client)
                    .Include(x => x.Project)
                    .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim().ToLower();

                query = query.Where(x =>
                    x.AdvanceReceiptNo.ToLower().Contains(keyword) ||
                    x.PaymentMode.ToLower().Contains(keyword) ||
                    (x.PaymentReference != null &&
                        x.PaymentReference.ToLower().Contains(keyword)) ||
                    (x.Client != null &&
                        x.Client.ClientName.ToLower().Contains(keyword)) ||
                    (x.Project != null &&
                        x.Project.ProjectName.ToLower().Contains(keyword)));
            }

            bool desc =
                string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            query = sortBy switch
            {
                "ReceiptNo" => desc
                    ? query.OrderByDescending(x => x.AdvanceReceiptNo)
                    : query.OrderBy(x => x.AdvanceReceiptNo),

                "Client" => desc
                    ? query.OrderByDescending(x => x.Client!.ClientName)
                    : query.OrderBy(x => x.Client!.ClientName),

                "Project" => desc
                    ? query.OrderByDescending(x => x.Project!.ProjectName)
                    : query.OrderBy(x => x.Project!.ProjectName),

                "Amount" => desc
                    ? query.OrderByDescending(x => x.Amount)
                    : query.OrderBy(x => x.Amount),

                "AvailableBalance" => desc
                    ? query.OrderByDescending(x => x.AvailableBalance)
                    : query.OrderBy(x => x.AvailableBalance),

                "PaymentMode" => desc
                    ? query.OrderByDescending(x => x.PaymentMode)
                    : query.OrderBy(x => x.PaymentMode),

                _ => desc
                    ? query.OrderByDescending(x => x.ReceiptDate)
                           .ThenByDescending(x => x.AdvanceReceiptId)
                    : query.OrderBy(x => x.ReceiptDate)
                           .ThenBy(x => x.AdvanceReceiptId)
            };

            var receipts =
                await query.ToListAsync();

            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

            return View(receipts);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();

            return View(new AdvanceReceiptModel
            {
                ReceiptDate = DateTime.Today,
                PaymentMode = "Bank Transfer"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdvanceReceiptModel model)
        {
            var validation = await _advanceReceiptService.ValidateAsync(model);
            foreach (var error in validation) ModelState.AddModelError(error.Key, error.Value);

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return View(model);
            }

            await _advanceReceiptService.CreateAsync(model, User.FindFirstValue(ClaimTypes.NameIdentifier));

            TempData["Success"] =
                "Advance receipt created successfully.";

            return RedirectToAction(nameof(Index));
        }

#endif
        [HttpGet]
        public async Task<IActionResult> Apply(int id)
        {
            var receipt = await _advanceReceiptRepository.GetAvailableByIdAsync(id);

            if (receipt == null)
            {
                return NotFound();
            }

            if (receipt.AvailableBalance <= 0)
            {
                TempData["Error"] =
                    "This advance receipt has no available balance.";

                return RedirectToAction(nameof(Index));
            }

            var vm = new AdvanceReceiptApplyViewModel
            {
                AdvanceReceiptId = receipt.AdvanceReceiptId,
                AdvanceReceiptNo = receipt.AdvanceReceiptNo,
                ClientName = receipt.Client?.ClientName ?? string.Empty,
                AvailableBalance = receipt.AvailableBalance,
                AmountToAdjust = receipt.AvailableBalance
            };

            await LoadApplyInvoicesAsync(vm, receipt.ClientId);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(
            int id,
            AdvanceReceiptApplyViewModel vm)
        {
            if (id != vm.AdvanceReceiptId)
            {
                return NotFound();
            }

            var receipt = await _advanceReceiptRepository.GetAvailableByIdAsync(id);

            if (receipt == null)
            {
                return NotFound();
            }

            vm.AdvanceReceiptNo = receipt.AdvanceReceiptNo;
            vm.ClientName = receipt.Client?.ClientName ?? string.Empty;
            vm.AvailableBalance = receipt.AvailableBalance;

            vm.AmountToAdjust =
                Math.Round(vm.AmountToAdjust, 2);

            if (vm.AmountToAdjust <= 0)
            {
                ModelState.AddModelError(
                    nameof(vm.AmountToAdjust),
                    "Adjustment amount must be greater than zero.");
            }

            if (vm.AmountToAdjust > receipt.AvailableBalance)
            {
                ModelState.AddModelError(
                    nameof(vm.AmountToAdjust),
                    $"Amount cannot exceed available advance balance {receipt.AvailableBalance:N2}.");
            }

            if (!ModelState.IsValid)
            {
                await LoadApplyInvoicesAsync(vm, receipt.ClientId);
                return View(vm);
            }

            var workflowErrors = await _advanceReceiptService.ApplyAsync(
                id,
                vm.InvoiceId,
                vm.AmountToAdjust,
                vm.Remarks);

            foreach (var error in workflowErrors)
            {
                ModelState.AddModelError(error.Key, error.Value);
            }

            if (!ModelState.IsValid)
            {
                await LoadApplyInvoicesAsync(vm, receipt.ClientId);
                return View(vm);
            }

            TempData["Success"] =
                "Advance receipt adjusted against invoice successfully.";

            return RedirectToAction(nameof(Index));

#if false // Superseded by IAdvanceReceiptService.ApplyAsync
            receipt.AdjustedAmount =
                Math.Round(
                    receipt.AdjustedAmount + vm.AmountToAdjust,
                    2);

            receipt.AvailableBalance =
                Math.Max(
                    0,
                    Math.Round(
                        receipt.Amount - receipt.AdjustedAmount,
                        2));

            receipt.UpdatedOn = DateTime.Now;

            invoice!.PaidAmount =
                Math.Round(
                    invoice.PaidAmount + vm.AmountToAdjust,
                    2);

            invoice.BalanceAmount =
                Math.Max(
                    0,
                    Math.Round(
                        invoice.GrandTotal - invoice.PaidAmount,
                        2));

            RefreshPaymentStatus(invoice);

            string adjustmentNote =
                $"Advance {receipt.AdvanceReceiptNo} adjusted against invoice {invoice.InvoiceNo}: {vm.AmountToAdjust:N2}.";

            if (!string.IsNullOrWhiteSpace(vm.Remarks))
            {
                adjustmentNote += $" {vm.Remarks.Trim()}";
            }

            receipt.Remarks =
                string.IsNullOrWhiteSpace(receipt.Remarks)
                    ? adjustmentNote
                    : $"{receipt.Remarks} | {adjustmentNote}";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Advance receipt adjusted against invoice successfully.";

            return RedirectToAction(nameof(Index));
#endif
        }

        private async Task LoadDropdownsAsync()
        {
            ViewBag.Clients = await _advanceReceiptRepository.GetActiveClientsAsync();
            ViewBag.Projects = await _advanceReceiptRepository.GetActiveProjectsAsync();
        }

        private async Task LoadApplyInvoicesAsync(
            AdvanceReceiptApplyViewModel vm,
            int clientId)
        {
            var invoices = await _advanceReceiptRepository.GetOutstandingInvoicesForClientAsync(clientId);

            vm.Invoices =
                invoices.Select(x =>
                    new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = x.InvoiceId.ToString(),
                        Text = $"{x.InvoiceNo} - Balance {x.BalanceAmount:N2}",
                        Selected = x.InvoiceId == vm.InvoiceId
                    });
        }

#if false // Superseded by IAdvanceReceiptService
        private static void NormalizeAdvanceReceipt(
            AdvanceReceiptModel model)
        {
            model.PaymentMode =
                model.PaymentMode?.Trim() ?? string.Empty;

            model.PaymentReference =
                string.IsNullOrWhiteSpace(model.PaymentReference)
                    ? null
                    : model.PaymentReference.Trim();

            model.Remarks =
                string.IsNullOrWhiteSpace(model.Remarks)
                    ? null
                    : model.Remarks.Trim();

            model.Amount =
                Math.Round(model.Amount, 2);
        }

        private async Task ValidateAdvanceReceiptAsync(
            AdvanceReceiptModel model)
        {
            if (model.ClientId <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.ClientId),
                    "Client is required.");
            }

            if (model.Amount <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.Amount),
                    "Advance amount must be greater than zero.");
            }

            if (model.ReceiptDate.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.ReceiptDate),
                    "Receipt date cannot be in the future.");
            }

            if (!string.IsNullOrWhiteSpace(model.PaymentReference))
            {
                bool duplicateReference =
                    await _context.AdvanceReceipts
                        .AsNoTracking()
                        .AnyAsync(x =>
                            x.PaymentReference == model.PaymentReference &&
                            !x.IsCancelled);

                if (duplicateReference)
                {
                    ModelState.AddModelError(
                        nameof(model.PaymentReference),
                        "This payment reference is already used.");
                }
            }
        }

        private async Task<string> GenerateAdvanceReceiptNoAsync()
        {
            int count =
                await _context.AdvanceReceipts.CountAsync();

            return $"ADV-{DateTime.Now:yyMM}-{count + 1:0000}";
        }

        private static void RefreshPaymentStatus(
            InvoiceModel invoice)
        {
            if (invoice.InvoiceStatus == "Cancelled")
            {
                return;
            }

            if (invoice.BalanceAmount <= 0)
            {
                invoice.PaymentStatus = "Paid";
            }
            else if (invoice.DueDate.HasValue &&
                     invoice.DueDate.Value.Date < DateTime.Today)
            {
                invoice.PaymentStatus = "Overdue";
            }
            else if (invoice.PaidAmount > 0)
            {
                invoice.PaymentStatus = "Partially Paid";
            }
            else
            {
                invoice.PaymentStatus = "Unpaid";
            }
        }
#endif
        #endregion
    }
}
