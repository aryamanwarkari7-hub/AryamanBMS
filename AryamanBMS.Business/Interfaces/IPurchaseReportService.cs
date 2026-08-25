using AryamanBMS.Models;

namespace AryamanBMS.Business.Interfaces;

public interface IPurchaseReportService
{
    Task<PurchaseReportData> GetAsync(
        string? search,
        string sortBy,
        string sortOrder);
}
