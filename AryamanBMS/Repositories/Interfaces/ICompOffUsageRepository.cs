using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface ICompOffUsageRepository
    {
        IQueryable<CompOffUsageModel> CompOffUsages { get; }

        Task AddAsync(CompOffUsageModel usage);

        Task UpdateAsync(CompOffUsageModel usage);

        Task SaveAsync();
    }
}