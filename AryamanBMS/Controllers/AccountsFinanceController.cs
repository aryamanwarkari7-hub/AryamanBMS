using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers;

[Authorize(Roles = "Admin, Master")]
public class AccountsFinanceController : Controller
{
    private readonly IAccountsFinanceDashboardService _dashboardService;

    public AccountsFinanceController(
        IAccountsFinanceDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard(int? month, int? year)
    {
        var dashboard = await _dashboardService.GetAsync(month, year);

        return View(ToViewModel(dashboard));
    }

    private static AccountsFinanceDashboardViewModel ToViewModel(
        AccountsFinanceDashboardData dashboard)
    {
        return new AccountsFinanceDashboardViewModel
        {
            Month = dashboard.Month,
            Year = dashboard.Year,
            PeriodLabel = dashboard.PeriodLabel,
            TotalInvoiced = dashboard.TotalInvoiced,
            TotalCollected = dashboard.TotalCollected,
            OutstandingReceivables = dashboard.OutstandingReceivables,
            OverdueReceivables = dashboard.OverdueReceivables,
            DraftInvoices = dashboard.DraftInvoices,
            IssuedInvoices = dashboard.IssuedInvoices,
            OverdueInvoices = dashboard.OverdueInvoices,
            AdvanceBalance = dashboard.AdvanceBalance,
            PendingExpenses = dashboard.PendingExpenses,
            PaidExpenses = dashboard.PaidExpenses,
            ExpenseGSTInput = dashboard.ExpenseGSTInput,
            SalesGSTOutput = dashboard.SalesGSTOutput,
            NetGSTPayable = dashboard.NetGSTPayable,
            CreditNoteTotal = dashboard.CreditNoteTotal,
            DebitNoteTotal = dashboard.DebitNoteTotal,
            AssetCount = dashboard.AssetCount,
            AssetValue = dashboard.AssetValue,
            AssignedAssets = dashboard.AssignedAssets,
            IdleAssets = dashboard.IdleAssets,
            InvoiceStatusBuckets = ToViewModel(
                dashboard.InvoiceStatusBuckets),
            PaymentStatusBuckets = ToViewModel(
                dashboard.PaymentStatusBuckets),
            ExpenseStatusBuckets = ToViewModel(
                dashboard.ExpenseStatusBuckets),
            AssetStatusBuckets = ToViewModel(
                dashboard.AssetStatusBuckets),
            OverdueInvoiceList = ToViewModel(
                dashboard.OverdueInvoiceList),
            PendingExpenseList = ToViewModel(
                dashboard.PendingExpenseList),
            RecentReceipts = ToViewModel(dashboard.RecentReceipts),
            AdvanceReceiptList = ToViewModel(dashboard.AdvanceReceiptList)
        };
    }

    private static List<AccountsFinanceBucket> ToViewModel(
        IEnumerable<AccountsFinanceBucketData> buckets)
    {
        return buckets.Select(x => new AccountsFinanceBucket
        {
            Label = x.Label,
            Count = x.Count,
            Amount = x.Amount,
            Percent = x.Percent,
            CssClass = x.CssClass
        }).ToList();
    }

    private static List<AccountsFinanceListItem> ToViewModel(
        IEnumerable<AccountsFinanceListItemData> items)
    {
        return items.Select(x => new AccountsFinanceListItem
        {
            Id = x.Id,
            Title = x.Title,
            Subtitle = x.Subtitle,
            Meta = x.Meta,
            Badge = x.Badge,
            Amount = x.Amount
        }).ToList();
    }
}
