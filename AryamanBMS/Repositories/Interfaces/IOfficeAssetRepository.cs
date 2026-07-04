using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IOfficeAssetRepository
    {
        Task<List<OfficeAssetModel>> GetAllAsync();

        Task<OfficeAssetModel?> GetByIdAsync(int id);

        Task<bool> AssetCodeExistsAsync(string assetCode, int? excludeId = null);

        Task<List<OfficeAssetModel>> GetByFinancialYearAsync(string financialYear);

        Task<List<OfficeAssetModel>> GetByCategoryAsync(string assetCategory);

        Task<List<OfficeAssetModel>> GetByStatusAsync(string status);

        Task<List<EmployeeModel>> GetActiveEmployeesAsync();

        Task<OfficeAssetAssignmentHistoryModel?> GetActiveAssignmentAsync(int officeAssetId);

        Task<List<OfficeAssetAssignmentHistoryModel>> GetAssignmentHistoryAsync(int officeAssetId);

        Task AddAsync(OfficeAssetModel model);

        Task UpdateAsync(OfficeAssetModel model);

        Task AssignAsync(
            int officeAssetId,
            int employeeId,
            string assignedByUserId,
            string? conditionOnAssignment,
            string? remarks);

        Task ReturnAsync(
            int officeAssetId,
            string returnedByUserId,
            string? conditionOnReturn,
            string? remarks);

        Task SaveAsync();
    }
}
