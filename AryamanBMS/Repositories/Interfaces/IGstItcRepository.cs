
using AryamanBMS.Models;


namespace AryamanBMS.Repositories.Interfaces
{
    public interface IGstItcRepository
    {
        Task<List<GstItcRecordModel>> GetAllAsync();

        Task<GstItcRecordModel?> GetByIdAsync(int id);

        Task<List<GstItcRecordModel>> GetBySnapshotAsync(int snapshotId);

        Task<List<GstItcRecordModel>> GetByVendorAsync(string vendorName);

        Task<decimal> GetTotalItcAsync(int snapshotId);

        Task AddAsync(GstItcRecordModel model);

        Task UpdateAsync(GstItcRecordModel model);

        Task DeleteAsync(GstItcRecordModel model);

        Task SaveAsync();
    }
}

