using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface ICompanyDocumentCategoryRepository
    {
        Task<List<CompanyDocumentCategoryModel>> GetAllAsync();

        Task<CompanyDocumentCategoryModel?> GetByIdAsync(int id);

        Task AddAsync(CompanyDocumentCategoryModel category);

        Task UpdateAsync(CompanyDocumentCategoryModel category);

        Task DeleteAsync(CompanyDocumentCategoryModel category);

        Task<bool> IsCategoryInUseAsync(int categoryId);

        Task SaveAsync();
    }
}