namespace AryamanBMS.ViewModels
{
    public class AccountsFinanceDashboardViewModel
    {
        public int Month { get; set; }

        public int Year { get; set; }

        public string PeriodLabel { get; set; } = string.Empty;

        public decimal TotalInvoiced { get; set; }

        public decimal TotalCollected { get; set; }

        public decimal OutstandingReceivables { get; set; }

        public decimal OverdueReceivables { get; set; }

        public int DraftInvoices { get; set; }

        public int IssuedInvoices { get; set; }

        public int OverdueInvoices { get; set; }

        public decimal AdvanceBalance { get; set; }

        public decimal PendingExpenses { get; set; }

        public decimal PaidExpenses { get; set; }

        public decimal ExpenseGSTInput { get; set; }

        public decimal SalesGSTOutput { get; set; }

        public decimal NetGSTPayable { get; set; }

        public decimal CreditNoteTotal { get; set; }

        public decimal DebitNoteTotal { get; set; }

        public int AssetCount { get; set; }

        public decimal AssetValue { get; set; }

        public int AssignedAssets { get; set; }

        public int IdleAssets { get; set; }

        public List<AccountsFinanceBucket> InvoiceStatusBuckets { get; set; } = new();

        public List<AccountsFinanceBucket> PaymentStatusBuckets { get; set; } = new();

        public List<AccountsFinanceBucket> ExpenseStatusBuckets { get; set; } = new();

        public List<AccountsFinanceBucket> AssetStatusBuckets { get; set; } = new();

        public List<AccountsFinanceListItem> OverdueInvoiceList { get; set; } = new();

        public List<AccountsFinanceListItem> PendingExpenseList { get; set; } = new();

        public List<AccountsFinanceListItem> RecentReceipts { get; set; } = new();

        public List<AccountsFinanceListItem> AdvanceReceiptList { get; set; } = new();
    }

    public class AccountsFinanceBucket
    {
        public string Label { get; set; } = string.Empty;

        public int Count { get; set; }

        public decimal Amount { get; set; }

        public decimal Percent { get; set; }

        public string CssClass { get; set; } = "bucket-info";
    }

    public class AccountsFinanceListItem
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Subtitle { get; set; }

        public string? Meta { get; set; }

        public string? Badge { get; set; }

        public decimal Amount { get; set; }
    }
}