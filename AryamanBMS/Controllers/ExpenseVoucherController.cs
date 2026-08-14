using System.Text;
using System.Text.RegularExpressions;
using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Employee,Master")]
    public class ExpenseVoucherController : Controller
    {
        private readonly IExpenseVoucherRepository _voucherRepository;
        private readonly IExpenseCategoryRepository _categoryRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ExpenseVoucherController> _logger;
        private readonly ILocationRepository _locationRepository;

        private const string DocumentFolder = "ExpenseVoucherDocuments";

        public ExpenseVoucherController(
           IExpenseVoucherRepository voucherRepository,
           IExpenseCategoryRepository categoryRepository,
           IFileStorageService fileStorageService,
           UserManager<ApplicationUserModel> userManager,
           ApplicationDbContext context,
           INotificationService notificationService,
           ILocationRepository locationRepository,
           ILogger<ExpenseVoucherController> logger)
        {
            _voucherRepository = voucherRepository;
            _categoryRepository = categoryRepository;
            _fileStorageService = fileStorageService;
            _userManager = userManager;
            _context = context;
            _notificationService = notificationService;
            _locationRepository = locationRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Index(
    string? status,
    int? categoryId,
    string? search,
    DateTime? fromDate,
    DateTime? toDate,
    string sortBy = "VoucherDate",
    string sortOrder = "desc",
    int page = 1,
    bool mine = false)
        {
            var vouchers = await _voucherRepository.GetAllAsync();

            if (mine || !IsFinanceUser())
            {
                string userId = GetCurrentUserId();

                vouchers = vouchers
                    .Where(x => x.CreatedByUserId == userId)
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                vouchers = vouchers
                    .Where(x => x.Status == status)
                    .ToList();
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                vouchers = vouchers
                    .Where(x => x.ExpenseCategoryId == categoryId.Value)
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim().ToLower();

                vouchers = vouchers
                    .Where(x =>
                        (!string.IsNullOrWhiteSpace(x.VoucherNumber) &&
                            x.VoucherNumber.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.VendorName) &&
                            x.VendorName.ToLower().Contains(keyword)) ||
                        (x.Vendor != null &&
                            !string.IsNullOrWhiteSpace(x.Vendor.VendorName) &&
                            x.Vendor.VendorName.ToLower().Contains(keyword)) ||
                        (x.Category != null &&
                            !string.IsNullOrWhiteSpace(x.Category.CategoryName) &&
                            x.Category.CategoryName.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.InvoiceNumber) &&
                            x.InvoiceNumber.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.Description) &&
                            x.Description.ToLower().Contains(keyword)))
                    .ToList();
            }

            if (fromDate.HasValue)
            {
                vouchers = vouchers
                    .Where(x => x.VoucherDate.Date >= fromDate.Value.Date)
                    .ToList();
            }

            if (toDate.HasValue)
            {
                vouchers = vouchers
                    .Where(x => x.VoucherDate.Date <= toDate.Value.Date)
                    .ToList();
            }

            bool desc =
                string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            vouchers = sortBy switch
            {
                "VoucherNo" => desc
                    ? vouchers.OrderByDescending(x => x.VoucherNumber).ToList()
                    : vouchers.OrderBy(x => x.VoucherNumber).ToList(),

                "Category" => desc
                    ? vouchers.OrderByDescending(x => x.Category?.CategoryName).ToList()
                    : vouchers.OrderBy(x => x.Category?.CategoryName).ToList(),

                "Vendor" => desc
                    ? vouchers.OrderByDescending(x => x.Vendor?.VendorName ?? x.VendorName).ToList()
                    : vouchers.OrderBy(x => x.Vendor?.VendorName ?? x.VendorName).ToList(),

                "Amount" => desc
                    ? vouchers.OrderByDescending(x => x.Amount).ToList()
                    : vouchers.OrderBy(x => x.Amount).ToList(),

                "TotalAmount" => desc
                    ? vouchers.OrderByDescending(x => x.TotalAmount).ToList()
                    : vouchers.OrderBy(x => x.TotalAmount).ToList(),

                "Status" => desc
                    ? vouchers.OrderByDescending(x => x.Status).ToList()
                    : vouchers.OrderBy(x => x.Status).ToList(),

                "PaymentStatus" => desc
                    ? vouchers.OrderByDescending(x => x.PaymentStatus).ToList()
                    : vouchers.OrderBy(x => x.PaymentStatus).ToList(),

                _ => desc
                    ? vouchers.OrderByDescending(x => x.VoucherDate).ToList()
                    : vouchers.OrderBy(x => x.VoucherDate).ToList()
            };

            const int pageSize = 20;
            int totalRecords = vouchers.Count();

            vouchers = vouchers
                .Skip((Math.Max(page, 1) - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Status = status;
            ViewBag.CategoryId = categoryId;
            ViewBag.Search = search;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            ViewBag.Mine = mine;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages =
                (int)Math.Ceiling(totalRecords / (double)pageSize);

            await LoadLookups();

            return View(vouchers);
        }

        public async Task<IActionResult> ExportExcel (
            string? status,
            int? categoryId,
            string? search,
            DateTime? fromDate,
            DateTime? toDate,
            bool mine = false)
        {
            var vouchers = await _voucherRepository.GetAllAsync();

            if (mine || !IsFinanceUser())
            {
                string userId = GetCurrentUserId();
                vouchers = vouchers.Where(x => x.CreatedByUserId == userId).ToList();
            }

            if (!string.IsNullOrWhiteSpace(status))
                vouchers = vouchers.Where(x => x.Status == status).ToList();

            if (categoryId.HasValue && categoryId.Value > 0)
                vouchers = vouchers.Where(x => x.ExpenseCategoryId == categoryId.Value).ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim().ToLower();
                vouchers = vouchers.Where(x =>
                    (!string.IsNullOrWhiteSpace(x.VoucherNumber) && x.VoucherNumber.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(x.VendorName) && x.VendorName.ToLower().Contains(keyword)) ||
                    (x.Vendor != null && !string.IsNullOrWhiteSpace(x.Vendor.VendorName) && x.Vendor.VendorName.ToLower().Contains(keyword)) ||
                    (x.Category != null && !string.IsNullOrWhiteSpace(x.Category.CategoryName) && x.Category.CategoryName.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(x.InvoiceNumber) && x.InvoiceNumber.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(x.Description) && x.Description.ToLower().Contains(keyword)))
                    .ToList();
            }

            if (fromDate.HasValue)
                vouchers = vouchers.Where(x => x.VoucherDate.Date >= fromDate.Value.Date).ToList();

            if (toDate.HasValue)
                vouchers = vouchers.Where(x => x.VoucherDate.Date <= toDate.Value.Date).ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Expense Vouchers");

            worksheet.Cell(1, 1).Value = "Voucher No";
            worksheet.Cell(1, 2).Value = "Date";
            worksheet.Cell(1, 3).Value = "Category";
            worksheet.Cell(1, 4).Value = "Vendor";
            worksheet.Cell(1, 5).Value = "Invoice";
            worksheet.Cell(1, 6).Value = "Amount";
            worksheet.Cell(1, 7).Value = "Total Amount";
            worksheet.Cell(1, 8).Value = "Status";
            worksheet.Cell(1, 9).Value = "Payment Status";

            int row = 2;

            foreach (var item in vouchers.OrderByDescending(x => x.VoucherDate))
            {
                worksheet.Cell(row, 1).Value = item.VoucherNumber;
                worksheet.Cell(row, 2).Value = item.VoucherDate;
                worksheet.Cell(row, 2).Style.DateFormat.Format = "dd-MMM-yyyy";
                worksheet.Cell(row, 3).Value = item.Category?.CategoryName;
                worksheet.Cell(row, 4).Value = item.Vendor?.VendorName ?? item.VendorName;
                worksheet.Cell(row, 5).Value = item.InvoiceNumber;
                worksheet.Cell(row, 6).Value = item.Amount;
                worksheet.Cell(row, 7).Value = item.TotalAmount;
                worksheet.Cell(row, 8).Value = item.Status;
                worksheet.Cell(row, 9).Value = item.PaymentStatus;

                row++;
            }

            var headerRange = worksheet.Range("A1:I1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"expense-vouchers-{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }

        public IActionResult Pending()
        {
            return RedirectToAction(nameof(Index), new
            {
                status = FinancialConstants.ExpenseVoucherStatus.Submitted
            });
        }

        public async Task<IActionResult> Create(bool mine = false)
        {
            var model = new ExpenseVoucherModel
            {
                VoucherDate = DateTime.Now,
                FinancialYear = GetCurrentFinancialYear()
            };

            await LoadLookups();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ExpenseVoucherModel model,
            bool mine = false)
        {
            ModelState.Remove(
                nameof(model.VoucherNumber));

            ModelState.Remove(
                nameof(model.CreatedByUserId));

            NormalizeVoucher(model);

            var category =
                await _categoryRepository.GetByIdAsync(
                    model.ExpenseCategoryId);

            if (category == null)
            {
                ModelState.AddModelError(
                    nameof(model.ExpenseCategoryId),
                    "Selected category does not exist.");
            }
            else if (model.GSTRate == 0 &&
                     category.DefaultGSTRate > 0)
            {
                model.GSTRate =
                    category.DefaultGSTRate;
            }

            ValidateVoucherBusinessRules(model);
            await ValidateGstPeriodOpen(model.VoucherDate);

            if (!string.IsNullOrWhiteSpace(
                    model.InvoiceNumber))
            {
                bool duplicateExists =
                    await _voucherRepository
                        .VendorInvoiceExistsAsync(
                            model.VendorId,
                            model.VendorGSTIN,
                            model.InvoiceNumber,
                            model.FinancialYear);

                if (duplicateExists)
                {
                    ModelState.AddModelError(
                        nameof(model.InvoiceNumber),
                        "This vendor invoice number already exists for the vendor.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadLookups();
                return View(model);
            }

            CalculateGSTAmounts(model);

            model.CreatedByUserId =
                GetCurrentUserId();

            model.Status =
                FinancialConstants
                    .ExpenseVoucherStatus
                    .Draft;

            model.FinancialYear =
                GetCurrentFinancialYear();

            await ApplyVendorDefaults(model);
            ApplyCategoryDefaults(model, category);
            RefreshPaymentFields(model);

            await _voucherRepository
                .CreateWithSequenceAsync(model);

            TempData["Success"] =
                $"Expense Voucher '{model.VoucherNumber}' created successfully.";

            return RedirectToAction(nameof(Index), new
            {
                mine
            });
        }

        #region Edit
        public async Task<IActionResult> Edit(int id)
        {
            var voucher = await _voucherRepository.GetByIdAsync(id);
            if (voucher == null)
                return NotFound();

            // Only allow editing of draft vouchers
            if (!CanModifyDraftVoucher(voucher))
            {
                TempData["Error"] = "Only draft expense vouchers can be edited.";
                return RedirectToAction(nameof(Index));
            }

            await LoadLookups();
            return View(voucher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
    ExpenseVoucherModel model)
        {
            var existing =
                await _voucherRepository.GetByIdAsync(
                    model.ExpenseVoucherId);

            if (existing == null)
                return NotFound();

            if (!CanModifyDraftVoucher(existing))
            {
                TempData["Error"] =
                    "Only accessible draft expense vouchers can be edited.";

                return RedirectToAction(nameof(Index));
            }

            NormalizeVoucher(model);

            var category =
                await _categoryRepository.GetByIdAsync(
                    model.ExpenseCategoryId);

            if (category == null)
            {
                ModelState.AddModelError(
                    nameof(model.ExpenseCategoryId),
                    "Selected category does not exist.");
            }
            else if (model.GSTRate == 0 &&
                     category.DefaultGSTRate > 0)
            {
                model.GSTRate =
                    category.DefaultGSTRate;
            }

            ValidateVoucherBusinessRules(model);
            await ValidateGstPeriodOpen(model.VoucherDate);

            if (!string.IsNullOrWhiteSpace(
                    model.InvoiceNumber))
            {
                bool duplicateExists =
                    await _voucherRepository
                        .VendorInvoiceExistsAsync(
                            model.VendorId,
                            model.VendorGSTIN,
                            model.InvoiceNumber,
                            existing.FinancialYear,
                            existing.ExpenseVoucherId);

                if (duplicateExists)
                {
                    ModelState.AddModelError(
                        nameof(model.InvoiceNumber),
                        "This vendor invoice number already exists for the vendor.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadLookups();
                return View(model);
            }

            existing.ExpenseCategoryId =     model.ExpenseCategoryId;

            existing.VoucherDate =  model.VoucherDate;

            existing.Description =  model.Description;
            existing.BusinessPurpose = model.BusinessPurpose;
            existing.BeneficiaryName = model.BeneficiaryName;
            existing.SupportingReference = model.SupportingReference;

            existing.Amount = model.Amount;

            existing.GSTRate =  model.GSTRate;

            existing.IsInterState =model.IsInterState;
            existing.CompanyStateCode = model.CompanyStateCode;
            existing.VendorStateCode = model.VendorStateCode;
            existing.PlaceOfSupplyStateCode = model.PlaceOfSupplyStateCode;
            existing.IsGstStateOverride = model.IsGstStateOverride;
            existing.GstStateOverrideReason = model.GstStateOverrideReason;

            existing.VendorId = model.VendorId;
            existing.VendorName = model.VendorName;

            existing.VendorGSTIN = model.VendorGSTIN;

            existing.InvoiceNumber = model.InvoiceNumber;
            existing.VendorInvoiceDate = model.VendorInvoiceDate;

            existing.ITCEligible = model.ITCEligible;
            existing.ITCStatus = model.ITCStatus;
            existing.ProjectId = model.ProjectId;
            existing.DepartmentId = model.DepartmentId;
            existing.CostCentreId = model.CostCentreId;
            existing.ExpenseClassification = model.ExpenseClassification;
            existing.IsEmployeeReimbursement = model.IsEmployeeReimbursement;
            existing.ReimbursementEmployeeId = model.ReimbursementEmployeeId;
            existing.ReimbursementStatus = model.ReimbursementStatus;

            existing.Remarks = model.Remarks;

            await ApplyVendorDefaults(existing);
            ApplyCategoryDefaults(existing, category);
            CalculateGSTAmounts(existing);
            RefreshPaymentFields(existing);

            await _voucherRepository.UpdateAsync(
                existing);

            await _voucherRepository.SaveAsync();

            TempData["Success"] =
                $"Expense Voucher '{existing.VoucherNumber}' updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        public async Task<IActionResult> Details(int id)
        {
            var voucher = await _voucherRepository.GetByIdAsync(id);
            if (voucher == null)
                return NotFound();

            if (!CanAccessVoucher(voucher))
            {
                return Forbid();
            }

            return View(voucher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int id)
        {
            var voucher =
                await _voucherRepository.GetByIdAsync(id);

            if (voucher == null)
            {
                return NotFound();
            }

            if (!CanModifyDraftVoucher(voucher))
            {
                TempData["Error"] =
                    "Only your own Draft expense claim can be submitted.";

                return RedirectToAction(nameof(Index));
            }

            bool submitted =
                await _voucherRepository.SubmitAsync(
                    id,
                    GetCurrentUserId());

            if (!submitted)
            {
                TempData["Error"] =
                    "Expense claim could not be submitted.";

                return RedirectToAction(nameof(Index));
            }

            try
            {
                await NotifyFinanceUsersAsync(
                    voucher,
                    notificationType: "ExpenseSubmitted",
                    title: "Expense Claim Submitted",
                    message:
                        $"Expense claim {voucher.VoucherNumber} for " +
                        $"₹{voucher.TotalAmount:N2} was submitted for approval.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Expense notification failed for voucher {VoucherId}.",
                    voucher.ExpenseVoucherId);
            }

            TempData["Success"] =
                $"Expense claim '{voucher.VoucherNumber}' submitted for approval.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {

            if (!IsFinanceUser())
            {
                return Forbid();
            } 
            var voucher = await _voucherRepository.GetByIdAsync(id);

            if (voucher == null)
                return NotFound();

            if (voucher.Status != FinancialConstants.ExpenseVoucherStatus.Submitted)
            {
                TempData["Error"] = "Only submitted expense vouchers can be approved.";
                return RedirectToAction(nameof(Index));
            }

            var userId = GetCurrentUserId();

            bool approved = await _voucherRepository.ApproveAsync(id,userId);

            if (!approved)
            {
                TempData["Error"] =
                    "Expense voucher could not be approved.";

                return RedirectToAction(nameof(Index));
            }
            await NotifyVoucherCreatorAsync(
             voucher,
             notificationType: "ExpenseApproved",
             title: "Expense Voucher Approved",
             message:
                 $"Expense voucher {voucher.VoucherNumber} for " +
                 $"₹{voucher.TotalAmount:N2} was approved.");
            TempData["Success"] = $"Expense Voucher '{voucher.VoucherNumber}' approved successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Post(int id)
        {
            if (!IsFinanceUser())
                return Forbid();

            var voucher = await _voucherRepository.GetByIdAsync(id);
            if (voucher == null)
                return NotFound();

            bool posted = await _voucherRepository.PostAsync(id, GetCurrentUserId());

            if (posted)
            {
                await NotifyVoucherCreatorAsync(
                    voucher,
                    notificationType: "ExpensePosted",
                    title: "Expense Voucher Posted",
                    message:
                        $"Expense voucher {voucher.VoucherNumber} was posted " +
                        $"to the accounts records.");
            }

            TempData[posted ? "Success" : "Error"] =
                posted
                    ? $"Expense Voucher '{voucher.VoucherNumber}' posted successfully."
                    : "Only approved expense vouchers can be posted.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reverse(int id, string reversalReason)
        {
            if (!IsFinanceUser())
                return Forbid();

            var voucher = await _voucherRepository.GetByIdAsync(id);
            if (voucher == null)
                return NotFound();

            if (voucher.Status != FinancialConstants.ExpenseVoucherStatus.Posted)
            {
                TempData["Error"] = "Only posted expense vouchers can be reversed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (voucher.IsReversed)
            {
                TempData["Error"] = "This expense voucher is already reversed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(reversalReason))
            {
                TempData["Error"] = "Reversal reason is required.";
                return RedirectToAction(nameof(Details), new { id });
            }

            voucher.IsReversed = true;
            voucher.ReversalReason = reversalReason.Trim();
            voucher.ReversedByUserId = GetCurrentUserId();
            voucher.ReversedOn = DateTime.Now;
            voucher.Status = FinancialConstants.ExpenseVoucherStatus.Reversed;
            voucher.ApprovalStatus = FinancialConstants.ExpenseVoucherStatus.Reversed;
            voucher.UpdatedOn = DateTime.Now;

            await _voucherRepository.UpdateAsync(voucher);
            await _voucherRepository.SaveAsync();

            await NotifyVoucherCreatorAsync(
             voucher,
             notificationType: "ExpenseReversed",
             title: "Expense Voucher Reversed",
             message:
                 $"Expense voucher {voucher.VoucherNumber} was reversed. " +
                 $"Reason: {voucher.ReversalReason}");

            TempData["Success"] = $"Expense Voucher '{voucher.VoucherNumber}' reversed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id,string rejectionReason)
        {
            if (!IsFinanceUser())
                return Forbid();

            var voucher =
                await _voucherRepository.GetByIdAsync(id);

            if (voucher == null)
                return NotFound();

            if (voucher.Status !=
                FinancialConstants.ExpenseVoucherStatus.Submitted)
            {
                TempData["Error"] =
                    "Only submitted expense vouchers can be rejected.";

                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                TempData["Error"] =
                    "Rejection reason is required.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (rejectionReason.Trim().Length > 500)
            {
                TempData["Error"] =
                    "Rejection reason cannot exceed 500 characters.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            bool rejected =
                await _voucherRepository.RejectAsync(
                    id,
                    GetCurrentUserId(),
                    rejectionReason);

            if (!rejected)
            {
                TempData["Error"] =
                    "Expense voucher could not be rejected.";

                return RedirectToAction(nameof(Index));
            }

            await NotifyVoucherCreatorAsync(
              voucher,
              notificationType: "ExpenseRejected",
              title: "Expense Voucher Rejected",
              message:
                  $"Expense voucher {voucher.VoucherNumber} was rejected. " +
                  $"Reason: {rejectionReason.Trim()}");

            TempData["Success"] =
                $"Expense Voucher '{voucher.VoucherNumber}' rejected.";

            return RedirectToAction(nameof(Index));
        }

        #region Delete
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var voucher = await _voucherRepository.GetByIdAsync(id);

            if (voucher == null)
            {
                return NotFound();
            }

            if (!CanModifyDraftVoucher(voucher))
            {
                TempData["Error"] = "Only accessible draft expense vouchers can be deleted.";
                return RedirectToAction(nameof(Index));
            }

            return View(voucher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var voucher = await _voucherRepository.GetByIdAsync(id);

            if (voucher == null)
            {
                return NotFound();
            }

            if (!CanModifyDraftVoucher(voucher))
            {
                TempData["Error"] = "Only accessible draft expense vouchers can be deleted.";
                return RedirectToAction(nameof(Index));
            }

            await _voucherRepository.SoftDeleteAsync(id);
            await _voucherRepository.SaveAsync();

            TempData["Success"] = $"Expense Voucher '{voucher.VoucherNumber}' deleted.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(
            int voucherId,
            string documentType,
            IFormFile file,
            string? remarks)
        {
            var voucher =
                await _voucherRepository.GetByIdAsync(voucherId);

            if (voucher == null)
                return NotFound();

            if (!CanAccessVoucher(voucher))
                return Forbid();

            documentType = documentType?.Trim() ?? string.Empty;
            remarks = remarks?.Trim();

            if (string.IsNullOrWhiteSpace(documentType))
            {
                TempData["Error"] = "Please select a document type.";
                return RedirectToAction(nameof(Details), new { id = voucherId });
            }

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction(nameof(Details), new { id = voucherId });
            }

            var uploadResult =
                await _fileStorageService.UploadAsync(file, DocumentFolder);

            if (!uploadResult.Success)
            {
                TempData["Error"] = uploadResult.ErrorMessage;
                return RedirectToAction(nameof(Details), new { id = voucherId });
            }

            try
            {
                var document = new ExpenseVoucherDocumentModel
                {
                    ExpenseVoucherId = voucherId,
                    DocumentType = documentType,
                    OriginalFileName = uploadResult.OriginalFileName,
                    StoredFilePath = uploadResult.RelativePath,
                    Remarks = remarks,
                    UploadedByUserId = GetCurrentUserId()
                };

                await _voucherRepository.AddDocumentAsync(document);
                await _voucherRepository.SaveAsync();

                TempData["Success"] = "Expense voucher document uploaded successfully.";
            }
            catch
            {
                await _fileStorageService.DeleteAsync(uploadResult.RelativePath);
                TempData["Error"] = "Document could not be saved. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id = voucherId });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var document =
                await _voucherRepository.GetDocumentByIdAsync(id);

            if (document == null || document.ExpenseVoucher == null)
                return NotFound();

            if (!CanAccessVoucher(document.ExpenseVoucher))
                return Forbid();

            var fileBytes =
                await _fileStorageService.DownloadAsync(document.StoredFilePath);

            if (fileBytes == null)
            {
                TempData["Error"] = "Document file was not found.";
                return RedirectToAction(
                    nameof(Details),
                    new { id = document.ExpenseVoucherId });
            }

            return File(
                fileBytes,
                GetContentType(document.OriginalFileName),
                Path.GetFileName(document.OriginalFileName));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var document =
                await _voucherRepository.GetDocumentByIdAsync(id);

            if (document == null || document.ExpenseVoucher == null)
                return NotFound();

            if (!CanAccessVoucher(document.ExpenseVoucher))
                return Forbid();

            if (document.ExpenseVoucher.Status !=
                FinancialConstants.ExpenseVoucherStatus.Draft)
            {
                TempData["Error"] =
                    "Documents linked to posted or rejected vouchers cannot be deleted.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = document.ExpenseVoucherId });
            }

            int voucherId = document.ExpenseVoucherId;
            string storedPath = document.StoredFilePath;

            await _voucherRepository.DeleteDocumentAsync(document);
            await _voucherRepository.SaveAsync();

            await _fileStorageService.DeleteAsync(storedPath);

            TempData["Success"] = "Expense voucher document deleted successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = voucherId });
        }
        // Helper Methods

        #region Helpers
        private bool IsFinanceUser()
        {
            return User.IsInRole("Admin") || User.IsInRole("Finance");
        }

        private bool CanAccessVoucher(ExpenseVoucherModel voucher)
        {
            if (IsFinanceUser())
            {
                return true;
            }

            return voucher.CreatedByUserId == GetCurrentUserId();
        }

        private bool CanModifyDraftVoucher(ExpenseVoucherModel voucher)
        {
            return voucher.Status == FinancialConstants.ExpenseVoucherStatus.Draft &&
                   CanAccessVoucher(voucher);
        }
        private async Task LoadLookups()
        {
            ViewBag.Categories =
                await _categoryRepository.GetAllActiveAsync();

            ViewBag.Vendors =
                await _context.Vendors
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.VendorName)
                    .ToListAsync();

            ViewBag.Projects =
                await _context.Projects
                    .AsNoTracking()
                    .OrderBy(x => x.ProjectName)
                    .ToListAsync();

            ViewBag.Departments =
                await _context.Departments
                    .AsNoTracking()
                    .OrderBy(x => x.DepartmentName)
                    .ToListAsync();

            ViewBag.States =
                await _locationRepository.GetActiveStatesAsync();
        }

        private string GetCurrentFinancialYear()
        {
            var today = DateTime.Now;
            int fyStart = today.Month >= 4 ? today.Year : today.Year - 1;
            int fyEnd = fyStart + 1;
            return $"{fyStart}-{fyEnd.ToString().Substring(2)}";
        }    

        private string GetCurrentUserId()
        {
            return _userManager.GetUserId(User)
                ?? throw new UnauthorizedAccessException(
                    "Current user could not be identified.");
        }

        private static string Csv(string? value)
        {
            value ??= string.Empty;
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        private static readonly decimal[] AllowedGstRates =
        {
            0m,
            5m,
            12m,
            18m,
            28m
        };

        private static readonly Regex GstinRegex =
            new(
                @"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$",
                RegexOptions.Compiled |
                RegexOptions.CultureInvariant);

        private static void NormalizeVoucher(
            ExpenseVoucherModel model)
        {
            model.Description =
                model.Description?.Trim() ??
                string.Empty;

            model.ExpensePartyType =
    string.IsNullOrWhiteSpace(model.ExpensePartyType)
        ? "Registered Vendor"
        : model.ExpensePartyType.Trim();

            model.VendorName =
                string.IsNullOrWhiteSpace(model.VendorName)
                    ? null
                    : model.VendorName.Trim();

            model.VendorGSTIN =
                string.IsNullOrWhiteSpace(model.VendorGSTIN)
                    ? null
                    : model.VendorGSTIN
                        .Trim()
                        .ToUpperInvariant();

            model.InvoiceNumber =
                string.IsNullOrWhiteSpace(model.InvoiceNumber)
                    ? null
                    : model.InvoiceNumber
                        .Trim()
                        .ToUpperInvariant();

            model.Remarks =
                string.IsNullOrWhiteSpace(model.Remarks)
                    ? null
                    : model.Remarks.Trim();

            model.BusinessPurpose =
                string.IsNullOrWhiteSpace(model.BusinessPurpose)
                    ? null
                    : model.BusinessPurpose.Trim();

            model.BeneficiaryName =
                string.IsNullOrWhiteSpace(model.BeneficiaryName)
                    ? null
                    : model.BeneficiaryName.Trim();

            model.SupportingReference =
                string.IsNullOrWhiteSpace(model.SupportingReference)
                    ? null
                    : model.SupportingReference.Trim();
        }

        private void ValidateVoucherBusinessRules(
            ExpenseVoucherModel model)
        {
            if (model.Amount <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.Amount),
                    "Amount must be greater than zero.");
            }

            if (model.VoucherDate.Date >
                DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.VoucherDate),
                    "Voucher date cannot be in the future.");
            }

            if (!AllowedGstRates.Contains(
                    model.GSTRate))
            {
                ModelState.AddModelError(
                    nameof(model.GSTRate),
                    "GST rate must be 0%, 5%, 12%, 18% or 28%.");
            }

            if (!string.IsNullOrWhiteSpace(
                    model.VendorGSTIN) &&
                !GstinRegex.IsMatch(model.VendorGSTIN))
            {
                ModelState.AddModelError(
                    nameof(model.VendorGSTIN),
                    "Enter a valid 15-character GSTIN.");
            }

            bool isRegisteredVendor =
    model.ExpensePartyType == "Registered Vendor";

            bool isSmallOrNonGstExpense =
                model.ExpensePartyType == "Unregistered Vendor" ||
                model.ExpensePartyType == "One-Time Vendor" ||
                model.ExpensePartyType == "Employee Reimbursement" ||
                model.ExpensePartyType == "Petty Cash";

            if (isRegisteredVendor && !model.VendorId.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.VendorId),
                    "Vendor is required for registered vendor expenses.");
            }

            if (isSmallOrNonGstExpense)
            {
                model.VendorId = null;
                model.VendorGSTIN = null;
                model.GSTRate = 0;
                model.ITCEligible = false;
                model.ITCStatus = "Not Applicable";
                model.Gstr2BMatchStatus = "Not Applicable";
                model.Gstr2BMatchedOn = null;
                model.Gstr2BMatchedByUserId = null;
                model.Gstr2BMismatchReason = null;
                model.ITCClaimMonth = null;
                model.ITCClaimYear = null;
                model.InputGSTGLAccountCode = null;
            }

            if (isSmallOrNonGstExpense &&
                string.IsNullOrWhiteSpace(model.VendorName) &&
                string.IsNullOrWhiteSpace(model.BeneficiaryName))
            {
                ModelState.AddModelError(
                    nameof(model.VendorName),
                    "Vendor or payee name is required.");
            }

            if (model.ITCEligible)
            {
                if (model.GSTRate <= 0)
                {
                    ModelState.AddModelError(
                        nameof(model.ITCEligible),
                        "ITC cannot be claimed when GST rate is zero.");
                }

                if (string.IsNullOrWhiteSpace(
                        model.VendorGSTIN))
                {
                    ModelState.AddModelError(
                        nameof(model.VendorGSTIN),
                        "Vendor GSTIN is required when ITC is eligible.");
                }

                if (string.IsNullOrWhiteSpace(
                        model.InvoiceNumber))
                {
                    ModelState.AddModelError(
                        nameof(model.InvoiceNumber),
                        "Vendor invoice number is required when ITC is eligible.");
                }
            }
        }
        private static void CalculateGSTAmounts(ExpenseVoucherModel model)
        {
            model.Amount = Math.Round(
                model.Amount,
                2,
                MidpointRounding.AwayFromZero);

            model.GSTRate = Math.Round(
                model.GSTRate,
                2,
                MidpointRounding.AwayFromZero);

            model.TaxableAmount = model.Amount;

            if (model.GSTRate <= 0)
            {
                model.CGSTAmount = 0;
                model.SGSTAmount = 0;
                model.IGSTAmount = 0;
                model.TotalGSTAmount = 0;
                model.TotalAmount = model.Amount;

                return;
            }

            decimal totalGst = Math.Round(
                model.Amount * model.GSTRate / 100,
                2,
                MidpointRounding.AwayFromZero);

            if (model.IsInterState)
            {
                model.CGSTAmount = 0;
                model.SGSTAmount = 0;
                model.IGSTAmount = totalGst;
            }
            else
            {
                decimal cgst = Math.Round(
                    totalGst / 2,
                    2,
                    MidpointRounding.AwayFromZero);

                model.CGSTAmount = cgst;
                model.SGSTAmount = totalGst - cgst;
                model.IGSTAmount = 0;
            }

            model.TotalGSTAmount = totalGst;

            model.TotalAmount = Math.Round(
                model.Amount + totalGst,
                2,
                MidpointRounding.AwayFromZero);
        }

        private async Task ValidateGstPeriodOpen(DateTime voucherDate)
        {
            bool isClosed =
                await _context.GstMonthlySnapshots
                    .AnyAsync(x =>
                        x.Month == voucherDate.Month &&
                        x.Year == voucherDate.Year &&
                        (x.Status == FinancialConstants.GstSnapshotStatus.Filed ||
                         x.Status == FinancialConstants.GstSnapshotStatus.Locked ||
                         x.IsFiledPeriodLocked));

            if (isClosed)
            {
                ModelState.AddModelError(
                    nameof(ExpenseVoucherModel.VoucherDate),
                    "This GST period is filed or locked. Reopen the GST period before changing expenses.");
            }
        }

        private async Task ApplyVendorDefaults(ExpenseVoucherModel model)
        {
            if (!model.VendorId.HasValue)
                return;

            var vendor = await _context.Vendors
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.VendorId == model.VendorId.Value && x.IsActive);

            if (vendor == null)
                return;

            model.VendorName = vendor.VendorName;
            model.VendorGSTIN = vendor.GSTIN;
            model.VendorStateCode = vendor.StateCode;

            if (string.IsNullOrWhiteSpace(model.PlaceOfSupplyStateCode))
            {
                model.PlaceOfSupplyStateCode = vendor.StateCode;
            }

            if (!model.IsGstStateOverride &&
                !string.IsNullOrWhiteSpace(model.CompanyStateCode) &&
                !string.IsNullOrWhiteSpace(model.PlaceOfSupplyStateCode))
            {
                model.IsInterState =
                    model.CompanyStateCode != model.PlaceOfSupplyStateCode;
            }
        }

        private static void ApplyCategoryDefaults(
            ExpenseVoucherModel model,
            ExpenseCategoryModel? category)
        {
            if (category == null)
                return;

            model.GLAccountCode = category.GLAccountCode;
            model.PayableGLAccountCode = category.PayableGLAccountCode;
            model.InputGSTGLAccountCode = category.InputGSTGLAccountCode;

            if (string.IsNullOrWhiteSpace(model.ExpenseClassification))
            {
                model.ExpenseClassification = category.ExpenseType;
            }

            model.ITCStatus =
                model.ITCEligible
                    ? model.ITCStatus
                    : "Not Applicable";
        }

        private static void RefreshPaymentFields(ExpenseVoucherModel model)
        {
            model.PaidAmount = Math.Round(model.PaidAmount, 2, MidpointRounding.AwayFromZero);
            model.BalanceAmount = Math.Max(model.TotalAmount - model.PaidAmount, 0);

            if (model.PaidAmount <= 0)
            {
                model.PaymentStatus = FinancialConstants.PaymentStatus.Unpaid;
            }
            else if (model.BalanceAmount <= 0)
            {
                model.PaymentStatus = FinancialConstants.PaymentStatus.Paid;
            }
            else
            {
                model.PaymentStatus = FinancialConstants.PaymentStatus.PartiallyPaid;
            }
        }
        private static string GetContentType(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }

        private async Task NotifyFinanceUsersAsync(
    ExpenseVoucherModel voucher,
    string notificationType,
    string title,
    string message)
        {
            var admins =
                await _userManager.GetUsersInRoleAsync("Admin");

            var financeUsers =
                await _userManager.GetUsersInRoleAsync("Finance");

            string currentUserId = GetCurrentUserId();

            var recipients = admins
                .Concat(financeUsers)
                .Where(x =>
                 x.IsActive &&
                 x.Id != currentUserId)
                .GroupBy(x => x.Id)
                .Select(x => x.First())
                .ToList();

            foreach (var recipient in recipients)
            {
                bool exists =
                    await _notificationService.ExistsAsync(
                        recipient.Id,
                        notificationType,
                        "ExpenseVoucher",
                        voucher.ExpenseVoucherId);

                if (exists)
                {
                    continue;
                }

                await _notificationService.CreateAsync(
                    userId: recipient.Id,
                    title: title,
                    message: message,
                    notificationType: notificationType,
                    referenceType: "ExpenseVoucher",
                    referenceId: voucher.ExpenseVoucherId,
                    actionUrl:
                        $"/ExpenseVoucher/Details/{voucher.ExpenseVoucherId}");
            }
        }

        private async Task NotifyVoucherCreatorAsync(
    ExpenseVoucherModel voucher,
    string notificationType,
    string title,
    string message)
        {
            if (string.IsNullOrWhiteSpace(voucher.CreatedByUserId))
            {
                return;
            }

            var creator =
                await _userManager.FindByIdAsync(
                    voucher.CreatedByUserId);

            if (creator == null || !creator.IsActive)
            {
                return;
            }

            var roles =
                await _userManager.GetRolesAsync(creator);

            bool canReceiveExpenseNotification =
                roles.Contains("Admin") ||
                roles.Contains("Finance") ||
                roles.Contains("Employee");

            if (!canReceiveExpenseNotification)
            {
                return;
            }

            bool exists =
                await _notificationService.ExistsAsync(
                    creator.Id,
                    notificationType,
                    "ExpenseVoucher",
                    voucher.ExpenseVoucherId);

            if (exists)
            {
                return;
            }

            await _notificationService.CreateAsync(
                userId: creator.Id,
                title: title,
                message: message,
                notificationType: notificationType,
                referenceType: "ExpenseVoucher",
                referenceId: voucher.ExpenseVoucherId,
                actionUrl:
                    $"/ExpenseVoucher/Details/{voucher.ExpenseVoucherId}");
        }

        #endregion Helpers
    }
}



