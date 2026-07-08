using AryamanBMS.Data;
using AryamanBMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class DebitNoteController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DebitNoteController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var notes =
                await _context.DebitNotes
                    .AsNoTracking()
                    .Include(x => x.OriginalInvoice)
                        .ThenInclude(x => x!.Client)
                    .OrderByDescending(x => x.CreatedOn)
                    .ToListAsync();

            return View(notes);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? invoiceId)
        {
            await LoadInvoicesAsync();

            return View(new DebitNoteModel
            {
                OriginalInvoiceId = invoiceId ?? 0
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DebitNoteModel model)
        {
            Normalize(model);
            await ValidateDebitNoteAsync(model);

            if (!ModelState.IsValid)
            {
                await LoadInvoicesAsync();
                return View(model);
            }

            model.DebitNoteNo = await GenerateDebitNoteNoAsync();
            model.TotalDebit =
                model.AdditionalTaxableValue +
                model.CGST +
                model.SGST +
                model.IGST;

            model.CreatedByUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            model.ApprovedByUserId = model.CreatedByUserId;
            model.ApprovedOn = DateTime.Now;
            model.CreatedOn = DateTime.Now;

            var invoice =
                await _context.Invoices
                    .FirstAsync(x =>
                        x.InvoiceId == model.OriginalInvoiceId);

            ApplyDebitToInvoice(
                invoice,
                model.TotalDebit,
                model.DebitNoteNo);

            await _context.DebitNotes.AddAsync(model);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Debit note created successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadInvoicesAsync()
        {
            ViewBag.Invoices =
                await _context.Invoices
                    .AsNoTracking()
                    .Include(x => x.Client)
                    .Where(x =>
                        !x.IsDeleted &&
                        x.InvoiceStatus == "Issued")
                    .OrderByDescending(x => x.InvoiceDate)
                    .ToListAsync();
        }

        private static void Normalize(DebitNoteModel model)
        {
            model.Reason =
                model.Reason?.Trim() ?? string.Empty;

            model.AdditionalTaxableValue =
                Math.Round(model.AdditionalTaxableValue, 2);

            model.CGST =
                Math.Round(model.CGST, 2);

            model.SGST =
                Math.Round(model.SGST, 2);

            model.IGST =
                Math.Round(model.IGST, 2);
        }

        private async Task ValidateDebitNoteAsync(DebitNoteModel model)
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

            if (model.AdditionalTaxableValue < 0 ||
                model.CGST < 0 ||
                model.SGST < 0 ||
                model.IGST < 0)
            {
                ModelState.AddModelError(
                    nameof(model.TotalDebit),
                    "Debit note values cannot be negative.");
            }

            decimal totalDebit =
                model.AdditionalTaxableValue +
                model.CGST +
                model.SGST +
                model.IGST;

            if (totalDebit <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.TotalDebit),
                    "Debit note total must be greater than zero.");
            }
        }

        private async Task<string> GenerateDebitNoteNoAsync()
        {
            int count =
                await _context.DebitNotes.CountAsync();

            return $"DBN-{DateTime.Now:yyMM}-{count + 1:0000}";
        }

        private static void ApplyDebitToInvoice(
            InvoiceModel invoice,
            decimal debitAmount,
            string debitNoteNo)
        {
            invoice.GrandTotal =
                Math.Round(
                    invoice.GrandTotal + debitAmount,
                    2);

            invoice.BalanceAmount =
                Math.Max(
                    0,
                    Math.Round(
                        invoice.GrandTotal - invoice.PaidAmount,
                        2));

            RefreshPaymentStatus(invoice);

            string note =
                $"Debit note {debitNoteNo} applied: {debitAmount:N2}.";

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
    }
}
