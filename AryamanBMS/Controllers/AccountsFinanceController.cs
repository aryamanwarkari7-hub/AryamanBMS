using AryamanBMS.Data;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AccountsFinanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountsFinanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard(int? month, int? year)
        {
            var today = DateTime.Today;

            int selectedMonth =
                month.HasValue &&
                month.Value >= 1 &&
                month.Value <= 12
                    ? month.Value
                    : today.Month;

            int selectedYear = year ?? today.Year;

            var monthStart = new DateTime(selectedYear, selectedMonth, 1);
            var nextMonth = monthStart.AddMonths(1);

            var invoices = await _context.Invoices
                .AsNoTracking()
                .Include(x => x.Client)
                .Where(x => !x.IsDeleted)
                .ToListAsync();

            var monthInvoices = invoices
                .Where(x =>
                    x.InvoiceDate >= monthStart &&
                    x.InvoiceDate < nextMonth)
                .ToList();

            var receipts = await _context.PaymentReceipts
                .AsNoTracking()
                .Include(x => x.Client)
                .Include(x => x.Invoice)
                .Where(x => x.IsActive && !x.IsCancelled)
                .ToListAsync();

            var monthReceipts = receipts
                .Where(x =>
                    x.ReceiptDate >= monthStart &&
                    x.ReceiptDate < nextMonth)
                .ToList();

            var advances = await _context.AdvanceReceipts
                .AsNoTracking()
                .Include(x => x.Client)
                .Where(x => !x.IsCancelled)
                .ToListAsync();

            var expenses = await _context.ExpenseVouchers
                .AsNoTracking()
                .Include(x => x.Category)
                .Where(x => x.IsActive && !x.IsReversed)
                .ToListAsync();

            var monthExpenses = expenses
                .Where(x =>
                    x.VoucherDate >= monthStart &&
                    x.VoucherDate < nextMonth)
                .ToList();

            var creditNotes = await _context.CreditNotes
                .AsNoTracking()
                .Where(x => !x.IsCancelled)
                .ToListAsync();

            var debitNotes = await _context.DebitNotes
                .AsNoTracking()
                .Where(x => !x.IsCancelled)
                .ToListAsync();

            var assets = await _context.OfficeAssets
                .AsNoTracking()
                .Where(x => x.IsActive)
                .ToListAsync();

            var overdueInvoices = invoices
                .Where(x =>
                    x.DueDate.HasValue &&
                    x.DueDate.Value.Date < today &&
                    !string.Equals(x.InvoiceStatus, "Cancelled", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(x.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase) &&
                    x.BalanceAmount > 0)
                .ToList();

            var pendingExpenses = expenses
                .Where(x =>
                    !string.Equals(x.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase) &&
                    x.BalanceAmount > 0)
                .ToList();

            decimal salesGstOutput = monthInvoices
                .Where(x => !string.Equals(x.InvoiceStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
                .Sum(x => x.GSTAmount);

            decimal expenseGstInput = monthExpenses
                .Where(x => x.ITCEligible)
                .Sum(x => x.TotalGSTAmount);

            var model = new AccountsFinanceDashboardViewModel
            {
                Month = selectedMonth,
                Year = selectedYear,
                PeriodLabel = monthStart.ToString("MMMM yyyy"),

                TotalInvoiced = monthInvoices
                    .Where(x => !string.Equals(x.InvoiceStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
                    .Sum(x => x.GrandTotal),

                TotalCollected = monthReceipts.Sum(x => x.AmountReceived),

                OutstandingReceivables = invoices
                    .Where(x => !string.Equals(x.InvoiceStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
                    .Sum(x => x.BalanceAmount),

                OverdueReceivables = overdueInvoices.Sum(x => x.BalanceAmount),

                DraftInvoices = invoices.Count(x =>
                    string.Equals(x.InvoiceStatus, "Draft", StringComparison.OrdinalIgnoreCase)),

                IssuedInvoices = invoices.Count(x =>
                    string.Equals(x.InvoiceStatus, "Issued", StringComparison.OrdinalIgnoreCase)),

                OverdueInvoices = overdueInvoices.Count,

                AdvanceBalance = advances.Sum(x => x.AvailableBalance),

                PendingExpenses = pendingExpenses.Sum(x => x.BalanceAmount),

                PaidExpenses = monthExpenses.Sum(x => x.PaidAmount),

                ExpenseGSTInput = expenseGstInput,
                SalesGSTOutput = salesGstOutput,
                NetGSTPayable = salesGstOutput - expenseGstInput,

                CreditNoteTotal = creditNotes.Sum(x => x.TotalCredit),
                DebitNoteTotal = debitNotes.Sum(x => x.TotalDebit),

                AssetCount = assets.Count,
                AssetValue = assets.Sum(x => x.WrittenDownValue > 0 ? x.WrittenDownValue : x.PurchaseValue),
                AssignedAssets = assets.Count(x => x.AssignedEmployeeId.HasValue),
                IdleAssets = assets.Count(x =>
                    string.Equals(x.Status, "Idle", StringComparison.OrdinalIgnoreCase))
            };

            model.InvoiceStatusBuckets = BuildAccountsBuckets(
                invoices
                    .GroupBy(x => x.InvoiceStatus ?? "Unknown")
                    .Select(x => new AccountsFinanceBucket
                    {
                        Label = x.Key,
                        Count = x.Count(),
                        Amount = x.Sum(y => y.GrandTotal),
                        CssClass = GetAccountsBucketClass(x.Key)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList());

            model.PaymentStatusBuckets = BuildAccountsBuckets(
                invoices
                    .GroupBy(x => x.PaymentStatus ?? "Unknown")
                    .Select(x => new AccountsFinanceBucket
                    {
                        Label = x.Key,
                        Count = x.Count(),
                        Amount = x.Sum(y => y.BalanceAmount),
                        CssClass = GetAccountsBucketClass(x.Key)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList());

            model.ExpenseStatusBuckets = BuildAccountsBuckets(
                expenses
                    .GroupBy(x => x.PaymentStatus ?? "Unknown")
                    .Select(x => new AccountsFinanceBucket
                    {
                        Label = x.Key,
                        Count = x.Count(),
                        Amount = x.Sum(y => y.BalanceAmount),
                        CssClass = GetAccountsBucketClass(x.Key)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList());

            model.AssetStatusBuckets = BuildAccountsBuckets(
                assets
                    .GroupBy(x => x.Status ?? "Unknown")
                    .Select(x => new AccountsFinanceBucket
                    {
                        Label = x.Key,
                        Count = x.Count(),
                        Amount = x.Sum(y => y.PurchaseValue),
                        CssClass = GetAccountsBucketClass(x.Key)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList());

            model.OverdueInvoiceList = overdueInvoices
                .OrderBy(x => x.DueDate)
                .Take(8)
                .Select(x => new AccountsFinanceListItem
                {
                    Id = x.InvoiceId,
                    Title = x.InvoiceNo,
                    Subtitle = x.Client?.ClientName,
                    Meta = x.DueDate?.ToString("dd-MMM-yyyy"),
                    Badge = x.PaymentStatus,
                    Amount = x.BalanceAmount
                })
                .ToList();

            model.PendingExpenseList = pendingExpenses
                .OrderByDescending(x => x.BalanceAmount)
                .Take(8)
                .Select(x => new AccountsFinanceListItem
                {
                    Id = x.ExpenseVoucherId,
                    Title = x.VoucherNumber,
                    Subtitle = x.Category?.CategoryName ?? x.VendorName,
                    Meta = x.VoucherDate.ToString("dd-MMM-yyyy"),
                    Badge = x.PaymentStatus,
                    Amount = x.BalanceAmount
                })
                .ToList();

            model.RecentReceipts = receipts
                .OrderByDescending(x => x.ReceiptDate)
                .Take(8)
                .Select(x => new AccountsFinanceListItem
                {
                    Id = x.PaymentReceiptId,
                    Title = x.ReceiptNo,
                    Subtitle = x.Client?.ClientName,
                    Meta = x.ReceiptDate.ToString("dd-MMM-yyyy"),
                    Badge = x.PaymentMode,
                    Amount = x.AmountReceived
                })
                .ToList();

            model.AdvanceReceiptList = advances
                .Where(x => x.AvailableBalance > 0)
                .OrderByDescending(x => x.ReceiptDate)
                .Take(8)
                .Select(x => new AccountsFinanceListItem
                {
                    Id = x.AdvanceReceiptId,
                    Title = x.AdvanceReceiptNo,
                    Subtitle = x.Client?.ClientName,
                    Meta = x.ReceiptDate.ToString("dd-MMM-yyyy"),
                    Badge = x.PaymentMode,
                    Amount = x.AvailableBalance
                })
                .ToList();

            return View(model);
        }

        private static List<AccountsFinanceBucket> BuildAccountsBuckets(
            List<AccountsFinanceBucket> buckets)
        {
            int total = buckets.Sum(x => x.Count);

            foreach (var bucket in buckets)
            {
                bucket.Percent = total == 0
                    ? 0
                    : Math.Round((decimal)bucket.Count / total * 100, 2);
            }

            return buckets;
        }

        private static string GetAccountsBucketClass(string value)
        {
            return value switch
            {
                "Paid" => "bucket-success",
                "Issued" => "bucket-success",
                "Approved" => "bucket-success",
                "InUse" => "bucket-success",
                "Partially Paid" => "bucket-info",
                "Draft" => "bucket-neutral",
                "Unpaid" => "bucket-warning",
                "Pending" => "bucket-warning",
                "Idle" => "bucket-warning",
                "Cancelled" => "bucket-danger",
                "Rejected" => "bucket-danger",
                "UnderRepair" => "bucket-danger",
                _ => "bucket-info"
            };
        }
    }
}