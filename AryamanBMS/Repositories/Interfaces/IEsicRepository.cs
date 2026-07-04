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

        Task<bool> MarkFiledAsync(
            int snapshotId,
            string filedByUserId);

        Task<bool> MarkPaidAsync(
            int snapshotId,
            string paidByUserId);

        // Challan
        Task AddChallanAsync(EsicChallanModel challan);

        Task UpdateChallanAsync(EsicChallanModel challan);

        Task<EsicChallanModel?> GetChallanByIdAsync(int id);

        // Document
        Task AddDocumentAsync(EsicDocumentModel document);

        Task<EsicDocumentModel?> GetDocumentByIdAsync(int id);

        Task DeleteDocumentAsync(EsicDocumentModel document);

        Task SaveAsync();
    }
}
