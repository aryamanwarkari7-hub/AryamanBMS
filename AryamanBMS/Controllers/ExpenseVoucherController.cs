using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        private const string DocumentFolder = "ExpenseVoucherDocuments";

        public ExpenseVoucherController(
            IExpenseVoucherRepository voucherRepository,
            IExpenseCategoryRepository categoryRepository,
            IFileStorageService fileStorageService,
            UserManager<ApplicationUserModel> userManager)
        {
            _voucherRepository = voucherRepository;
            _categoryRepository = categoryRepository;
            _fileStorageService = fileStorageService;
            _userManager = userManager;
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
                        (!string.IsNullOrWhiteSpace(x.InvoiceNumber) && x.InvoiceNumber.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.Description) && x.Description.ToLower().Contains(keyword)))
                    .ToList();
            }

            ViewBag.Status = status;
            ViewBag.CategoryId = categoryId;
            ViewBag.Search = search;

            await LoadCategories();

            return View(vouchers);
        }

        public IActionResult Pending()
        {
            return RedirectToAction(nameof(Index), new
            {
                status = FinancialConstants.ExpenseVoucherStatus.Draft
            });
        }

        public async Task<IActionResult> Create()
        {
            var model = new ExpenseVoucherModel
            {
                VoucherDate = DateTime.Now,
                FinancialYear = GetCurrentFinancialYear()
            };

            await LoadCategories();
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

            if (!string.IsNullOrWhiteSpace(
                    model.InvoiceNumber))
            {
                bool duplicateExists =
                    await _voucherRepository
                        .VendorInvoiceExistsAsync(
                            model.VendorName,
                            model.InvoiceNumber);

                if (duplicateExists)
                {
                    ModelState.AddModelError(
                        nameof(model.InvoiceNumber),
                        "This vendor invoice number already exists for the vendor.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadCategories();
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

            await LoadCategories();
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

            if (!string.IsNullOrWhiteSpace(
                    model.InvoiceNumber))
            {
                bool duplicateExists =
                    await _voucherRepository
                        .VendorInvoiceExistsAsync(
                            model.VendorName,
                            model.InvoiceNumber,
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
                await LoadCategories();
                return View(model);
            }

            existing.ExpenseCategoryId =     model.ExpenseCategoryId;

            existing.VoucherDate =  model.VoucherDate;

            existing.Description =  model.Description;

            existing.Amount = model.Amount;

            existing.GSTRate =  model.GSTRate;

            existing.IsInterState =model.IsInterState;

            existing.VendorName = model.VendorName;

            existing.VendorGSTIN = model.VendorGSTIN;

            existing.InvoiceNumber = model.InvoiceNumber;

            existing.ITCEligible = model.ITCEligible;

            existing.Remarks = model.Remarks;

            CalculateGSTAmounts(existing);

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
        public async Task<IActionResult> Approve(int id)
        {

            if (!IsFinanceUser())
            {
                return Forbid();
            } 
            var voucher = await _voucherRepository.GetByIdAsync(id);
            if (voucher == null)
                return NotFound();

            if (voucher.Status != FinancialConstants.ExpenseVoucherStatus.Draft)
            {
                TempData["Error"] = "Only draft expense vouchers can be approved.";
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
        public async Task<IActionResult> Reject(int id,string rejectionReason)
        {
            if (!IsFinanceUser())
                return Forbid();

            var voucher =
                await _voucherRepository.GetByIdAsync(id);

            if (voucher == null)
                return NotFound();

            if (voucher.Status !=
                FinancialConstants.ExpenseVoucherStatus.Draft)
            {
                TempData["Error"] =
                    "Only draft expense vouchers can be rejected.";

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
        private async Task LoadCategories()
        {
            ViewBag.Categories = await _categoryRepository.GetAllActiveAsync();
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



