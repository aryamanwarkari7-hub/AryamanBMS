
using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IGstReturnRepository
    {
        Task<List<GstReturnModel>> GetAllAsync();

        Task<GstReturnModel?> GetByIdAsync(int id);

        Task<List<GstReturnModel>> GetBySnapshotAsync(int snapshotId);

        Task<GstReturnModel?> GetByReturnTypeAsync(int snapshotId, string returnType);

        Task AddAsync(GstReturnModel model);

        Task UpdateAsync(GstReturnModel model);

        Task DeleteAsync(GstReturnModel model);

        Task SaveAsync();
    }
}
