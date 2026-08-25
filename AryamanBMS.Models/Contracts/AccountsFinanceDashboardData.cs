namespace AryamanBMS.Models;

public class AccountsFinanceDashboardSnapshot
{
    public List<InvoiceModel> Invoices { get; init; } = [];

    public List<PaymentReceiptModel> Receipts { get; init; } = [];

    public List<AdvanceReceiptModel> Advances { get; init; } = [];

    public List<ExpenseVoucherModel> Expenses { get; init; } = [];

    public List<CreditNoteModel> CreditNotes { get; init; } = [];

    public List<DebitNoteModel> DebitNotes { get; init; } = [];

    public List<OfficeAssetModel> Assets { get; init; } = [];
}

public class AccountsFinanceDashboardData
{
    public int Month { get; init; }
    public int Year { get; init; }
    public string PeriodLabel { get; init; } = string.Empty;
    public decimal TotalInvoiced { get; init; }
    public decimal TotalCollected { get; init; }
    public decimal OutstandingReceivables { get; init; }
    public decimal OverdueReceivables { get; init; }
    public int DraftInvoices { get; init; }
    public int IssuedInvoices { get; init; }
    public int OverdueInvoices { get; init; }
    public decimal AdvanceBalance { get; init; }
    public decimal PendingExpenses { get; init; }
    public decimal PaidExpenses { get; init; }
    public decimal ExpenseGSTInput { get; init; }
    public decimal SalesGSTOutput { get; init; }
    public decimal NetGSTPayable { get; init; }
    public decimal CreditNoteTotal { get; init; }
    public decimal DebitNoteTotal { get; init; }
    public int AssetCount { get; init; }
    public decimal AssetValue { get; init; }
    public int AssignedAssets { get; init; }
    public int IdleAssets { get; init; }
    public List<AccountsFinanceBucketData> InvoiceStatusBuckets { get; init; } = [];
    public List<AccountsFinanceBucketData> PaymentStatusBuckets { get; init; } = [];
    public List<AccountsFinanceBucketData> ExpenseStatusBuckets { get; init; } = [];
    public List<AccountsFinanceBucketData> AssetStatusBuckets { get; init; } = [];
    public List<AccountsFinanceListItemData> OverdueInvoiceList { get; init; } = [];
    public List<AccountsFinanceListItemData> PendingExpenseList { get; init; } = [];
    public List<AccountsFinanceListItemData> RecentReceipts { get; init; } = [];
    public List<AccountsFinanceListItemData> AdvanceReceiptList { get; init; } = [];
}

public class AccountsFinanceBucketData
{
    public string Label { get; init; } = string.Empty;
    public int Count { get; init; }
    public decimal Amount { get; init; }
    public decimal Percent { get; init; }
    public string CssClass { get; init; } = "bucket-info";
}

public class AccountsFinanceListItemData
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Subtitle { get; init; }
    public string? Meta { get; init; }
    public string? Badge { get; init; }
    public decimal Amount { get; init; }
}
