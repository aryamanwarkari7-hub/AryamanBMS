using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Services.Interfaces;
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

        public async Task<IActionResult> Index(int? vendorId)
        {
            var payments = await _context.VendorPayments
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Include(x => x.Vendor)
                .Include(x => x.ExpenseVoucher)
                .OrderByDescending(x => x.PaymentDate)
                .ToListAsync();

            if (vendorId.HasValue)
            {
                payments = payments
                    .Where(x => x.VendorId == vendorId.Value)
                    .ToList();
            }

            await LoadLookups();
            ViewBag.VendorId = vendorId;
            return View(payments);
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

            ViewBag.Vouchers = new SelectList(
                await _context.ExpenseVouchers
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive &&
                        x.Status == FinancialConstants.ExpenseVoucherStatus.Posted &&
                        x.PaymentStatus != FinancialConstants.PaymentStatus.Paid)
                    .OrderByDescending(x => x.VoucherDate)
                    .ToListAsync(),
                "ExpenseVoucherId",
                "VoucherNumber");
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
    }
}
