using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class InvoiceController : Controller
    {
        private readonly IInvoiceRepository _invoiceRepository;

        public InvoiceController(IInvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }


        #region Index
        public async Task<IActionResult> Index(
          string? search,
          int? clientId,
          string? invoiceStatus,
          string? paymentStatus)
        {
            var allInvoices = await _invoiceRepository.GetAllAsync();

            var activeInvoices = allInvoices
                .Where(x => !x.IsDeleted)
                .ToList();

            var filteredInvoices = activeInvoices.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                filteredInvoices = filteredInvoices.Where(x =>
                    x.InvoiceNo.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase) ||
                    (x.Client?.ClientName?.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.Project?.ProjectName?.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (x.ProjectName?.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (clientId.HasValue)
            {
                filteredInvoices = filteredInvoices
                    .Where(x => x.ClientId == clientId.Value);
            }

            if (!string.IsNullOrWhiteSpace(invoiceStatus))
            {
                filteredInvoices = filteredInvoices.Where(x =>
                    string.Equals(
                        x.InvoiceStatus,
                        invoiceStatus,
                        StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                filteredInvoices = filteredInvoices.Where(x =>
                    string.Equals(
                        x.PaymentStatus,
                        paymentStatus,
                        StringComparison.OrdinalIgnoreCase));
            }

            var model = new InvoiceTrackerViewModel
            {
                Invoices = filteredInvoices
                    .OrderByDescending(x => x.InvoiceDate)
                    .ThenByDescending(x => x.InvoiceId)
                    .ToList(),

                Clients = await _invoiceRepository.GetClientsAsync(),

                TotalInvoices = activeInvoices.Count,

                DraftCount = activeInvoices.Count(x =>
                    x.InvoiceStatus == "Draft"),

                IssuedCount = activeInvoices.Count(x =>
                    x.InvoiceStatus == "Issued"),

                CancelledCount = allInvoices.Count(x =>
                    x.InvoiceStatus == "Cancelled" || x.IsDeleted),

                UnpaidCount = activeInvoices.Count(x =>
                    x.PaymentStatus == "Unpaid"),

                PartiallyPaidCount = activeInvoices.Count(x =>
                    x.PaymentStatus == "Partially Paid"),

                PaidCount = activeInvoices.Count(x =>
                    x.PaymentStatus == "Paid"),

                TotalInvoiceAmount = activeInvoices.Sum(x =>
                    x.GrandTotal),

                TotalReceivedAmount = activeInvoices.Sum(x =>
                    x.PaidAmount),

                TotalOutstandingAmount = activeInvoices.Sum(x =>
                    x.BalanceAmount)
            };

            return View(model);
        }

        #endregion

        #region Create

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new InvoiceModel
            {
                InvoiceDate = DateTime.Today,
                InvoiceNo = await _invoiceRepository.GenerateInvoiceNoAsync(),
                InvoiceStatus = "Draft",
                PaidAmount = 0
            };

            await LoadDropdownsAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InvoiceModel model)
        {
            ModelState.Remove(nameof(model.InvoiceNo));

            NormalizeInvoice(model);

            await ValidateAndAssignProjectAsync(model);

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return View(model);
            }

            await _invoiceRepository.CreateWithSequenceAsync(model);

            TempData["Success"] = "Invoice created successfully.";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Edit

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);

            if (invoice.InvoiceStatus == "Cancelled")
            {
                TempData["Error"] = "Cancelled invoices cannot be edited.";
                return RedirectToAction(nameof(Index));
            }

            if (invoice.PaymentStatus == "Paid")
            {
                TempData["Error"] = "Paid invoices cannot be edited.";
                return RedirectToAction(nameof(Index));
            }

            if (invoice == null || invoice.IsDeleted)
            {
                return NotFound();
            }

            await LoadDropdownsAsync();

            return View(invoice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InvoiceModel model)
        {
            if (id != model.InvoiceId)
                return NotFound();

            var existingInvoice = await _invoiceRepository.GetByIdAsync(id);

            if (existingInvoice.InvoiceStatus == "Cancelled")
            {
                TempData["Error"] = "Cancelled invoices cannot be edited.";
                return RedirectToAction(nameof(Index));
            }

            if (existingInvoice.PaymentStatus == "Paid")
            {
                TempData["Error"] = "Paid invoices cannot be edited.";
                return RedirectToAction(nameof(Index));
            }

            if (existingInvoice == null ||
                existingInvoice.IsDeleted)
            {
                return NotFound();
            }

            model.PaidAmount = existingInvoice.PaidAmount;

            NormalizeInvoice(model);

            await ValidateAndAssignProjectAsync(model);

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return View(model);
            }

            existingInvoice.ClientId = model.ClientId;
            existingInvoice.ProjectId = model.ProjectId;
            existingInvoice.ProjectName = model.ProjectName;

            existingInvoice.InvoiceNo = model.InvoiceNo;
            existingInvoice.InvoiceDate = model.InvoiceDate;
            existingInvoice.DueDate = model.DueDate;

            existingInvoice.BillingAddress = model.BillingAddress;
            existingInvoice.GSTNo = model.GSTNo;
            existingInvoice.IsInterState = model.IsInterState;
            existingInvoice.PaymentTerms = model.PaymentTerms;
            existingInvoice.InvoiceStatus = model.InvoiceStatus;
            existingInvoice.Remarks = model.Remarks;

            existingInvoice.Discount = model.Discount;
            existingInvoice.SubTotal = model.SubTotal;
            existingInvoice.GSTAmount = model.GSTAmount;
            existingInvoice.GrandTotal = model.GrandTotal;

            existingInvoice.InvoiceDetails.Clear();

            foreach (var detail in model.InvoiceDetails)
            {
                existingInvoice.InvoiceDetails.Add(
                    new InvoiceDetailsModel
                    {
                        ItemName = detail.ItemName,
                        Description = detail.Description,
                        Qty = detail.Qty,
                        Unit = detail.Unit,
                        Rate = detail.Rate,
                        GSTPercent = detail.GSTPercent,
                        GSTAmount = detail.GSTAmount,
                        Amount = detail.Amount,
                        SortOrder = detail.SortOrder
                    });
            }

            await _invoiceRepository.UpdateAsync(existingInvoice);
            await _invoiceRepository.SaveAsync();

            TempData["Success"] =
                "Invoice updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Details

        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);

            if (invoice == null || invoice.IsDeleted)
            {
                return NotFound();
            }

            return View(invoice);
        }

        #endregion

        #region Delete

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);

            if (invoice == null || invoice.IsDeleted)
            {
                return NotFound();
            }

            return View(invoice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);

            if (invoice == null || invoice.IsDeleted)
            {
                return NotFound();
            }

            if (invoice.PaidAmount > 0)
            {
                TempData["Error"] =
                    "Invoices with payment receipts cannot be cancelled.";

                return RedirectToAction(nameof(Index));
            }

            if (invoice.InvoiceStatus == "Cancelled")
            {
                TempData["Error"] =
                    "Invoice is already cancelled.";

                return RedirectToAction(nameof(Index));
            }

            await _invoiceRepository.DeleteAsync(invoice);
            await _invoiceRepository.SaveAsync();

            TempData["Success"] = "Invoice cancelled successfully.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Print
        public async Task<IActionResult> Print(int id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);

            if (invoice == null || invoice.IsDeleted)
            {
                return NotFound();
            }

            return View(invoice);
        }
        #endregion

        #region Helpers
        private async Task LoadDropdownsAsync()
        {
            ViewBag.Clients = await _invoiceRepository.GetClientsAsync();
            ViewBag.Projects = await _invoiceRepository.GetProjectsAsync();
        }

        private async Task<bool> ValidateAndAssignProjectAsync(
    InvoiceModel model)
        {
            if (!model.ProjectId.HasValue)
            {
                model.ProjectName = null;
                return true;
            }

            var projects =
                await _invoiceRepository.GetProjectsAsync();

            var project = projects.FirstOrDefault(x =>
                x.Id == model.ProjectId.Value);

            if (project == null)
            {
                ModelState.AddModelError(
                    nameof(model.ProjectId),
                    "Selected project does not exist or is inactive.");

                return false;
            }

            model.ProjectName = project.ProjectName;

            return true;
        }

        private static void NormalizeInvoice(InvoiceModel model)
        {
            model.InvoiceDetails = model.InvoiceDetails
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.ItemName) ||
                    x.Qty > 0 ||
                    x.Rate > 0)
                .ToList();

            decimal subTotal = 0;
            decimal gstTotal = 0;

            int sortOrder = 1;

            foreach (var item in model.InvoiceDetails)
            {
                item.SortOrder = sortOrder++;

                decimal taxableAmount = item.Qty * item.Rate;
                item.GSTAmount = taxableAmount * item.GSTPercent / 100;
                item.Amount = taxableAmount + item.GSTAmount;

                subTotal += taxableAmount;
                gstTotal += item.GSTAmount;
            }

            if (model.Discount < 0)
            {
                model.Discount = 0;
            }

            if (model.PaidAmount < 0)
            {
                model.PaidAmount = 0;
            }

            model.SubTotal = subTotal;
            model.GSTAmount = gstTotal;
            model.GrandTotal = subTotal - model.Discount + gstTotal;

            if (model.PaidAmount > model.GrandTotal)
            {
                model.PaidAmount = model.GrandTotal;
            }

            model.BalanceAmount = model.GrandTotal - model.PaidAmount;
        }

        #endregion
    }
}