using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IEsicRepository
    {
        // Snapshot
        Task<List<EsicMonthlySnapshotModel>> GetAllSnapshotsAsync();

        Task<EsicMonthlySnapshotModel?> GetSnapshotByIdAsync(int id);

        Task<EsicMonthlySnapshotModel?> GetSnapshotByMonthYearAsync(int month, int year);

        Task<EsicMonthlySnapshotModel> GenerateSnapshotAsync(int month, int year);

        Task UpdateSnapshotStatusAsync(int snapshotId, string status);

        // Challan
        Task AddChallanAsync(EsicChallanModel challan);

        Task UpdateChallanAsync(EsicChallanModel challan);

        Task<EsicChallanModel?> GetChallanByIdAsync(int id);

        // Document
        Task AddDocumentAsync(EsicDocumentModel document);

        Task<EsicDocumentModel?> GetDocumentByIdAsync(int id);

        Task DeleteDocumentAsync(int id);

        Task SaveAsync();
    }
}