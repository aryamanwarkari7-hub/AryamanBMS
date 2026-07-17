using System.Text;
using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class VendorPaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly INotificationService _notificationService;
        private readonly ILogger<VendorPaymentController> _logger;

        public VendorPaymentController(
            ApplicationDbContext context,
            UserManager<ApplicationUserModel> userManager,
            INotificationService notificationService,
            ILogger<VendorPaymentController> logger)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(
            int? vendorId,
            string? search,
            DateTime? fromDate,
            DateTime? toDate,
            string sortBy = "PaymentDate",
            string sortOrder = "desc",
            int page = 1)
        {
            var payments = await _context.VendorPayments
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Include(x => x.Vendor)
                .Include(x => x.ExpenseVoucher)
                .ToListAsync();

            if (vendorId.HasValue)
            {
                payments = payments
                    .Where(x => x.VendorId == vendorId.Value)
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim().ToLower();

                payments = payments
                    .Where(x =>
                        (!string.IsNullOrWhiteSpace(x.PaymentNo) &&
                            x.PaymentNo.ToLower().Contains(keyword)) ||
                        (x.Vendor != null &&
                            !string.IsNullOrWhiteSpace(x.Vendor.VendorName) &&
                            x.Vendor.VendorName.ToLower().Contains(keyword)) ||
                        (x.ExpenseVoucher != null &&
                            !string.IsNullOrWhiteSpace(x.ExpenseVoucher.VoucherNumber) &&
                            x.ExpenseVoucher.VoucherNumber.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.PaymentMode) &&
                            x.PaymentMode.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.TransactionReference) &&
                            x.TransactionReference.ToLower().Contains(keyword)))
                    .ToList();
            }

            if (fromDate.HasValue)
            {
                payments = payments
                    .Where(x => x.PaymentDate.Date >= fromDate.Value.Date)
                    .ToList();
            }

            if (toDate.HasValue)
            {
                payments = payments
                    .Where(x => x.PaymentDate.Date <= toDate.Value.Date)
                    .ToList();
            }

            bool desc =
                string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            payments = sortBy switch
            {
                "PaymentNo" => desc
                    ? payments.OrderByDescending(x => x.PaymentNo).ToList()
                    : payments.OrderBy(x => x.PaymentNo).ToList(),

                "Vendor" => desc
                    ? payments.OrderByDescending(x => x.Vendor?.VendorName).ToList()
                    : payments.OrderBy(x => x.Vendor?.VendorName).ToList(),

                "Voucher" => desc
                    ? payments.OrderByDescending(x => x.ExpenseVoucher?.VoucherNumber).ToList()
                    : payments.OrderBy(x => x.ExpenseVoucher?.VoucherNumber).ToList(),

                "Mode" => desc
                    ? payments.OrderByDescending(x => x.PaymentMode).ToList()
                    : payments.OrderBy(x => x.PaymentMode).ToList(),

                "Reference" => desc
                    ? payments.OrderByDescending(x => x.TransactionReference).ToList()
                    : payments.OrderBy(x => x.TransactionReference).ToList(),

                "Amount" => desc
                    ? payments.OrderByDescending(x => x.AmountPaid).ToList()
                    : payments.OrderBy(x => x.AmountPaid).ToList(),

                _ => desc
                    ? payments.OrderByDescending(x => x.PaymentDate).ToList()
                    : payments.OrderBy(x => x.PaymentDate).ToList()
            };

            const int pageSize = 20;
            int totalRecords = payments.Count;

            payments = payments
                .Skip((Math.Max(page, 1) - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            await LoadLookups();
            ViewBag.VendorId = vendorId;
            ViewBag.Search = search;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages =
                (int)Math.Ceiling(totalRecords / (double)pageSize);
            return View(payments);
        }

        public async Task<IActionResult> ExportExcel(
            int? vendorId,
            string? search,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var payments = await _context.VendorPayments
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Include(x => x.Vendor)
                .Include(x => x.ExpenseVoucher)
                .ToListAsync();

            if (vendorId.HasValue)
                payments = payments.Where(x => x.VendorId == vendorId.Value).ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim().ToLower();
                payments = payments.Where(x =>
                    (!string.IsNullOrWhiteSpace(x.PaymentNo) && x.PaymentNo.ToLower().Contains(keyword)) ||
                    (x.Vendor != null && !string.IsNullOrWhiteSpace(x.Vendor.VendorName) && x.Vendor.VendorName.ToLower().Contains(keyword)) ||
                    (x.ExpenseVoucher != null && !string.IsNullOrWhiteSpace(x.ExpenseVoucher.VoucherNumber) && x.ExpenseVoucher.VoucherNumber.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(x.PaymentMode) && x.PaymentMode.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(x.TransactionReference) && x.TransactionReference.ToLower().Contains(keyword)))
                    .ToList();
            }

            if (fromDate.HasValue)
                payments = payments.Where(x => x.PaymentDate.Date >= fromDate.Value.Date).ToList();

            if (toDate.HasValue)
                payments = payments.Where(x => x.PaymentDate.Date <= toDate.Value.Date).ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Vendor Payments");

            worksheet.Cell(1, 1).Value = "Payment No";
            worksheet.Cell(1, 2).Value = "Date";
            worksheet.Cell(1, 3).Value = "Vendor";
            worksheet.Cell(1, 4).Value = "Voucher";
            worksheet.Cell(1, 5).Value = "Mode";
            worksheet.Cell(1, 6).Value = "Reference";
            worksheet.Cell(1, 7).Value = "Amount";

            int row = 2;

            foreach (var item in payments.OrderByDescending(x => x.PaymentDate))
            {
                worksheet.Cell(row, 1).Value = item.PaymentNo;
                worksheet.Cell(row, 2).Value = item.PaymentDate;
                worksheet.Cell(row, 2).Style.DateFormat.Format = "dd-MMM-yyyy";
                worksheet.Cell(row, 3).Value = item.Vendor?.VendorName;
                worksheet.Cell(row, 4).Value = item.ExpenseVoucher?.VoucherNumber;
                worksheet.Cell(row, 5).Value = item.PaymentMode;
                worksheet.Cell(row, 6).Value = item.TransactionReference;
                worksheet.Cell(row, 7).Value = item.AmountPaid;

                row++;
            }

            var headerRange = worksheet.Range("A1:G1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"vendor-payments-{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }

        public async Task<IActionResult> Create(int? expenseVoucherId)
        {
            var model = new VendorPaymentModel
            {
                PaymentDate = DateTime.Today,
                ExpenseVoucherId = expenseVoucherId ?? 0
            };

            if (expenseVoucherId.HasValue)
            {
                var voucher = await _context.ExpenseVouchers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ExpenseVoucherId == expenseVoucherId.Value);

                if (voucher != null)
                {
                    model.VendorId = voucher.VendorId ?? 0;
                    model.AmountPaid = voucher.BalanceAmount > 0
                        ? voucher.BalanceAmount
                        : voucher.TotalAmount - voucher.PaidAmount;
                }
            }

            await LoadLookups();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VendorPaymentModel model)
        {
            ModelState.Remove(nameof(model.PaymentNo));
            ModelState.Remove(nameof(model.PaidByUserId));

            model.TransactionReference = string.IsNullOrWhiteSpace(model.TransactionReference)
                ? null
                : model.TransactionReference.Trim();
            model.Remarks = string.IsNullOrWhiteSpace(model.Remarks)
                ? null
                : model.Remarks.Trim();

            var voucher = await _context.ExpenseVouchers
                .Include(x => x.VendorPayments.Where(p => p.IsActive))
                .FirstOrDefaultAsync(x =>
                    x.ExpenseVoucherId == model.ExpenseVoucherId &&
                    x.IsActive);

            if (voucher == null)
            {
                ModelState.AddModelError(nameof(model.ExpenseVoucherId), "Selected voucher does not exist.");
            }
            else
            {
                decimal paid = voucher.VendorPayments.Sum(x => x.AmountPaid);
                decimal balance = Math.Max(voucher.TotalAmount - paid, 0);

                if (voucher.Status != FinancialConstants.ExpenseVoucherStatus.Posted)
                {
                    ModelState.AddModelError(nameof(model.ExpenseVoucherId), "Only posted vouchers can be paid.");
                }

                if (model.VendorId != voucher.VendorId)
                {
                    ModelState.AddModelError(nameof(model.VendorId), "Vendor must match the selected voucher.");
                }

                if (model.AmountPaid <= 0 || model.AmountPaid > balance)
                {
                    ModelState.AddModelError(nameof(model.AmountPaid), $"Payment cannot exceed unpaid balance {balance:N2}.");
                }
            }

            if (!string.IsNullOrWhiteSpace(model.TransactionReference) &&
                await _context.VendorPayments.AnyAsync(x =>
                    x.IsActive &&
                    x.TransactionReference == model.TransactionReference))
            {
                ModelState.AddModelError(nameof(model.TransactionReference), "This transaction reference is already used.");
            }

            if (!ModelState.IsValid)
            {
                await LoadLookups();
                return View(model);
            }

            model.PaymentNo = await GetNextPaymentNo();
            model.PaidByUserId = _userManager.GetUserId(User) ?? string.Empty;
            model.CreatedOn = DateTime.Now;
            model.IsActive = true;

            await _context.VendorPayments.AddAsync(model);
            await _context.SaveChangesAsync();

            await RefreshVoucherPaymentStatus(model.ExpenseVoucherId);

            await NotifyFinanceUsersAsync(
                notificationType: "VendorPaymentMade",
                title: "Vendor Payment Recorded",
                message:
                    $"Vendor payment {model.PaymentNo} of ₹{model.AmountPaid:N2} " +
                    "has been recorded.",
                referenceType: "VendorPayment",
                referenceId: model.VendorPaymentId,
                actionUrl: "/VendorPayment/Index",
                actionUserId: _userManager.GetUserId(User));

            TempData["Success"] = $"Vendor payment '{model.PaymentNo}' recorded successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadLookups()
        {
            ViewBag.Vendors = new SelectList(
                await _context.Vendors
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.VendorName)
                    .ToListAsync(),
                "VendorId",
                "VendorName");

            ViewBag.Vouchers = await _context.ExpenseVouchers
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.Status == FinancialConstants.ExpenseVoucherStatus.Posted &&
                    x.PaymentStatus != FinancialConstants.PaymentStatus.Paid)
                .Select(x => new
                {
                    x.ExpenseVoucherId,
                    x.VendorId,
                    VoucherText =
                        x.VoucherNumber + " | " +
                        (x.Vendor != null ? x.Vendor.VendorName : x.VendorName) +
                        " | Balance: ₹" +
                        (
                            x.BalanceAmount > 0
                                ? x.BalanceAmount
                                : x.TotalAmount - x.PaidAmount
                        ).ToString("N2")
                })
                .OrderByDescending(x => x.ExpenseVoucherId)
                .ToListAsync();
        }

        private async Task<string> GetNextPaymentNo()
        {
            string financialYear = GetCurrentFinancialYear();
            int count = await _context.VendorPayments
                .CountAsync(x => x.PaymentNo.Contains(financialYear));

            return $"VPAY-{financialYear}-{count + 1:0000}";
        }

        private async Task RefreshVoucherPaymentStatus(int voucherId)
        {
            var voucher = await _context.ExpenseVouchers
                .Include(x => x.VendorPayments.Where(p => p.IsActive))
                .FirstOrDefaultAsync(x => x.ExpenseVoucherId == voucherId);

            if (voucher == null)
                return;

            voucher.PaidAmount = voucher.VendorPayments.Sum(x => x.AmountPaid);
            voucher.BalanceAmount = Math.Max(voucher.TotalAmount - voucher.PaidAmount, 0);
            voucher.PaymentStatus = voucher.PaidAmount <= 0
                ? FinancialConstants.PaymentStatus.Unpaid
                : voucher.BalanceAmount <= 0
                    ? FinancialConstants.PaymentStatus.Paid
                    : FinancialConstants.PaymentStatus.PartiallyPaid;
            voucher.UpdatedOn = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        private async Task NotifyFinanceUsersAsync(
            string notificationType,
            string title,
            string message,
            string referenceType,
            int referenceId,
            string actionUrl,
            string? actionUserId)
        {
            try
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                var financeUsers = await _userManager.GetUsersInRoleAsync("Finance");

                var recipients = admins
                    .Concat(financeUsers)
                    .Where(x => x.IsActive && x.Id != actionUserId)
                    .GroupBy(x => x.Id)
                    .Select(x => x.First())
                    .ToList();

                foreach (var recipient in recipients)
                {
                    bool exists = await _notificationService.ExistsAsync(
                        recipient.Id,
                        notificationType,
                        referenceType,
                        referenceId);

                    if (exists)
                    {
                        continue;
                    }

                    await _notificationService.CreateAsync(
                        recipient.Id,
                        title,
                        message,
                        notificationType,
                        referenceType,
                        referenceId,
                        actionUrl);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Vendor payment notification failed. Type: {NotificationType}, Reference: {ReferenceType}/{ReferenceId}",
                    notificationType,
                    referenceType,
                    referenceId);
            }
        }

        private static string GetCurrentFinancialYear()
        {
            var today = DateTime.Now;
            int fyStart = today.Month >= 4 ? today.Year : today.Year - 1;
            int fyEnd = fyStart + 1;
            return $"{fyStart}-{fyEnd.ToString().Substring(2)}";
        }

        private static string Csv(string? value)
        {
            value ??= string.Empty;
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}
