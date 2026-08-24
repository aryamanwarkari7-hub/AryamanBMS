using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IFinancialAuditDocumentRepository
    {
        Task<List<FinancialAuditDocumentModel>> GetAllAsync();

        Task<FinancialAuditDocumentModel?> GetByIdAsync(int id);

        Task<List<FinancialAuditDocumentModel>> GetByFinancialYearAsync(string financialYear);

        Task<List<FinancialAuditDocumentModel>> GetByCategoryAsync(string documentCategory);

        Task<bool> ActiveDuplicateExistsAsync(
            string documentCategory,
            string financialYear,
            string fileName,
            int? excludeId = null);

        Task AddAsync(FinancialAuditDocumentModel model);

        Task UpdateAsync(FinancialAuditDocumentModel model);

        Task SaveAsync();
    }
}
