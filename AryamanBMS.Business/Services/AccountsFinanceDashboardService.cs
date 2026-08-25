using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Business.Services;

public class AccountsFinanceDashboardService
    : IAccountsFinanceDashboardService
{
    private readonly IAccountsFinanceDashboardRepository _dashboardRepository;

    public AccountsFinanceDashboardService(
        IAccountsFinanceDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    public async Task<AccountsFinanceDashboardData> GetAsync(
        int? month,
        int? year)
    {
        var today = DateTime.Today;
        int selectedMonth = month.HasValue && month.Value >= 1 && month.Value <= 12
            ? month.Value
            : today.Month;
        int selectedYear = year ?? today.Year;
        var monthStart = new DateTime(selectedYear, selectedMonth, 1);
        var nextMonth = monthStart.AddMonths(1);

        var snapshot = await _dashboardRepository.GetSnapshotAsync();
        var invoices = snapshot.Invoices;
        var receipts = snapshot.Receipts;
        var advances = snapshot.Advances;
        var expenses = snapshot.Expenses;
        var assets = snapshot.Assets;

        var monthInvoices = invoices
            .Where(x => x.InvoiceDate >= monthStart && x.InvoiceDate < nextMonth)
            .ToList();
        var monthReceipts = receipts
            .Where(x => x.ReceiptDate >= monthStart && x.ReceiptDate < nextMonth)
            .ToList();
        var monthExpenses = expenses
            .Where(x => x.VoucherDate >= monthStart && x.VoucherDate < nextMonth)
            .ToList();
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

        return new AccountsFinanceDashboardData
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
            CreditNoteTotal = snapshot.CreditNotes.Sum(x => x.TotalCredit),
            DebitNoteTotal = snapshot.DebitNotes.Sum(x => x.TotalDebit),
            AssetCount = assets.Count,
            AssetValue = assets.Sum(x =>
                x.WrittenDownValue > 0 ? x.WrittenDownValue : x.PurchaseValue),
            AssignedAssets = assets.Count(x => x.AssignedEmployeeId.HasValue),
            IdleAssets = assets.Count(x =>
                string.Equals(x.Status, "Idle", StringComparison.OrdinalIgnoreCase)),
            InvoiceStatusBuckets = BuildBuckets(
                invoices
                    .GroupBy(x => x.InvoiceStatus ?? "Unknown")
                    .Select(x => CreateBucket(x.Key, x.Count(), x.Sum(y => y.GrandTotal)))
                    .OrderByDescending(x => x.Count)
                    .ToList()),
            PaymentStatusBuckets = BuildBuckets(
                invoices
                    .GroupBy(x => x.PaymentStatus ?? "Unknown")
                    .Select(x => CreateBucket(x.Key, x.Count(), x.Sum(y => y.BalanceAmount)))
                    .OrderByDescending(x => x.Count)
                    .ToList()),
            ExpenseStatusBuckets = BuildBuckets(
                expenses
                    .GroupBy(x => x.PaymentStatus ?? "Unknown")
                    .Select(x => CreateBucket(x.Key, x.Count(), x.Sum(y => y.BalanceAmount)))
                    .OrderByDescending(x => x.Count)
                    .ToList()),
            AssetStatusBuckets = BuildBuckets(
                assets
                    .GroupBy(x => x.Status ?? "Unknown")
                    .Select(x => CreateBucket(x.Key, x.Count(), x.Sum(y => y.PurchaseValue)))
                    .OrderByDescending(x => x.Count)
                    .ToList()),
            OverdueInvoiceList = overdueInvoices
                .OrderBy(x => x.DueDate)
                .Take(8)
                .Select(x => new AccountsFinanceListItemData
                {
                    Id = x.InvoiceId,
                    Title = x.InvoiceNo,
                    Subtitle = x.Client?.ClientName,
                    Meta = x.DueDate?.ToString("dd-MMM-yyyy"),
                    Badge = x.PaymentStatus,
                    Amount = x.BalanceAmount
                })
                .ToList(),
            PendingExpenseList = pendingExpenses
                .OrderByDescending(x => x.BalanceAmount)
                .Take(8)
                .Select(x => new AccountsFinanceListItemData
                {
                    Id = x.ExpenseVoucherId,
                    Title = x.VoucherNumber,
                    Subtitle = x.Category?.CategoryName ?? x.VendorName,
                    Meta = x.VoucherDate.ToString("dd-MMM-yyyy"),
                    Badge = x.PaymentStatus,
                    Amount = x.BalanceAmount
                })
                .ToList(),
            RecentReceipts = receipts
                .OrderByDescending(x => x.ReceiptDate)
                .Take(8)
                .Select(x => new AccountsFinanceListItemData
                {
                    Id = x.PaymentReceiptId,
                    Title = x.ReceiptNo,
                    Subtitle = x.Client?.ClientName,
                    Meta = x.ReceiptDate.ToString("dd-MMM-yyyy"),
                    Badge = x.PaymentMode,
                    Amount = x.AmountReceived
                })
                .ToList(),
            AdvanceReceiptList = advances
                .Where(x => x.AvailableBalance > 0)
                .OrderByDescending(x => x.ReceiptDate)
                .Take(8)
                .Select(x => new AccountsFinanceListItemData
                {
                    Id = x.AdvanceReceiptId,
                    Title = x.AdvanceReceiptNo,
                    Subtitle = x.Client?.ClientName,
                    Meta = x.ReceiptDate.ToString("dd-MMM-yyyy"),
                    Badge = x.PaymentMode,
                    Amount = x.AvailableBalance
                })
                .ToList()
        };
    }

    private static List<AccountsFinanceBucketData> BuildBuckets(
        List<AccountsFinanceBucketData> buckets)
    {
        int total = buckets.Sum(x => x.Count);

        return buckets
            .Select(x => new AccountsFinanceBucketData
            {
                Label = x.Label,
                Count = x.Count,
                Amount = x.Amount,
                Percent = total == 0
                    ? 0
                    : Math.Round((decimal)x.Count / total * 100, 2),
                CssClass = GetBucketClass(x.Label)
            })
            .ToList();
    }

    private static AccountsFinanceBucketData CreateBucket(
        string label,
        int count,
        decimal amount)
    {
        return new AccountsFinanceBucketData
        {
            Label = label,
            Count = count,
            Amount = amount,
            CssClass = GetBucketClass(label)
        };
    }

    private static string GetBucketClass(string value)
    {
        return value switch
        {
            "Paid" or "Issued" or "Approved" or "InUse" => "bucket-success",
            "Partially Paid" => "bucket-info",
            "Draft" => "bucket-neutral",
            "Unpaid" or "Pending" or "Idle" => "bucket-warning",
            "Cancelled" or "Rejected" or "UnderRepair" => "bucket-danger",
            _ => "bucket-info"
        };
    }
}
