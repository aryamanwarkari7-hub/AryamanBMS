using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IOffDayWorkRequestRepository
    {
        IQueryable<OffDayWorkRequestModel> Requests { get; }

        Task<OffDayWorkRequestModel?> GetByIdAsync(int id);

        Task AddAsync(OffDayWorkRequestModel request);

        Task UpdateAsync(OffDayWorkRequestModel request);

        Task DeleteAsync(OffDayWorkRequestModel request);

        Task SaveAsync();
    }
}