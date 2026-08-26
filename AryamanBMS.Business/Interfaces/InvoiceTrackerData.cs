using AryamanBMS.Models;

namespace AryamanBMS.Business.Interfaces;

public class InvoiceTrackerData
{
    public List<InvoiceModel> Invoices { get; init; } = [];
    public List<ClientModel> Clients { get; init; } = [];
    public List<int> AvailableYears { get; init; } = [];
    public int TotalInvoices { get; init; }
    public int DraftCount { get; init; }
    public int IssuedCount { get; init; }
    public int CancelledCount { get; init; }
    public int UnpaidCount { get; init; }
    public int PartiallyPaidCount { get; init; }
    public int PaidCount { get; init; }
    public decimal TotalInvoiceAmount { get; init; }
    public decimal TotalReceivedAmount { get; init; }
    public decimal TotalOutstandingAmount { get; init; }
}
