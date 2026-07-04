using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class InvoiceController : Controller
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IFileStorageService _fileStorageService;

        public InvoiceController(
            IInvoiceRepository invoiceRepository,
            IFileStorageService fileStorageService)
        {
            _invoiceRepository = invoiceRepository;
            _fileStorageService = fileStorageService;
        }

        #region Index

        public async Task<IActionResult> Index(
            string? search,
            int? clientId,
            string? invoiceStatus,
            string? paymentStatus)
        {
            var allInvoices =
                await _invoiceRepository.GetAllAsync();

            var activeInvoices = allInvoices
                .Where(x => !x.IsDeleted)
                .ToList();

            var filteredInvoices =
                activeInvoices.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                filteredInvoices =
                    filteredInvoices.Where(x =>
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
                filteredInvoices =
                    filteredInvoices.Where(x =>
                        x.ClientId == clientId.Value);
            }

            if (!string.IsNullOrWhiteSpace(invoiceStatus))
            {
                filteredInvoices =
                    filteredInvoices.Where(x =>
                        string.Equals(
                            x.InvoiceStatus,
                            invoiceStatus,
                            StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                filteredInvoices =
                    filteredInvoices.Where(x =>
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

                Clients =
                    await _invoiceRepository.GetClientsAsync(),

                TotalInvoices =
                    activeInvoices.Count,

                DraftCount =
                    activeInvoices.Count(x =>
                        x.InvoiceStatus == "Draft"),

                IssuedCount =
                    activeInvoices.Count(x =>
                        x.InvoiceStatus == "Issued"),

                CancelledCount =
                    allInvoices.Count(x =>
                        x.InvoiceStatus == "Cancelled" ||
                        x.IsDeleted),

                UnpaidCount =
                    activeInvoices.Count(x =>
                        x.PaymentStatus == "Unpaid"),

                PartiallyPaidCount =
                    activeInvoices.Count(x =>
                        x.PaymentStatus == "Partially Paid"),

                PaidCount =
                    activeInvoices.Count(x =>
                        x.PaymentStatus == "Paid"),

                TotalInvoiceAmount =
                    activeInvoices.Sum(x =>
                        x.GrandTotal),

                TotalReceivedAmount =
                    activeInvoices.Sum(x =>
                        x.PaidAmount),

                TotalOutstandingAmount =
                    activeInvoices.Sum(x =>
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

                InvoiceNo =
                    await _invoiceRepository
                        .GenerateInvoiceNoAsync(),

                InvoiceStatus = "Draft",
                PaymentStatus = "Unpaid",
                PaidAmount = 0
            };

            await LoadDropdownsAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            InvoiceModel model,
            IFormFile? Attachment)
        {
            ModelState.Remove(
                nameof(model.InvoiceNo));

            NormalizeInvoice(model);

            ValidateInvoiceBusinessRules(model);

            await ValidateAndAssignProjectAsync(model);

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();

                return View(model);
            }

            FileUploadResult? uploadedFile = null;

            if (Attachment != null)
            {
                uploadedFile =
                    await _fileStorageService.UploadAsync(
                        Attachment,
                        "InvoiceDocuments");

                if (!uploadedFile.Success)
                {
                    ModelState.AddModelError(
                        nameof(Attachment),
                        uploadedFile.ErrorMessage ??
                        "Invoice attachment could not be uploaded.");

                    await LoadDropdownsAsync();

                    return View(model);
                }

                model.AttachmentPath =
                    uploadedFile.RelativePath;
            }

            try
            {
                await _invoiceRepository
                    .CreateWithSequenceAsync(model);
            }
            catch
            {
                if (uploadedFile != null)
                {
                    await _fileStorageService.DeleteAsync(
                        uploadedFile.RelativePath);
                }

                throw;
            }

            TempData["Success"] =
                "Invoice created successfully.";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Edit

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var invoice =
                await _invoiceRepository.GetByIdAsync(id);

            if (invoice == null ||
                invoice.IsDeleted)
            {
                return NotFound();
            }

            if (invoice.InvoiceStatus == "Cancelled")
            {
                TempData["Error"] =
                    "Cancelled invoices cannot be edited.";

                return RedirectToAction(nameof(Index));
            }

            if (invoice.PaymentStatus == "Paid")
            {
                TempData["Error"] =
                    "Paid invoices cannot be edited.";

                return RedirectToAction(nameof(Index));
            }

            await LoadDropdownsAsync();

            return View(invoice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            InvoiceModel model,
            IFormFile? Attachment)
        {
            if (id != model.InvoiceId)
            {
                return NotFound();
            }

            var existingInvoice =
                await _invoiceRepository.GetByIdAsync(id);

            if (existingInvoice == null ||
                existingInvoice.IsDeleted)
            {
                return NotFound();
            }

            if (existingInvoice.InvoiceStatus ==
                "Cancelled")
            {
                TempData["Error"] =
                    "Cancelled invoices cannot be edited.";

                return RedirectToAction(nameof(Index));
            }

            if (existingInvoice.PaymentStatus ==
                "Paid")
            {
                TempData["Error"] =
                    "Paid invoices cannot be edited.";

                return RedirectToAction(nameof(Index));
            }

            model.InvoiceNo =
                existingInvoice.InvoiceNo;

            model.PaidAmount =
                existingInvoice.PaidAmount;

            model.PaymentStatus =
                existingInvoice.PaymentStatus;

            model.AttachmentPath =
                existingInvoice.AttachmentPath;

            if (existingInvoice.PaidAmount > 0)
            {
                model.ClientId =
                    existingInvoice.ClientId;

                model.ProjectId =
                    existingInvoice.ProjectId;

                model.ProjectName =
                    existingInvoice.ProjectName;
            }

            NormalizeInvoice(model);

            ValidateInvoiceBusinessRules(model);

            if (!IsAllowedInvoiceStatusTransition(
                    existingInvoice.InvoiceStatus,
                    model.InvoiceStatus))
            {
                ModelState.AddModelError(
                    nameof(model.InvoiceStatus),
                    $"Invoice status cannot change from " +
                    $"{existingInvoice.InvoiceStatus} to " +
                    $"{model.InvoiceStatus}.");
            }

            await ValidateAndAssignProjectAsync(model);

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();

                return View(model);
            }

            string? oldAttachmentPath =
                existingInvoice.AttachmentPath;

            FileUploadResult? uploadedFile = null;

            if (Attachment != null)
            {
                uploadedFile =
                    await _fileStorageService.UploadAsync(
                        Attachment,
                        "InvoiceDocuments");

                if (!uploadedFile.Success)
                {
                    ModelState.AddModelError(
                        nameof(Attachment),
                        uploadedFile.ErrorMessage ??
                        "Invoice attachment could not be uploaded.");

                    await LoadDropdownsAsync();

                    return View(model);
                }

                existingInvoice.AttachmentPath =
                    uploadedFile.RelativePath;
            }

            existingInvoice.ClientId =
                model.ClientId;

            existingInvoice.ProjectId =
                model.ProjectId;

            existingInvoice.ProjectName =
                model.ProjectName;

            existingInvoice.InvoiceDate =
                model.InvoiceDate;

            existingInvoice.DueDate =
                model.DueDate;

            existingInvoice.BillingAddress =
                model.BillingAddress;

            existingInvoice.GSTNo =
                model.GSTNo;

            existingInvoice.IsInterState =
                model.IsInterState;

            existingInvoice.PaymentTerms =
                model.PaymentTerms;

            existingInvoice.InvoiceStatus =
                model.InvoiceStatus;

            existingInvoice.Remarks =
                model.Remarks;

            existingInvoice.Discount =
                model.Discount;

            existingInvoice.SubTotal =
                model.SubTotal;

            existingInvoice.GSTAmount =
                model.GSTAmount;

            existingInvoice.GrandTotal =
                model.GrandTotal;

            existingInvoice.BalanceAmount =
                Math.Max(
                    0,
                    model.GrandTotal -
                    existingInvoice.PaidAmount);

            existingInvoice.InvoiceDetails.Clear();

            foreach (var detail in model.InvoiceDetails)
            {
                existingInvoice.InvoiceDetails.Add(
                    new InvoiceDetailsModel
                    {
                        ItemName =
                            detail.ItemName,

                        Description =
                            detail.Description,

                        Qty =
                            detail.Qty,

                        Unit =
                            detail.Unit,

                        Rate =
                            detail.Rate,

                        GSTPercent =
                            detail.GSTPercent,

                        GSTAmount =
                            detail.GSTAmount,

                        Amount =
                            detail.Amount,

                        SortOrder =
                            detail.SortOrder
                    });
            }

            try
            {
                await _invoiceRepository
                    .UpdateAsync(existingInvoice);

                await _invoiceRepository
                    .SaveAsync();
            }
            catch
            {
                if (uploadedFile != null)
                {
                    await _fileStorageService.DeleteAsync(
                        uploadedFile.RelativePath);
                }

                throw;
            }

            if (uploadedFile != null &&
                !string.IsNullOrWhiteSpace(
                    oldAttachmentPath))
            {
                await _fileStorageService.DeleteAsync(
                    oldAttachmentPath);
            }

            TempData["Success"] =
                "Invoice updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Details

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var invoice =
                await _invoiceRepository.GetByIdAsync(id);

            if (invoice == null ||
                invoice.IsDeleted)
            {
                return NotFound();
            }

            return View(invoice);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAttachment(
            int id)
        {
            var invoice =
                await _invoiceRepository.GetByIdAsync(id);

            if (invoice == null ||
                invoice.IsDeleted ||
                string.IsNullOrWhiteSpace(
                    invoice.AttachmentPath))
            {
                return NotFound();
            }

            byte[]? fileBytes =
                await _fileStorageService.DownloadAsync(
                    invoice.AttachmentPath);

            if (fileBytes == null)
            {
                return NotFound();
            }

            string fileName =
                Path.GetFileName(
                    invoice.AttachmentPath);

            string extension =
                Path.GetExtension(fileName)
                    .ToLowerInvariant();

            string contentType = extension switch
            {
                ".pdf" =>
                    "application/pdf",

                ".doc" =>
                    "application/msword",

                ".docx" =>
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",

                ".xls" =>
                    "application/vnd.ms-excel",

                ".xlsx" =>
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                ".jpg" or ".jpeg" =>
                    "image/jpeg",

                ".png" =>
                    "image/png",

                _ =>
                    "application/octet-stream"
            };

            return File(
                fileBytes,
                contentType,
                fileName);
        }

        #endregion

        #region Delete

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var invoice =
                await _invoiceRepository.GetByIdAsync(id);

            if (invoice == null ||
                invoice.IsDeleted)
            {
                return NotFound();
            }

            return View(invoice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var invoice =
                await _invoiceRepository.GetByIdAsync(id);

            if (invoice == null ||
                invoice.IsDeleted)
            {
                return NotFound();
            }

            if (invoice.PaidAmount > 0)
            {
                TempData["Error"] =
                    "Invoices with payment receipts cannot be cancelled.";

                return RedirectToAction(nameof(Index));
            }

            if (invoice.InvoiceStatus ==
                "Cancelled")
            {
                TempData["Error"] =
                    "Invoice is already cancelled.";

                return RedirectToAction(nameof(Index));
            }

            await _invoiceRepository
                .DeleteAsync(invoice);

            await _invoiceRepository
                .SaveAsync();

            TempData["Success"] =
                "Invoice cancelled successfully.";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Print

        [HttpGet]
        public async Task<IActionResult> Print(int id)
        {
            var invoice =
                await _invoiceRepository.GetByIdAsync(id);

            if (invoice == null ||
                invoice.IsDeleted)
            {
                return NotFound();
            }

            return View(invoice);
        }

        #endregion

        #region Helpers

        private static bool IsAllowedInvoiceStatusTransition(
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
                "Draft" =>
                    newStatus is
                        "Issued" or
                        "Cancelled",

                "Issued" =>
                    newStatus is
                        "Cancelled",

                "Cancelled" =>
                    false,

                _ =>
                    false
            };
        }

        private void ValidateInvoiceBusinessRules(
            InvoiceModel model)
        {
            if (model.DueDate.HasValue &&
                model.DueDate.Value.Date <
                model.InvoiceDate.Date)
            {
                ModelState.AddModelError(
                    nameof(model.DueDate),
                    "Due date cannot be before invoice date.");
            }

            if (model.InvoiceDetails == null ||
                model.InvoiceDetails.Count == 0)
            {
                ModelState.AddModelError(
                    nameof(model.InvoiceDetails),
                    "At least one valid invoice item is required.");

                return;
            }

            for (int index = 0;
                 index < model.InvoiceDetails.Count;
                 index++)
            {
                // Convert ICollection to IList for indexing
                var detail = model.InvoiceDetails.ElementAt(index);

                if (string.IsNullOrWhiteSpace(detail.ItemName))
                {
                    ModelState.AddModelError(
                        $"InvoiceDetails[{index}].ItemName",
                        "Item name is required.");
                }

                if (detail.Qty <= 0)
                {
                    ModelState.AddModelError(
                        $"InvoiceDetails[{index}].Qty",
                        "Quantity must be greater than zero.");
                }

                if (detail.Rate < 0)
                {
                    ModelState.AddModelError(
                        $"InvoiceDetails[{index}].Rate",
                        "Rate cannot be negative.");
                }

                if (detail.GSTPercent < 0 ||
                    detail.GSTPercent > 100)
                {
                    ModelState.AddModelError(
                        $"InvoiceDetails[{index}].GSTPercent",
                        "GST percentage must be between 0 and 100.");
                }
            }

            if (model.Discount < 0)
            {
                ModelState.AddModelError(
                    nameof(model.Discount),
                    "Discount cannot be negative.");
            }

            if (model.Discount > model.SubTotal)
            {
                ModelState.AddModelError(
                    nameof(model.Discount),
                    "Discount cannot exceed the subtotal.");
            }

            if (model.InvoiceStatus != "Draft" &&
                model.InvoiceStatus != "Issued")
            {
                ModelState.AddModelError(
                    nameof(model.InvoiceStatus),
                    "Invoice status must be Draft or Issued.");
            }
        }

        private async Task LoadDropdownsAsync()
        {
            ViewBag.Clients =
                await _invoiceRepository
                    .GetClientsAsync();

            ViewBag.Projects =
                await _invoiceRepository
                    .GetProjectsAsync();
        }

        private async Task<bool>
            ValidateAndAssignProjectAsync(
                InvoiceModel model)
        {
            if (!model.ProjectId.HasValue)
            {
                model.ProjectName = null;

                return true;
            }

            var projects =
                await _invoiceRepository
                    .GetProjectsAsync();

            var project =
                projects.FirstOrDefault(x =>
                    x.Id == model.ProjectId.Value);

            if (project == null)
            {
                ModelState.AddModelError(
                    nameof(model.ProjectId),
                    "Selected project does not exist or is inactive.");

                return false;
            }

            model.ProjectName =
                project.ProjectName;

            return true;
        }

        private static void NormalizeInvoice(
            InvoiceModel model)
        {
            model.InvoiceDetails ??=
                new List<InvoiceDetailsModel>();

            model.InvoiceDetails =
                model.InvoiceDetails
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x.ItemName) ||
                        x.Qty > 0 ||
                        x.Rate > 0)
                    .ToList();

            decimal subTotal = 0;
            decimal gstTotal = 0;
            int sortOrder = 1;

            foreach (var item in
                     model.InvoiceDetails)
            {
                item.ItemName =
                    item.ItemName?.Trim();

                item.Description =
                    item.Description?.Trim();

                item.Unit =
                    item.Unit?.Trim();

                item.SortOrder =
                    sortOrder++;

                decimal taxableAmount =
                    item.Qty * item.Rate;

                item.GSTAmount =
                    taxableAmount *
                    item.GSTPercent / 100;

                item.Amount =
                    taxableAmount +
                    item.GSTAmount;

                subTotal +=
                    taxableAmount;

                gstTotal +=
                    item.GSTAmount;
            }

            if (model.Discount < 0)
            {
                model.Discount = 0;
            }

            if (model.PaidAmount < 0)
            {
                model.PaidAmount = 0;
            }

            model.SubTotal =
                subTotal;

            model.GSTAmount =
                gstTotal;

            model.GrandTotal =
                subTotal -
                model.Discount +
                gstTotal;

            if (model.PaidAmount >
                model.GrandTotal)
            {
                model.PaidAmount =
                    model.GrandTotal;
            }

            model.BalanceAmount =
                Math.Max(
                    0,
                    model.GrandTotal -
                    model.PaidAmount);
        }

        #endregion
    }
}