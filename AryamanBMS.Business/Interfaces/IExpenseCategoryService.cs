using AryamanBMS.Models;

namespace AryamanBMS.Business.Interfaces;

public interface IExpenseCategoryService
{
    Task<List<ExpenseCategoryModel>> GetActiveAsync(
        string? search,
        string sortBy,
        string sortOrder);

    Task<ExpenseCategoryModel?> GetActiveByIdAsync(int id);

    Task<IReadOnlyDictionary<string, string>> ValidateForCreateAsync(
        ExpenseCategoryModel category);

    Task<IReadOnlyDictionary<string, string>> ValidateForUpdateAsync(
        ExpenseCategoryModel category);

    Task CreateAsync(ExpenseCategoryModel category);

    Task<ExpenseCategoryModel?> UpdateAsync(ExpenseCategoryModel category);

    Task<ExpenseCategoryModel?> DeleteAsync(int id);
}
