namespace AryamanBMS.Models
{
    public class InvoiceTrackerViewModel
    {
        public List<InvoiceModel> Invoices { get; set; } = new();

        public List<ClientModel> Clients { get; set; } = new();

        public int TotalInvoices { get; set; }

        public int DraftCount { get; set; }

        public int IssuedCount { get; set; }

        public int CancelledCount { get; set; }

        public int UnpaidCount { get; set; }

        public int PartiallyPaidCount { get; set; }

        public int PaidCount { get; set; }

        public decimal TotalInvoiceAmount { get; set; }

        public decimal TotalReceivedAmount { get; set; }

        public decimal TotalOutstandingAmount { get; set; }
    }
}