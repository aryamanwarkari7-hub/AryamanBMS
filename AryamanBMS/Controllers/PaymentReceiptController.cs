using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class PaymentReceiptController : Controller
    {
        private readonly IPaymentReceiptRepository _paymentRepository;

        public PaymentReceiptController(IPaymentReceiptRepository paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public async Task<IActionResult> Index(
                  string? search,
                  int? clientId,
                  string? status)
        {
            var payments = await _paymentRepository.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim().ToLower();

                payments = payments
                    .Where(x =>
                        (x.ReceiptNo != null && x.ReceiptNo.ToLower().Contains(keyword)) ||
                        (x.Client != null && x.Client.ClientName.ToLower().Contains(keyword)) ||
                        (x.Invoice != null && x.Invoice.InvoiceNo.ToLower().Contains(keyword)))
                    .ToList();
            }

            if (clientId.HasValue && clientId.Value > 0)
            {
                payments = payments
                    .Where(x => x.ClientId == clientId.Value)
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                payments = status switch
                {
                    "Active" => payments.Where(x => !x.IsCancelled).ToList(),
                    "Cancelled" => payments.Where(x => x.IsCancelled).ToList(),
                    _ => payments
                };
            }

            var allPayments = await _paymentRepository.GetAllAsync();
            var activePayments = allPayments.Where(x => !x.IsCancelled).ToList();

            var model = new PaymentReceiptTrackerViewModel
            {
                PaymentReceipts = payments,
                Clients = await _paymentRepository.GetClientsAsync(),

                Summary = new PaymentSummary
                {
                    TotalReceipts = allPayments.Count,
                    TotalReceived = activePayments.Sum(x => x.AmountReceived),
                    PendingCount = activePayments.Count,
                    CompletedCount = activePayments.Count,
                    CancelledCount = allPayments.Count(x => x.IsCancelled)
                }
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new PaymentReceiptModel
            {
                ReceiptNo = await _paymentRepository.GenerateReceiptNoAsync(),
                ReceiptDate = DateTime.Today,
                PaymentMode = "Cash",
                IsActive = true,
                IsCancelled = false
            };

            await LoadFormDataAsync(model.ClientId);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaymentReceiptModel model)
        {
            await LoadFormDataAsync(model.ClientId);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var invoice = await GetSelectedInvoiceAsync(model.ClientId, model.InvoiceId);

            if (!ValidateReceipt(model, invoice, invoice?.BalanceAmount ?? 0))
            {
                return View(model);
            }

            model.IsActive = true;
            model.IsCancelled = false;

            await _paymentRepository.AddAsync(model);

            TempData["Success"] = "Payment receipt created successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);

            if (payment == null || payment.IsCancelled)
            {
                return NotFound();
            }

            await LoadFormDataAsync(payment.ClientId);

            return View(payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PaymentReceiptModel model)
        {
            if (id != model.PaymentReceiptId)
            {
                return NotFound();
            }

            var payment = await _paymentRepository.GetByIdAsync(id);

            if (payment == null || payment.IsCancelled)
            {
                return NotFound();
            }

            await LoadFormDataAsync(model.ClientId);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var invoice = await GetSelectedInvoiceAsync(model.ClientId, model.InvoiceId);

            decimal availableBalance = invoice?.BalanceAmount ?? 0;

            if (invoice != null &&
                payment.InvoiceId == model.InvoiceId &&
                !payment.IsCancelled)
            {
                availableBalance += payment.AmountReceived;
            }

            if (!ValidateReceipt(model, invoice, availableBalance))
            {
                return View(model);
            }

            payment.ClientId = model.ClientId;
            payment.InvoiceId = model.InvoiceId;
            payment.ReceiptDate = model.ReceiptDate;
            payment.AmountReceived = model.AmountReceived;
            payment.PaymentMode = model.PaymentMode;
            payment.BankName = model.BankName;
            payment.TransactionNo = model.TransactionNo;
            payment.ReferenceNo = model.ReferenceNo;
            payment.ReceivedBy = model.ReceivedBy;
            payment.Remarks = model.Remarks;

            await _paymentRepository.UpdateAsync(payment);

            TempData["Success"] = "Payment receipt updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);

            if (payment == null)
            {
                return NotFound();
            }

            try
            {
                await _paymentRepository.DeleteAsync(payment);

                TempData["Success"] = "Payment receipt cancelled successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoicesByClient(
               int clientId,
               int? selectedInvoiceId)
        {
            var invoices = await _paymentRepository.GetInvoicesByClientAsync(clientId);

            var result = invoices
                .Where(x =>
                    !x.IsDeleted &&
                    (x.BalanceAmount > 0 ||
                     x.InvoiceId == selectedInvoiceId))
                .OrderByDescending(x => x.InvoiceDate)
                .Select(x => new
                {
                    value = x.InvoiceId,
                    text = $"{x.InvoiceNo} - Balance {x.BalanceAmount:N2}",
                    amount = x.GrandTotal,
                    balance = x.BalanceAmount,
                    invoiceDate = x.InvoiceDate.ToString("yyyy-MM-dd")
                });

            return Json(result);
        }

        private async Task LoadFormDataAsync(int? clientId = null)
        {
            var clients = await _paymentRepository.GetClientsAsync();

            ViewBag.Clients = new SelectList(
                clients,
                "ClientId",
                "ClientName",
                clientId);

            var invoices = clientId.HasValue && clientId.Value > 0
                ? await _paymentRepository.GetInvoicesByClientAsync(clientId.Value)
                : await _paymentRepository.GetInvoicesAsync();

            ViewBag.Invoices = new SelectList(
                invoices.Where(x => !x.IsDeleted && x.BalanceAmount > 0),
                "InvoiceId",
                "InvoiceNo");
        }

        private async Task<InvoiceModel?> GetSelectedInvoiceAsync(
            int clientId,
            int invoiceId)
        {
            return (await _paymentRepository.GetInvoicesByClientAsync(clientId))
                .FirstOrDefault(x => x.InvoiceId == invoiceId && !x.IsDeleted);
        }

        private bool ValidateReceipt(
            PaymentReceiptModel model,
            InvoiceModel? invoice,
            decimal availableBalance)
        {
            if (invoice == null)
            {
                ModelState.AddModelError(
                    nameof(model.InvoiceId),
                    "Please select a valid invoice.");

                return false;
            }

            if (model.AmountReceived <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.AmountReceived),
                    "Amount received should be greater than zero.");

                return false;
            }

            if (model.AmountReceived > availableBalance)
            {
                ModelState.AddModelError(
                    nameof(model.AmountReceived),
                    $"Payment cannot exceed balance amount {availableBalance:N2}.");

                return false;
            }

            if (model.ReceiptDate < invoice.InvoiceDate)
            {
                ModelState.AddModelError(
                    nameof(model.ReceiptDate),
                    "Receipt date cannot be before invoice date.");

                return false;
            }

            return true;
        }
    }
}