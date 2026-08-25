using AryamanBMS.Models;

namespace AryamanBMS.Business.Interfaces;

public interface IPaymentReceiptTrackerService
{
    Task<PaymentReceiptTrackerData> GetTrackerAsync(
        string? search,
        int? clientId,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        string sortBy,
        string sortOrder,
        int page);

    Task<List<PaymentReceiptModel>> GetForExportAsync(
        string? search,
        int? clientId,
        string? status,
        DateTime? fromDate,
        DateTime? toDate);
}
