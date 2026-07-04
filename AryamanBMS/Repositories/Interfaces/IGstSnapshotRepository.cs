
using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IGstSnapshotRepository
    {
        Task<List<GstMonthlySnapshotModel>> GetAllAsync();

        Task<GstMonthlySnapshotModel?> GetByIdAsync(int id);

        Task<GstMonthlySnapshotModel?> GetByMonthYearAsync(int month, int year);

        Task AddAsync(GstMonthlySnapshotModel snapshot);

        Task UpdateAsync(GstMonthlySnapshotModel snapshot);

        Task DeleteAsync(GstMonthlySnapshotModel snapshot);


        Task<bool> LockAsync(
          int month,
          int year,
          string filedByUserId);

        Task<bool> ReopenAsync(
          int month,
          int year,
          string reopenedByUserId,
          string reason);

        Task SaveAsync();

        
    }
}

