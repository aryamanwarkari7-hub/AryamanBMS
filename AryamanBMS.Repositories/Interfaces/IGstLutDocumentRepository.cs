using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IGstLutDocumentRepository
    {
        Task<List<GstLutDocumentModel>> GetActiveByConfigurationIdAsync(
            int gstConfigurationId);

        Task<GstLutDocumentModel?> GetActiveByIdAsync(int id);

        Task DeactivateActiveByConfigurationIdAsync(
            int gstConfigurationId);

        Task AddAsync(GstLutDocumentModel document);

        Task SaveAsync();
    }
}