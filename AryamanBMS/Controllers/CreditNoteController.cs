using AryamanBMS.Data;
using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Master")]
    public class CreditNoteController : Controller
    {
        #region Actions

        private readonly ApplicationDbContext _context;
        private readonly ICreditNoteQueryService _queryService;

        public CreditNoteController(ApplicationDbContext context, ICreditNoteQueryService queryService)
        {
            _context = context;
            _queryService = queryService;
        }

        public async Task<IActionResult> Index(
            string? search,
            string sortBy = "CreatedOn",
            string sortOrder = "desc")
        {
            var notes = await _queryService.GetAllAsync(search, sortBy, sortOrder);
            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            return View(notes);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? invoiceId)
        {
            await LoadInvoicesAsync();

            return View(new CreditNoteModel
            {
                OriginalInvoiceId = invoiceId ?? 0
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreditNoteModel model)
        {
            Normalize(model);
            await ValidateCreditNoteAsync(model);

            if (!ModelState.IsValid)
            {
                await LoadInvoicesAsync();
                return View(model);
            }

            model.CreditNoteNo = await GenerateCreditNoteNoAsync();
            model.TotalCredit =
                model.TaxableValueReduction +
                model.CGSTAdjustment +
                model.SGSTAdjustment +
                model.IGSTAdjustment;

            model.CreatedByUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            model.ApprovedByUserId = model.CreatedByUserId;
            model.ApprovedOn = DateTime.Now;
            model.CreatedOn = DateTime.Now;

            var invoice =
                await _context.Invoices
                    .FirstAsync(x =>
                        x.InvoiceId == model.OriginalInvoiceId);

            ApplyCreditToInvoice(
                invoice,
                model.TotalCredit,
                model.CreditNoteNo);

            await _context.CreditNotes.AddAsync(model);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Credit note created successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadInvoicesAsync()
        {
            ViewBag.Invoices = await _queryService.GetIssuedInvoicesAsync();
        }

        private static void Normalize(CreditNoteModel model)
        {
            model.Reason = model.Reason?.Trim() ?? string.Empty;
            model.GSTPeriod =
                string.IsNullOrWhiteSpace(model.GSTPeriod)
                    ? null
                    : model.GSTPeriod.Trim();

            model.TaxableValueReduction =
                Math.Round(model.TaxableValueReduction, 2);
            model.CGSTAdjustment =
                Math.Round(model.CGSTAdjustment, 2);
            model.SGSTAdjustment =
                Math.Round(model.SGSTAdjustment, 2);
            model.IGSTAdjustment =
                Math.Round(model.IGSTAdjustment, 2);
        }

        private async Task ValidateCreditNoteAsync(CreditNoteModel model)
        {
            var invoice =
                await _context.Invoices
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.InvoiceId == model.OriginalInvoiceId &&
                        !x.IsDeleted &&
                        x.InvoiceStatus == "Issued");

            if (invoice == null)
            {
                ModelState.AddModelError(
                    nameof(model.OriginalInvoiceId),
                    "Select a valid issued invoice.");
                return;
            }

            if (string.IsNullOrWhiteSpace(model.Reason))
            {
                ModelState.AddModelError(
                    nameof(model.Reason),
                    "Reason is required.");
            }

            decimal totalCredit =
                model.TaxableValueReduction +
                model.CGSTAdjustment +
                model.SGSTAdjustment +
                model.IGSTAdjustment;

            if (totalCredit <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.TotalCredit),
                    "Credit note total must be greater than zero.");
            }

            if (totalCredit > invoice.GrandTotal)
            {
                ModelState.AddModelError(
                    nameof(model.TotalCredit),
                    "Credit note cannot exceed invoice total.");
            }
        }

        private async Task<string> GenerateCreditNoteNoAsync()
        {
            int count = await _context.CreditNotes.CountAsync();

            return $"CRN-{DateTime.Now:yyMM}-{count + 1:0000}";
        }

        private static void ApplyCreditToInvoice(
            InvoiceModel invoice,
            decimal creditAmount,
            string creditNoteNo)
        {
            invoice.GrandTotal =
                Math.Max(
                    0,
                    Math.Round(
                        invoice.GrandTotal - creditAmount,
                        2));

            invoice.BalanceAmount =
                Math.Max(
                    0,
                    Math.Round(
                        invoice.GrandTotal - invoice.PaidAmount,
                        2));

            RefreshPaymentStatus(invoice);

            string note =
                $"Credit note {creditNoteNo} applied: {creditAmount:N2}.";

            invoice.Remarks =
                string.IsNullOrWhiteSpace(invoice.Remarks)
                    ? note
                    : $"{invoice.Remarks} | {note}";
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
        #endregion
    }
}
