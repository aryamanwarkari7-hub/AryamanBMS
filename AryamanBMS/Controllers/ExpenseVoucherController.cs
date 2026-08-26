using System.Text;
using System.Text.RegularExpressions;
using AryamanBMS.Business.Interfaces;
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
        private readonly IExpenseVoucherTrackerService _voucherTrackerService;
        private readonly IExpenseVoucherCreateService _voucherCreateService;
        private readonly IExpenseVoucherTransitionService _voucherTransitionService;
        private readonly IExpenseVoucherDocumentService _voucherDocumentService;
        private readonly IExpenseCategoryRepository _categoryRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly IVendorRepository _vendorRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ExpenseVoucherController> _logger;
        private readonly ILocationRepository _locationRepository;

        private const string DocumentFolder = "ExpenseVoucherDocuments";

        public ExpenseVoucherController(
           IExpenseVoucherRepository voucherRepository,
           IExpenseVoucherTrackerService voucherTrackerService,
           IExpenseVoucherCreateService voucherCreateService,
           IExpenseVoucherTransitionService voucherTransitionService,
           IExpenseVoucherDocumentService voucherDocumentService,
           IExpenseCategoryRepository categoryRepository,
           IFileStorageService fileStorageService,
           UserManager<ApplicationUserModel> userManager,
           IVendorRepository vendorRepository,
           IProjectRepository projectRepository,
           IDepartmentRepository departmentRepository,
           INotificationService notificationService,
           ILocationRepository locationRepository,
           ILogger<ExpenseVoucherController> logger)
        {
            _voucherRepository = voucherRepository;
            _voucherTrackerService = voucherTrackerService;
            _voucherCreateService = voucherCreateService;
            _voucherTransitionService = voucherTransitionService;
            _voucherDocumentService = voucherDocumentService;
            _categoryRepository = categoryRepository;
            _fileStorageService = fileStorageService;
            _userManager = userManager;
            _vendorRepository = vendorRepository;
            _projectRepository = projectRepository;
            _departmentRepository = departmentRepository;
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
            bool restrictToCurrentUser = mine || !IsFinanceUser();
            string userId = restrictToCurrentUser
                ? GetCurrentUserId()
                : string.Empty;

            var tracker = await _voucherTrackerService.GetTrackerAsync(
                status,
                categoryId,
                search,
                fromDate,
                toDate,
                sortBy,
                sortOrder,
                page,
                userId,
                restrictToCurrentUser);

            ViewBag.Status = status;
            ViewBag.CategoryId = categoryId;
            ViewBag.Search = search;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            ViewBag.Mine = mine;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = tracker.TotalPages;

            await LoadLookups();

            return View(tracker.Vouchers);
        }

        public async Task<IActionResult> ExportExcel(
            string? status,
            int? categoryId,
            string? search,
            DateTime? fromDate,
            DateTime? toDate,
            bool mine = false)
        {
            bool restrictToCurrentUser = mine || !IsFinanceUser();
            string userId = restrictToCurrentUser
                ? GetCurrentUserId()
                : string.Empty;

            var vouchers = await _voucherTrackerService.GetForExportAsync(
                status,
                categoryId,
                search,
                fromDate,
                toDate,
                userId,
                restrictToCurrentUser);
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
            ModelState.Remove(nameof(model.VoucherNumber));
            ModelState.Remove(nameof(model.CreatedByUserId));

            var validation = await _voucherCreateService.ValidateAsync(model);
            AddValidationErrors(validation.Errors);

            if (!ModelState.IsValid)
            {
                await LoadLookups();
                return View(model);
            }

            await _voucherCreateService.CreateAsync(
                model,
                validation.Category,
                GetCurrentUserId(),
                GetCurrentFinancialYear());

            TempData["Success"] =
                $"Expense Voucher '{model.VoucherNumber}' created successfully.";

            return RedirectToAction(nameof(Index), new { mine });
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
            var existing = await _voucherRepository.GetByIdAsync(model.ExpenseVoucherId);
            if (existing == null) return NotFound();
            if (!CanModifyDraftVoucher(existing))
            {
                TempData["Error"] = "Only accessible draft expense vouchers can be edited.";
                return RedirectToAction(nameof(Index));
            }

            var validation = await _voucherCreateService.ValidateForUpdateAsync(model, existing);
            AddValidationErrors(validation.Errors);
            if (!ModelState.IsValid) { await LoadLookups(); return View(model); }

            await _voucherCreateService.UpdateAsync(existing, model, validation.Category);
            TempData["Success"] = $"Expense Voucher '{existing.VoucherNumber}' updated successfully.";
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

            var submitted = await _voucherTransitionService.SubmitAsync(
                voucher,
                GetCurrentUserId());

            if (!submitted.Succeeded)
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

            var approved = await _voucherTransitionService.ApproveAsync(voucher, userId);

            if (!approved.Succeeded)
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

            var posted = await _voucherTransitionService.PostAsync(voucher, GetCurrentUserId());

            if (posted.Succeeded)
            {
                await NotifyVoucherCreatorAsync(
                    voucher,
                    notificationType: "ExpensePosted",
                    title: "Expense Voucher Posted",
                    message:
                        $"Expense voucher {voucher.VoucherNumber} was posted " +
                        $"to the accounts records.");
            }

            TempData[posted.Succeeded ? "Success" : "Error"] =
                posted.Succeeded
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

            var reversed = await _voucherTransitionService.ReverseAsync(voucher, GetCurrentUserId(), reversalReason);
            if (!reversed.Succeeded) { TempData["Error"] = reversed.ErrorMessage; return RedirectToAction(nameof(Details), new { id }); }

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
        public async Task<IActionResult> Reject(int id, string rejectionReason)
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

            var rejected = await _voucherTransitionService.RejectAsync(voucher, GetCurrentUserId(), rejectionReason);

            if (!rejected.Succeeded)
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

            var deleted = await _voucherTransitionService.DeleteDraftAsync(voucher);
            if (!deleted.Succeeded) { TempData["Error"] = deleted.ErrorMessage; return RedirectToAction(nameof(Index)); }

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

                await _voucherDocumentService.CreateAsync(document);

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

            int voucherId = document.ExpenseVoucherId;
            string storedPath = document.StoredFilePath;

            if (!await _voucherDocumentService.DeleteAsync(document))
            {
                TempData["Error"] =
                    "Documents linked to posted or rejected vouchers cannot be deleted.";
                return RedirectToAction(nameof(Details), new { id = voucherId });
            }

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
            return User.IsInRole("Admin") ||
                   User.IsInRole("Finance") ||
                   User.IsInRole("Master");
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

            ViewBag.Vendors = await _vendorRepository.GetActiveAsync();

            ViewBag.Projects = _projectRepository.Projects
                .OrderBy(x => x.ProjectName)
                .ToList();

            ViewBag.Departments = (await _departmentRepository.GetAllAsync())
                .OrderBy(x => x.DepartmentName)
                .ToList();

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

        private void AddValidationErrors(
            IReadOnlyDictionary<string, string> errors)
        {
            foreach (var error in errors)
            {
                ModelState.AddModelError(error.Key, error.Value);
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



