namespace AryamanBMS.ViewModels
{
    public class GstDashboardViewModel
    {
        public int Month { get; set; }

        public int Year { get; set; }

        public string FinancialYear { get; set; } = "";

        public string SnapshotStatus { get; set; } = "Pending";

        public decimal SalesTaxable { get; set; }

        public decimal OutputGST { get; set; }

        public decimal InputGST { get; set; }

        public decimal NetGSTPayable { get; set; }

        public int InvoiceCount { get; set; }

        public int ExpenseVoucherCount { get; set; }

        public string Gstr1Status { get; set; } = "Pending";

        public string Gstr3BStatus { get; set; } = "Pending";

        public string ChallanStatus { get; set; } = "Pending";
    }
}