using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface ICompanyProfileRepository
    {
        Task<CompanyProfileModel?> GetActiveAsync();

        Task<bool> ExistsAsync();

        Task AddAsync(CompanyProfileModel model);

        Task UpdateAsync(CompanyProfileModel model);

        Task SaveAsync();
    }
}