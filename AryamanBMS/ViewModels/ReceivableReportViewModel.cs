namespace AryamanBMS.ViewModels
{
    public class ReceivablesReportViewModel
    {
        public decimal TotalInvoiced { get; set; }

        public decimal TotalCollected { get; set; }

        public decimal TotalOutstanding { get; set; }

        public decimal OverdueAmount { get; set; }

        public List<ClientReceivableRowViewModel> ClientRows { get; set; } = new();

        public List<ProjectReceivableRowViewModel> ProjectRows { get; set; } = new();

        public List<InvoiceReceivableRowViewModel> InvoiceRows { get; set; } = new();
    }

    public class ClientReceivableRowViewModel
    {
        public int ClientId { get; set; }

        public string ClientName { get; set; } = string.Empty;

        public decimal TotalInvoiced { get; set; }

        public decimal TotalCollected { get; set; }

        public decimal Outstanding { get; set; }

        public decimal Overdue { get; set; }
    }

    public class ProjectReceivableRowViewModel
    {
        public int? ProjectId { get; set; }

        public string ProjectName { get; set; } = string.Empty;

        public decimal TotalInvoiced { get; set; }

        public decimal TotalCollected { get; set; }

        public decimal Outstanding { get; set; }

        public decimal Overdue { get; set; }
    }

    public class InvoiceReceivableRowViewModel
    {
        public int InvoiceId { get; set; }

        public string InvoiceNo { get; set; } = string.Empty;

        public string ClientName { get; set; } = string.Empty;

        public string ProjectName { get; set; } = string.Empty;

        public DateTime InvoiceDate { get; set; }

        public DateTime? DueDate { get; set; }

        public decimal InvoiceTotal { get; set; }

        public decimal AmountReceived { get; set; }

        public decimal OutstandingBalance { get; set; }

        public int AgeingDays { get; set; }

        public string PaymentStatus { get; set; } = string.Empty;
    }
}