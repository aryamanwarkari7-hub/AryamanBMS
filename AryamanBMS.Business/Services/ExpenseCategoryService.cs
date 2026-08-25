using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Business.Services;

public class ExpenseCategoryService : IExpenseCategoryService
{
    private readonly IExpenseCategoryRepository _categoryRepository;

    public ExpenseCategoryService(IExpenseCategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<List<ExpenseCategoryModel>> GetActiveAsync(
        string? search,
        string sortBy,
        string sortOrder)
    {
        var categories = (await _categoryRepository.GetAllActiveAsync()).ToList();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim().ToLower();

            categories = categories
                .Where(x =>
                    (!string.IsNullOrWhiteSpace(x.CategoryCode) &&
                        x.CategoryCode.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(x.CategoryName) &&
                        x.CategoryName.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(x.Description) &&
                        x.Description.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(x.GLAccountCode) &&
                        x.GLAccountCode.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(x.ExpenseType) &&
                        x.ExpenseType.ToLower().Contains(keyword)))
                .ToList();
        }

        bool descending = string.Equals(
            sortOrder,
            "desc",
            StringComparison.OrdinalIgnoreCase);

        return sortBy switch
        {
            "CategoryCode" => descending
                ? categories.OrderByDescending(x => x.CategoryCode).ToList()
                : categories.OrderBy(x => x.CategoryCode).ToList(),
            "GSTRate" => descending
                ? categories.OrderByDescending(x => x.DefaultGSTRate).ToList()
                : categories.OrderBy(x => x.DefaultGSTRate).ToList(),
            "ITC" => descending
                ? categories.OrderByDescending(x => x.ITCEligible).ToList()
                : categories.OrderBy(x => x.ITCEligible).ToList(),
            "ExpenseType" => descending
                ? categories.OrderByDescending(x => x.ExpenseType).ToList()
                : categories.OrderBy(x => x.ExpenseType).ToList(),
            _ => descending
                ? categories.OrderByDescending(x => x.CategoryName).ToList()
                : categories.OrderBy(x => x.CategoryName).ToList()
        };
    }

    public async Task<ExpenseCategoryModel?> GetActiveByIdAsync(int id)
    {
        return await _categoryRepository.GetByIdAsync(id);
    }

    public async Task<IReadOnlyDictionary<string, string>> ValidateForCreateAsync(
        ExpenseCategoryModel category)
    {
        NormalizeForCreate(category);

        var errors = ValidateGstRate(category);

        if (await _categoryRepository.CategoryCodeExistsAsync(category.CategoryCode))
        {
            errors[nameof(category.CategoryCode)] =
                "This category code already exists.";
        }

        return errors;
    }

    public async Task<IReadOnlyDictionary<string, string>> ValidateForUpdateAsync(
        ExpenseCategoryModel category)
    {
        var errors = new Dictionary<string, string>();

        if (await _categoryRepository.CategoryCodeExistsAsync(
                category.CategoryCode,
                category.ExpenseCategoryId))
        {
            errors[nameof(category.CategoryCode)] =
                "This category code already exists.";
        }

        return errors;
    }

    public async Task CreateAsync(ExpenseCategoryModel category)
    {
        await _categoryRepository.AddAsync(category);
        await _categoryRepository.SaveAsync();
    }

    public async Task<ExpenseCategoryModel?> UpdateAsync(ExpenseCategoryModel category)
    {
        var existing = await _categoryRepository.GetByIdAsync(
            category.ExpenseCategoryId);

        if (existing == null)
        {
            return null;
        }

        existing.CategoryCode = category.CategoryCode;
        existing.CategoryName = category.CategoryName;
        existing.Description = category.Description;
        existing.DefaultGSTRate = category.DefaultGSTRate;
        existing.ITCEligible = category.ITCEligible;
        existing.GLAccountCode = category.GLAccountCode;
        existing.ExpenseType = category.ExpenseType;
        existing.PayableGLAccountCode = category.PayableGLAccountCode;
        existing.InputGSTGLAccountCode = category.InputGSTGLAccountCode;
        existing.IsCapitalExpense = category.IsCapitalExpense;

        await _categoryRepository.UpdateAsync(existing);
        await _categoryRepository.SaveAsync();

        return existing;
    }

    public async Task<ExpenseCategoryModel?> DeleteAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);

        if (category == null)
        {
            return null;
        }

        await _categoryRepository.SoftDeleteAsync(id);
        await _categoryRepository.SaveAsync();

        return category;
    }

    private static Dictionary<string, string> ValidateGstRate(
        ExpenseCategoryModel category)
    {
        var errors = new Dictionary<string, string>();

        if (category.DefaultGSTRate < 0 || category.DefaultGSTRate > 100)
        {
            errors[nameof(category.DefaultGSTRate)] =
                "GST rate must be between 0 and 100.";
        }

        return errors;
    }

    private static void NormalizeForCreate(ExpenseCategoryModel category)
    {
        category.CategoryCode =
            (category.CategoryCode ?? string.Empty).Trim().ToUpperInvariant();
        category.CategoryName = (category.CategoryName ?? string.Empty).Trim();
    }
}
