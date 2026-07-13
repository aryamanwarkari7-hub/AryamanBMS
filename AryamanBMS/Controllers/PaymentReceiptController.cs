using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class PaymentReceiptController : Controller
    {
        private readonly IPaymentReceiptRepository _paymentRepository;
        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly INotificationService _notificationService;

        public PaymentReceiptController(
          IPaymentReceiptRepository paymentRepository,
          UserManager<ApplicationUserModel> userManager,
          INotificationService notificationService)
        {
            _paymentRepository = paymentRepository;
            _userManager = userManager;
            _notificationService = notificationService;
        }


        #region Index
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
                    ActiveReceiptCount = activePayments.Count,
                    CancelledReceiptCount = allPayments.Count(x => x.IsCancelled),
                }
            };

            return View(model);
        }

        #endregion

        #region Create

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

            ModelState.Remove(nameof(model.ReceiptNo));

            NormalizeReceipt(model);

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

            bool duplicateReference =
               await _paymentRepository.TransactionReferenceExistsAsync(
                   model.TransactionNo,
                   model.ReferenceNo);

            if (duplicateReference)
            {
                ModelState.AddModelError(
                    nameof(model.TransactionNo),
                    "This transaction or reference number already exists.");

                return View(model);
            }

            try
            {
                await _paymentRepository.AddAsync(model);

                var updatedInvoice =
                    (await _paymentRepository
                        .GetInvoicesByClientAsync(model.ClientId))
                    .FirstOrDefault(x =>
                        x.InvoiceId == model.InvoiceId);

                if (updatedInvoice != null)
                {
                    await NotifyPaymentReceivedAsync(
                        model,
                        updatedInvoice);
                }
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(
                    nameof(model.AmountReceived),
                    ex.Message);

                await LoadFormDataAsync(model.ClientId);

                return View(model);
            }
            catch (Exception)
            {
                TempData["Error"] =
                    "Payment receipt could not be created.";

                await LoadFormDataAsync(model.ClientId);

                return View(model);
            }

            TempData["Success"] =
                "Payment receipt created successfully.";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Edit
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
        public async Task<IActionResult> Edit(
            int id,
            PaymentReceiptModel model)
        {
            if (id != model.PaymentReceiptId)
                return NotFound();

            var payment =
                await _paymentRepository.GetByIdAsync(id);

            if (payment == null || payment.IsCancelled)
                return NotFound();

            // Preserve trusted relationship and system values
            model.ClientId = payment.ClientId;
            model.InvoiceId = payment.InvoiceId;
            model.ReceiptNo = payment.ReceiptNo;
            model.IsCancelled = payment.IsCancelled;
            model.IsActive = payment.IsActive;

            NormalizeReceipt(model);

            await LoadFormDataAsync(payment.ClientId);

            if (!ModelState.IsValid)
                return View(model);

            var invoice =
                await GetSelectedInvoiceAsync(
                    payment.ClientId,
                    payment.InvoiceId);

            decimal availableBalance =
                (invoice?.BalanceAmount ?? 0) +
                payment.AmountReceived;

            if (!ValidateReceipt(
                    model,
                    invoice,
                    availableBalance))
            {
                return View(model);
            }

            payment.ReceiptDate = model.ReceiptDate;
            payment.AmountReceived = model.AmountReceived;
            payment.PaymentMode = model.PaymentMode;
            payment.BankName = model.BankName;
            payment.TransactionNo = model.TransactionNo;
            payment.ReferenceNo = model.ReferenceNo;
            payment.ReceivedBy = model.ReceivedBy;
            payment.Remarks = model.Remarks;

            bool duplicateReference =
               await _paymentRepository.TransactionReferenceExistsAsync(
                  model.TransactionNo,
                  model.ReferenceNo,
                  payment.PaymentReceiptId);

            if (duplicateReference)
            {
                ModelState.AddModelError(
                    nameof(model.TransactionNo),
                    "This transaction or reference number already exists.");

                return View(model);
            }

            try
            {
                await _paymentRepository.UpdateAsync(payment);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(
                    nameof(model.AmountReceived),
                    ex.Message);

                await LoadFormDataAsync(payment.ClientId);

                return View(model);
            }

            TempData["Success"] =
                "Payment receipt updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Details 
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

        #endregion

        #region Delete
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var payment =
                await _paymentRepository.GetByIdAsync(id);

            if (payment == null)
                return NotFound();

            if (payment.IsCancelled)
            {
                TempData["Error"] =
                    "This payment receipt is already cancelled.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            return View(payment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
          int id,
          string cancellationReason)
        {
            if (string.IsNullOrWhiteSpace(
                    cancellationReason))
            {
                TempData["Error"] =
                    "Cancellation reason is required.";

                return RedirectToAction(
                    nameof(Delete),
                    new { id });
            }

            if (cancellationReason.Trim().Length > 500)
            {
                TempData["Error"] =
                    "Cancellation reason cannot exceed 500 characters.";

                return RedirectToAction(
                    nameof(Delete),
                    new { id });
            }

            string? userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] =
                    "Current user could not be identified.";

                return RedirectToAction(
                    nameof(Delete),
                    new { id });
            }

            bool cancelled =
                await _paymentRepository.CancelAsync(
                    id,
                    userId,
                    cancellationReason);

            if (!cancelled)
            {
                TempData["Error"] =
                    "Payment receipt could not be cancelled.";

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] =
                "Payment receipt cancelled successfully.";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Helpers

        [HttpGet]
        public async Task<IActionResult> GetInvoicesByClient(
               int clientId,
               int? selectedInvoiceId)
        {
            var invoices = await _paymentRepository.GetInvoicesByClientAsync(clientId);

            var result = invoices
                .Where(x =>
                   !x.IsDeleted &&
                   x.InvoiceStatus == "Issued" &&
                   x.InvoiceStatus != "Cancelled" &&
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
                 invoices.Where(x =>
                     !x.IsDeleted &&
                     x.InvoiceStatus == "Issued" &&
                     x.BalanceAmount > 0),
                 "InvoiceId",
                 "InvoiceNo");
        }

        private async Task<InvoiceModel?> GetSelectedInvoiceAsync(
            int clientId,
            int invoiceId)
        {
            return (await _paymentRepository.GetInvoicesByClientAsync(clientId))
    .         FirstOrDefault(x =>
                 x.InvoiceId == invoiceId &&
                 !x.IsDeleted &&
                 x.InvoiceStatus == "Issued");
        }

        private static readonly string[] AllowedPaymentModes =
{
             "Cash",
             "Cheque",
             "Bank Transfer",
             "NEFT",
             "RTGS",
             "IMPS",
             "UPI"
};

        private static void NormalizeReceipt(
            PaymentReceiptModel model)
        {
            model.PaymentMode =
                model.PaymentMode?.Trim() ?? string.Empty;

            model.BankName =
                string.IsNullOrWhiteSpace(model.BankName)
                    ? null
                    : model.BankName.Trim();

            model.TransactionNo =
                string.IsNullOrWhiteSpace(model.TransactionNo)
                    ? null
                    : model.TransactionNo.Trim();

            model.ReferenceNo =
                string.IsNullOrWhiteSpace(model.ReferenceNo)
                    ? null
                    : model.ReferenceNo.Trim();

            model.ReceivedBy =
                string.IsNullOrWhiteSpace(model.ReceivedBy)
                    ? null
                    : model.ReceivedBy.Trim();

            model.Remarks =
                string.IsNullOrWhiteSpace(model.Remarks)
                    ? null
                    : model.Remarks.Trim();
        }

        private bool ValidateReceipt(PaymentReceiptModel model,
                                     InvoiceModel? invoice,
                                     decimal availableBalance)
        {
            if (invoice == null)
            {
                ModelState.AddModelError(
                    nameof(model.InvoiceId),
                    "Selected invoice was not found.");

                return false;
            }

            if (invoice.ClientId != model.ClientId)
            {
                ModelState.AddModelError(
                    nameof(model.InvoiceId),
                    "Selected invoice does not belong to this client.");
            }

            if (invoice.IsDeleted ||
               invoice.InvoiceStatus == "Cancelled" ||
               invoice.InvoiceStatus == "Draft") 
            {
                ModelState.AddModelError(
                    nameof(model.InvoiceId),
                    "Payments can only be recorded against issued invoices.");
            }

            if (invoice.PaymentStatus == "Paid" ||invoice.BalanceAmount <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.InvoiceId),
                    "This invoice is already fully paid.");
            }

            if (model.AmountReceived <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.AmountReceived),
                    "Amount received must be greater than zero.");
            }

            if (model.AmountReceived > availableBalance)
            {
                ModelState.AddModelError(
                    nameof(model.AmountReceived),
                    $"Amount cannot exceed the available balance of ₹{availableBalance:N2}.");
            }

            if (model.ReceiptDate.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.ReceiptDate),
                    "Receipt date cannot be in the future.");
            }

            if (model.ReceiptDate.Date < invoice.InvoiceDate.Date)
            {
                ModelState.AddModelError(
                    nameof(model.ReceiptDate),
                    "Receipt date cannot be before the invoice date.");
            }

            if (string.IsNullOrWhiteSpace(model.PaymentMode))
            {
                ModelState.AddModelError(
                    nameof(model.PaymentMode),
                    "Payment mode is required.");
            }

            if (!AllowedPaymentModes.Contains( model.PaymentMode,
                   StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    nameof(model.PaymentMode),
                    "Invalid payment mode.");
            }

            bool isCash =
                string.Equals(
                    model.PaymentMode,
                    "Cash",
                    StringComparison.OrdinalIgnoreCase);

            bool requiresBank =
                model.PaymentMode is
                    "Cheque" or
                    "Bank Transfer" or
                    "NEFT" or
                    "RTGS" or
                    "IMPS";

            if (requiresBank &&
                string.IsNullOrWhiteSpace(model.BankName))
            {
                ModelState.AddModelError(
                    nameof(model.BankName),
                    "Bank name is required for the selected payment mode.");
            }

            if (!isCash &&
                string.IsNullOrWhiteSpace(model.TransactionNo) &&
                string.IsNullOrWhiteSpace(model.ReferenceNo))
            {
                ModelState.AddModelError(
                    nameof(model.TransactionNo),
                    "Transaction or reference number is required for non-cash payments.");
            }

           return ModelState.IsValid;
        }

        private async Task NotifyPaymentReceivedAsync(
    PaymentReceiptModel receipt,
    InvoiceModel invoice)
        {
            var admins =
                await _userManager.GetUsersInRoleAsync("Admin");

            var financeUsers =
                await _userManager.GetUsersInRoleAsync("Finance");

            var recipients = admins
                .Concat(financeUsers)
                .Where(x => x.IsActive)
                .GroupBy(x => x.Id)
                .Select(x => x.First())
                .ToList();

            string clientName =
                invoice.Client?.ClientName ?? "Client";

            foreach (var recipient in recipients)
            {
                bool paymentNotificationExists =
                    await _notificationService.ExistsAsync(
                        recipient.Id,
                        "PaymentReceived",
                        "PaymentReceipt",
                        receipt.PaymentReceiptId);

                if (!paymentNotificationExists)
                {
                    await _notificationService.CreateAsync(
                        userId: recipient.Id,
                        title: "Payment Received",
                        message:
                            $"Payment of ₹{receipt.AmountReceived:N2} was received " +
                            $"from {clientName} against invoice {invoice.InvoiceNo}.",
                        notificationType: "PaymentReceived",
                        referenceType: "PaymentReceipt",
                        referenceId: receipt.PaymentReceiptId,
                        actionUrl:
                            $"/PaymentReceipt/Details/{receipt.PaymentReceiptId}");
                }

                if (string.Equals(
                        invoice.PaymentStatus,
                        "Paid",
                        StringComparison.OrdinalIgnoreCase))
                {
                    bool settledNotificationExists =
                        await _notificationService.ExistsAsync(
                            recipient.Id,
                            "InvoiceSettled",
                            "Invoice",
                            invoice.InvoiceId);

                    if (!settledNotificationExists)
                    {
                        await _notificationService.CreateAsync(
                            userId: recipient.Id,
                            title: "Invoice Fully Settled",
                            message:
                                $"Invoice {invoice.InvoiceNo} for {clientName} " +
                                $"has been fully paid.",
                            notificationType: "InvoiceSettled",
                            referenceType: "Invoice",
                            referenceId: invoice.InvoiceId,
                            actionUrl:
                                $"/Invoice/Details/{invoice.InvoiceId}");
                    }
                }
            }
        }

        #endregion
    }
}