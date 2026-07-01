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

        public async Task<IActionResult> Index()
        {
            var model = new InvoiceTrackerViewModel
            {
                Invoices = await _invoiceRepository.GetAllAsync(),
                Clients = await _invoiceRepository.GetClientsAsync()
            };

            return View(model);
        }

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
            NormalizeInvoice(model);

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return View(model);
            }

            await _invoiceRepository.AddAsync(model);
            await _invoiceRepository.SaveAsync();

            TempData["Success"] = "Invoice created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);

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
            {
                return NotFound();
            }

            NormalizeInvoice(model);

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return View(model);
            }

            await _invoiceRepository.UpdateAsync(model);
            await _invoiceRepository.SaveAsync();

            TempData["Success"] = "Invoice updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);

            if (invoice == null || invoice.IsDeleted)
            {
                return NotFound();
            }

            return View(invoice);
        }

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

            await _invoiceRepository.DeleteAsync(invoice);
            await _invoiceRepository.SaveAsync();

            TempData["Success"] = "Invoice cancelled successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Print(int id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);

            if (invoice == null || invoice.IsDeleted)
            {
                return NotFound();
            }

            return View(invoice);
        }

        private async Task LoadDropdownsAsync()
        {
            ViewBag.Clients = await _invoiceRepository.GetClientsAsync();
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
    }
}