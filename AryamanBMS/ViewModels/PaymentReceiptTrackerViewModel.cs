using AryamanBMS.Models;

namespace AryamanBMS.ViewModels
{
    public class PaymentReceiptTrackerViewModel
    {
        public List<PaymentReceiptModel> PaymentReceipts { get; set; } = new();

        public PaymentSummary Summary { get; set; } = new();

        public PaymentFilter Filter { get; set; } = new();

        // ADD THIS HERE
        public List<ClientModel> Clients { get; set; } = new();
    }

    public class PaymentSummary
    {
        public int TotalReceipts { get; set; }

        public decimal TotalReceived { get; set; }

        public int ActiveReceiptCount { get; set; }

        public int CancelledReceiptCount { get; set; }

        
    }

    public class PaymentFilter
    {
        public int? ClientId { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public string Status { get; set; } = "all";

        public string? SearchTerm { get; set; }
    }
}