using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface ICompOffCreditRepository
    {
        IQueryable<CompOffCreditModel> CompOffCredits { get; }

        Task<CompOffCreditModel?> GetByIdAsync(int id);

        Task AddAsync(CompOffCreditModel compOffCredit);

        Task UpdateAsync(CompOffCreditModel compOffCredit);

        Task DeleteAsync(CompOffCreditModel compOffCredit);

        Task SaveAsync();
    }
}