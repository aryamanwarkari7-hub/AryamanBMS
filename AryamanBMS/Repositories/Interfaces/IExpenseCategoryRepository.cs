using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IExpenseCategoryRepository
    {
        Task<IEnumerable<ExpenseCategoryModel>> GetAllActiveAsync();

        Task<ExpenseCategoryModel?> GetByIdAsync(int id);

        Task<ExpenseCategoryModel?> GetByCategoryCodeAsync(string code);

        Task<bool> CategoryCodeExistsAsync(string code, int? excludeId = null);

        Task AddAsync(ExpenseCategoryModel model);

        Task UpdateAsync(ExpenseCategoryModel model);

        Task SoftDeleteAsync(int id);

        Task SaveAsync();
    }
}