using AryamanBMS.Models;

namespace AryamanBMS.Business.Interfaces;

public interface IExpenseVoucherTrackerService
{
    Task<ExpenseVoucherTrackerData> GetTrackerAsync(
        string? status,
        int? categoryId,
        string? search,
        DateTime? fromDate,
        DateTime? toDate,
        string sortBy,
        string sortOrder,
        int page,
        string currentUserId,
        bool restrictToCurrentUser);

    Task<List<ExpenseVoucherModel>> GetForExportAsync(
        string? status,
        int? categoryId,
        string? search,
        DateTime? fromDate,
        DateTime? toDate,
        string currentUserId,
        bool restrictToCurrentUser);
}
