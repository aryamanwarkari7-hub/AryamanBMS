using AryamanBMS.Models;

namespace AryamanBMS.Business.Interfaces;

public interface IInvoiceQueryService
{
    Task<InvoiceTrackerData> GetTrackerAsync(
        string? search,
        int? clientId,
        string? invoiceStatus,
        string? paymentStatus,
        int? month,
        int? year);
}
