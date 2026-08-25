using AryamanBMS.Models;

namespace AryamanBMS.Business.Interfaces;

public interface IReceivablesReportService
{
    Task<ReceivablesReportData> GetReceivablesAsync();

    Task<InvoiceAgeingReportData> GetAgeingAsync();
}
