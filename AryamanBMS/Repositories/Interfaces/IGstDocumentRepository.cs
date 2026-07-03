using AryamanBMS.Models;


namespace AryamanBMS.Repositories.Interfaces
{
    public interface IGstDocumentRepository
    {
        Task<List<GstDocumentModel>> GetAllAsync();

        Task<GstDocumentModel?> GetByIdAsync(int id);

        Task<List<GstDocumentModel>> GetBySnapshotAsync(int snapshotId);

        Task<List<GstDocumentModel>> GetByDocumentTypeAsync(string documentType);

        Task AddAsync(GstDocumentModel model);

        Task UpdateAsync(GstDocumentModel model);

        Task DeleteAsync(GstDocumentModel model);

        Task SaveAsync();
    }
}
