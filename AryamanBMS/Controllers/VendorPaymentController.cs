using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Master")]
    public class VendorPaymentController : Controller
    {
        #region Actions

        private readonly IVendorPaymentService _vendorPaymentService;
        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly INotificationService _notificationService;
        private readonly ILogger<VendorPaymentController> _logger;

        public VendorPaymentController(
            IVendorPaymentService vendorPaymentService,
            UserManager<ApplicationUserModel> userManager,
            INotificationService notificationService,
            ILogger<VendorPaymentController> logger)
        {
            _vendorPaymentService = vendorPaymentService;
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
            var payments = await _vendorPaymentService.GetAllAsync(
                vendorId,
                search,
                fromDate,
                toDate,
                sortBy,
                sortOrder);

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
            var payments = await _vendorPaymentService.GetAllAsync(
                vendorId,
                search,
                fromDate,
                toDate,
                "PaymentDate",
                "desc");

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
            var model = await _vendorPaymentService.CreateDraftAsync(expenseVoucherId);

            await LoadLookups();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VendorPaymentModel model)
        {
            ModelState.Remove(nameof(model.PaymentNo));
            ModelState.Remove(nameof(model.PaidByUserId));

            var validationErrors = await _vendorPaymentService.ValidateAsync(model);
            foreach (var (field, error) in validationErrors)
            {
                ModelState.AddModelError(field, error);
            }

            if (!ModelState.IsValid)
            {
                await LoadLookups();
                return View(model);
            }

            await _vendorPaymentService.CreateAsync(
                model,
                _userManager.GetUserId(User) ?? string.Empty);

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
            var vendors = await _vendorPaymentService.GetActiveVendorsAsync();
            var vouchers = await _vendorPaymentService.GetPayablePostedVouchersAsync();

            ViewBag.Vendors = new SelectList(
                vendors,
                "VendorId",
                "VendorName");

            ViewBag.Vouchers = vouchers
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
                .ToList();
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

        #endregion
    }
}
