
using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IGstChallanRepository
    {
        Task<List<GstChallanModel>> GetAllAsync();

        Task<GstChallanModel?> GetByIdAsync(int id);

        Task<List<GstChallanModel>> GetBySnapshotAsync(int snapshotId);

        Task<GstChallanModel?> GetByChallanNumberAsync(string challanNumber);

        Task AddAsync(GstChallanModel model);

        Task UpdateAsync(GstChallanModel model);

        Task DeleteAsync(GstChallanModel model);

        Task SaveAsync();
    }
}
