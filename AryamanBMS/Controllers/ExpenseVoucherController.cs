using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using AryamanBMS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance,Employee")]
    public class ExpenseVoucherController : Controller
    {
        private readonly IExpenseVoucherRepository _voucherRepository;
        private readonly IExpenseCategoryRepository _categoryRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly ApplicationDbContext _context;

        private const string DocumentFolder = "ExpenseVoucherDocuments";

        public ExpenseVoucherController(
            IExpenseVoucherRepository voucherRepository,
            IExpenseCategoryRepository categoryRepository,
            IFileStorageService fileStorageService,
            UserManager<ApplicationUserModel> userManager,
            ApplicationDbContext context)
        {
            _voucherRepository = voucherRepository;
            _categoryRepository = categoryRepository;
            _fileStorageService = fileStorageService;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index(string? status, int? categoryId, string? search)
        {
            var vouchers = await _voucherRepository.GetAllAsync();

            if (!IsFinanceUser())
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
                        (!string.IsNullOrWhiteSpace(x.VoucherNumber) && x.VoucherNumber.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.VendorName) && x.VendorName.ToLower().Contains(keyword)) ||
                        (x.Vendor != null && x.Vendor.VendorName.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.InvoiceNumber) && x.InvoiceNumber.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.Description) && x.Description.ToLower().Contains(keyword)))
                    .ToList();
            }

            ViewBag.Status = status;
            ViewBag.CategoryId = categoryId;
            ViewBag.Search = search;

            await LoadLookups();

            return View(vouchers);
        }

        public IActionResult Pending()
        {
            return RedirectToAction(nameof(Index), new
            {
                status = FinancialConstants.ExpenseVoucherStatus.Submitted
            });
        }

        public async Task<IActionResult> Create()
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
        public async Task<IActionResult> Create(ExpenseVoucherModel model)
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

            return RedirectToAction(nameof(Index));
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
            var voucher = await _voucherRepository.GetByIdAsync(id);
            if (voucher == null)
                return NotFound();

            if (!CanModifyDraftVoucher(voucher))
            {
                TempData["Error"] = "Only accessible draft vouchers can be submitted.";
                return RedirectToAction(nameof(Index));
            }

            bool submitted = await _voucherRepository.SubmitAsync(id, GetCurrentUserId());

            TempData[submitted ? "Success" : "Error"] =
                submitted
                    ? $"Expense Voucher '{voucher.VoucherNumber}' submitted for approval."
                    : "Expense voucher could not be submitted.";

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

            bool approved =
                await _voucherRepository.ApproveAsync(
                    id,
                    userId);

            if (!approved)
            {
                TempData["Error"] =
                    "Expense voucher could not be approved.";

                return RedirectToAction(nameof(Index));
            }

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
            ViewBag.Categories = await _categoryRepository.GetAllActiveAsync();
            ViewBag.Vendors = await _context.Vendors
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.VendorName)
                .ToListAsync();
            ViewBag.Projects = await _context.Projects
                .AsNoTracking()
                .OrderBy(x => x.ProjectName)
                .ToListAsync();
            ViewBag.Departments = await _context.Departments
                .AsNoTracking()
                .OrderBy(x => x.DepartmentName)
                .ToListAsync();
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
    }
}



