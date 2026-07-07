using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using AryamanBMS.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DocumentFormat.OpenXml.Spreadsheet;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class InvoiceController : Controller
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IInvoiceDocumentService _invoiceDocumentService;

        private readonly ApplicationDbContext _context;

        public InvoiceController(
             IInvoiceRepository invoiceRepository,
             IFileStorageService fileStorageService,
             IInvoiceDocumentService invoiceDocumentService,
             ApplicationDbContext context)
        {
            _invoiceRepository = invoiceRepository;
            _fileStorageService = fileStorageService;
            _invoiceDocumentService = invoiceDocumentService;
            _context = context;
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

                InvoiceNo = await _invoiceRepository.GenerateInvoiceNoAsync(),

                InvoiceType = "Tax Invoice",

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

            await ValidatePurchaseOrderAsync(model);

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

            model.InvoiceNo = existingInvoice.InvoiceNo;

            model.PaidAmount =  existingInvoice.PaidAmount;

            model.PaymentStatus = existingInvoice.PaymentStatus;

            model.AttachmentPath =  existingInvoice.AttachmentPath;

            if (existingInvoice.PaidAmount > 0)
            {
                model.ClientId =  existingInvoice.ClientId;

                model.ProjectId = existingInvoice.ProjectId;

                model.ProjectName =  existingInvoice.ProjectName;
            }

            NormalizeInvoice(model);

            ValidateInvoiceBusinessRules(model);

            if (!IsAllowedInvoiceStatusTransition(
                    existingInvoice.InvoiceStatus,
                    model.InvoiceStatus))
            {
                ModelState.AddModelError(nameof(model.InvoiceStatus),
                    $"Invoice status cannot change from " +
                    $"{existingInvoice.InvoiceStatus} to " +
                    $"{model.InvoiceStatus}.");
            }

            await ValidateAndAssignProjectAsync(model);
            await ValidatePurchaseOrderAsync(model);

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

            existingInvoice.ClientId =   model.ClientId;
            existingInvoice.ProjectId =  model.ProjectId;
            existingInvoice.ProjectName =  model.ProjectName;
            existingInvoice.InvoiceType =  model.InvoiceType;
            existingInvoice.InvoiceDate =  model.InvoiceDate;
            existingInvoice.PurchaseWorkOrderId = model.PurchaseWorkOrderId;
            existingInvoice.ProposalId = model.ProposalId;
            existingInvoice.SACCode = model.SACCode;
            existingInvoice.DueDate =  model.DueDate;
            existingInvoice.BillingAddress =  model.BillingAddress;
            existingInvoice.GSTNo = model.GSTNo;
            existingInvoice.IsInterState = model.IsInterState;
            existingInvoice.PaymentTerms = model.PaymentTerms;
            existingInvoice.InvoiceStatus = model.InvoiceStatus;
            existingInvoice.Remarks =  model.Remarks;
            existingInvoice.Discount =  model.Discount;
            existingInvoice.SubTotal =  model.SubTotal;
            existingInvoice.GSTAmount = model.GSTAmount;
            existingInvoice.GrandTotal = model.GrandTotal;
            existingInvoice.BalanceAmount =
                Math.Max(0, model.GrandTotal - existingInvoice.PaidAmount);

            existingInvoice.InvoiceDetails.Clear();

            foreach (var detail in model.InvoiceDetails)
            {
                existingInvoice.InvoiceDetails.Add(
                    new InvoiceDetailsModel
                    {
                        ItemName = detail.ItemName,

                        Description = detail.Description,

                        Qty =  detail.Qty,

                        Unit = detail.Unit,

                        Rate =   detail.Rate,

                        GSTPercent = detail.GSTPercent,

                        GSTAmount =  detail.GSTAmount,

                        Amount = detail.Amount,

                        SortOrder = detail.SortOrder
                    });
            }

            try
            {
                await _invoiceRepository.UpdateAsync(existingInvoice);

                await _invoiceRepository.SaveAsync();
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

            if (uploadedFile != null && !string.IsNullOrWhiteSpace(
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

            ViewBag.CurrentDocx =
                await _context
                    .InvoiceDocumentVersions
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.InvoiceId == id &&
                        x.DocumentFormat == "DOCX" &&
                        x.IsCurrent);

            ViewBag.CurrentPdf =
                await _context
                    .InvoiceDocumentVersions
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.InvoiceId == id &&
                        x.DocumentFormat == "PDF" &&
                        x.IsCurrent);

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

        #region Generated Documents

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateDocuments(
            int id)
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
                    "Cancelled invoices cannot generate documents.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            string? userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            try
            {
                await _invoiceDocumentService
                    .GenerateAsync(
                        id,
                        userId);

                TempData["Success"] =
                    "Invoice DOCX and PDF generated successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    ex.Message;
            }

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadCurrentDocument(
            int id,
            string format)
        {
            format =
                format?.Trim().ToUpperInvariant()
                ?? string.Empty;

            if (format != "DOCX" &&
                format != "PDF")
            {
                return BadRequest();
            }

            var document =
                await _context
                    .InvoiceDocumentVersions
                    .AsNoTracking()
                    .Where(x =>
                        x.InvoiceId == id &&
                        x.DocumentFormat == format &&
                        x.IsCurrent)
                    .OrderByDescending(x =>
                        x.VersionNumber)
                    .FirstOrDefaultAsync();

            if (document == null)
            {
                TempData["Error"] =
                    $"No generated {format} document is available.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            byte[]? fileBytes =
                await _fileStorageService
                    .DownloadAsync(
                        document.StoredFilePath);

            if (fileBytes == null)
            {
                return NotFound();
            }

            return File(
                fileBytes,
                document.ContentType,
                document.OriginalFileName);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocumentVersion(
            int id)
        {
            var document =
                await _context
                    .InvoiceDocumentVersions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.InvoiceDocumentVersionId == id);

            if (document == null)
            {
                return NotFound();
            }

            byte[]? fileBytes =
                await _fileStorageService
                    .DownloadAsync(
                        document.StoredFilePath);

            if (fileBytes == null)
            {
                return NotFound();
            }

            return File(
                fileBytes,
                document.ContentType,
                document.OriginalFileName);
        }

        [HttpGet]
        public async Task<IActionResult> DocumentHistory(
            int id)
        {
            var invoice =
                await _invoiceRepository.GetByIdAsync(id);

            if (invoice == null ||
                invoice.IsDeleted)
            {
                return NotFound();
            }

            ViewBag.Invoice =  invoice;

            var documents =
                await _context
                    .InvoiceDocumentVersions
                    .AsNoTracking()
                    .Where(x =>
                        x.InvoiceId == id)
                    .OrderByDescending(x =>
                        x.VersionNumber)
                    .ThenBy(x =>
                        x.DocumentFormat)
                    .ToListAsync();

            return View(documents);
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

            var pdfDocument =
                await _context
                    .InvoiceDocumentVersions
                    .AsNoTracking()
                    .Where(x =>
                        x.InvoiceId == id &&
                        x.DocumentFormat == "PDF" &&
                        x.IsCurrent)
                    .OrderByDescending(x =>
                        x.VersionNumber)
                    .FirstOrDefaultAsync();

            if (pdfDocument == null)
            {
                TempData["Error"] =
                    "Generate the invoice documents before printing.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            byte[]? fileBytes =
                await _fileStorageService.DownloadAsync(
                    pdfDocument.StoredFilePath);

            if (fileBytes == null)
            {
                TempData["Error"] =
                    "The generated PDF file could not be found.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            return File(
                fileBytes,
                "application/pdf");
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

        private void ValidateInvoiceBusinessRules(InvoiceModel model)
        {

            if (model.InvoiceType != "Tax Invoice" &&
                model.InvoiceType != "Proforma Invoice")
            {
                ModelState.AddModelError(
                    nameof(model.InvoiceType),
                    "Invoice type must be Tax Invoice or Proforma Invoice.");
            }

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

            ViewBag.PurchaseOrders =
              await _context.PurchaseOrders
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.OrderDate)
            .ToListAsync();
        }

        private async Task<bool> ValidateAndAssignProjectAsync(
                InvoiceModel model)
        {
            if (!model.ProjectId.HasValue)
            {
                model.ProjectName = null;

                return true;
            }

            var projects =await _invoiceRepository.GetProjectsAsync();

            var project =
                projects.FirstOrDefault(x =>x.Id == model.ProjectId.Value);

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
            model.InvoiceType =
                string.Equals(
                    model.InvoiceType,
                    "Proforma Invoice",
                    StringComparison.OrdinalIgnoreCase)
                    ? "Proforma Invoice"
                    : "Tax Invoice";

            bool isProforma =
                model.InvoiceType ==
                "Proforma Invoice";

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

            foreach (var item in model.InvoiceDetails)
            {
                item.ItemName = item.ItemName?.Trim() ??string.Empty;

                item.Description =  item.Description?.Trim();

                item.Unit = item.Unit?.Trim() ?? string.Empty;

                item.SortOrder = sortOrder++;

                item.Qty = Math.Round(item.Qty, 2);

                item.Rate = Math.Round(item.Rate, 2);

                decimal taxableAmount =
                    Math.Round(
                        item.Qty * item.Rate,
                        2);

                if (isProforma)
                {
                    item.GSTPercent = 0;
                    item.GSTAmount = 0;
                    model.SACCode = null;
                    model.IsInterState = false;
                }
                else
                {
                    item.GSTPercent =
                        Math.Clamp(
                            item.GSTPercent,
                            0,
                            100);

                    item.GSTAmount =
                        Math.Round(
                            taxableAmount *
                            item.GSTPercent / 100m,
                            2);
                }

                /*
                 * Amount stores the taxable line amount.
                 * GST remains separate.
                 */
                item.Amount =  taxableAmount;

                subTotal +=  taxableAmount;

                gstTotal +=  item.GSTAmount;
            }

            model.Discount =  Math.Max(0,Math.Round(
                        model.Discount,
                        2));

            if (model.Discount > subTotal)
            {
                model.Discount = subTotal;
            }

            model.PaidAmount = Math.Max(0,Math.Round(
                        model.PaidAmount,
                        2));

            model.SubTotal = Math.Round(subTotal,2);

            model.GSTAmount = isProforma
                    ? 0
                    : Math.Round(
                        gstTotal,
                        2);

            decimal taxableTotal = model.SubTotal -  model.Discount;

            model.GrandTotal =
                Math.Round(
                    taxableTotal +
                    model.GSTAmount,
                    2);

            if (model.PaidAmount >
                model.GrandTotal)
            {
                model.PaidAmount = model.GrandTotal;
            }

            model.BalanceAmount =
                Math.Max(
                    0,
                    Math.Round( model.GrandTotal - model.PaidAmount,2));
        }

        [HttpGet]
        public async Task<IActionResult> GetPurchaseOrderDetails(
    int id)
        {
            var order =
                await _context.PurchaseOrders
                    .AsNoTracking()
                    .Include(x => x.Client)
                    .Include(x => x.Proposal)
                    .FirstOrDefaultAsync(x =>
                        x.PurchaseOrderId == id &&
                        x.IsActive);

            if (order == null)
            {
                return NotFound();
            }

            return Json(new
            {
                order.PurchaseOrderId,
                order.OrderNumber,
                order.OrderTitle,
                order.ClientId,
                order.ProposalId,
                order.OrderAmount,
                order.DeliveryDueDate,
                order.VendorReference,
                order.Remarks
            });
        }
        private async Task ValidatePurchaseOrderAsync(
    InvoiceModel model)
        {
            if (!model.PurchaseWorkOrderId.HasValue)
            {
                return;
            }

            var order =
                await _context.PurchaseOrders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.PurchaseOrderId ==
                            model.PurchaseWorkOrderId.Value &&
                        x.IsActive);

            if (order == null)
            {
                ModelState.AddModelError(
                    nameof(model.PurchaseWorkOrderId),
                    "Selected Purchase Order is invalid.");
                return;
            }

            if (order.ClientId != model.ClientId)
            {
                ModelState.AddModelError(
                    nameof(model.PurchaseWorkOrderId),
                    "Purchase Order does not belong to the selected client.");
            }

            model.ProposalId = order.ProposalId;
        }

        #endregion
    }
}