using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IPfRepository
    {
        // Snapshot
        Task<List<PfMonthlySnapshotModel>> GetAllSnapshotsAsync();

        Task<PfMonthlySnapshotModel?> GetSnapshotByIdAsync(int id);

        Task<PfMonthlySnapshotModel?> GetSnapshotByMonthYearAsync(int month, int year);

        Task<PfMonthlySnapshotModel> GenerateSnapshotAsync(int month, int year);

        

        Task<bool> MarkFiledAsync(
            int snapshotId,
            string filedByUserId);

        Task<bool> MarkPaidAsync(
            int snapshotId,
            string paidByUserId);

        // Challan
        Task AddChallanAsync(PfChallanModel challan);

        Task UpdateChallanAsync(PfChallanModel challan);

        Task<PfChallanModel?> GetChallanByIdAsync(int id);



        // Document
        Task AddDocumentAsync(PfDocumentModel document);

        Task<PfDocumentModel?> GetDocumentByIdAsync(int id);

        Task DeleteDocumentAsync(PfDocumentModel document);

        Task SaveAsync();
    }
}
