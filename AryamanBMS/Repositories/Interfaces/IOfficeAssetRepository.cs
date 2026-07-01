using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IOfficeAssetRepository
    {
        Task<List<OfficeAssetModel>> GetAllAsync();

        Task<OfficeAssetModel?> GetByIdAsync(int id);

        Task<List<OfficeAssetModel>> GetByFinancialYearAsync(string financialYear);

        Task<List<OfficeAssetModel>> GetByCategoryAsync(string assetCategory);

        Task<List<OfficeAssetModel>> GetByStatusAsync(string status);

        Task AddAsync(OfficeAssetModel model);

        Task UpdateAsync(OfficeAssetModel model);

        Task DeleteAsync(OfficeAssetModel model);

        Task SaveAsync();
    }
}