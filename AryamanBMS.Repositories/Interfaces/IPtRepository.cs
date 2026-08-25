using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IPtRepository
    {
        // Snapshot
        Task<List<PtMonthlySnapshotModel>> GetAllSnapshotsAsync();

        Task<PtMonthlySnapshotModel?> GetSnapshotByIdAsync(int id);

        Task<PtMonthlySnapshotModel?> GetSnapshotByMonthYearAsync(int month, int year);

        Task<PtMonthlySnapshotModel> GenerateSnapshotAsync(int month, int year);

        Task<bool> MarkFiledAsync(
    int snapshotId,
    string filedByUserId);

        Task<bool> MarkPaidAsync(
            int snapshotId,
            string paidByUserId);

        // Challan
        Task AddChallanAsync(PtChallanModel challan);

        Task UpdateChallanAsync(PtChallanModel challan);

        Task<PtChallanModel?> GetChallanByIdAsync(int id);

        // Document
        Task AddDocumentAsync(PtDocumentModel document);

        Task<PtDocumentModel?> GetDocumentByIdAsync(int id);

        Task DeleteDocumentAsync(PtDocumentModel document);

        Task SaveAsync();
    }
}
