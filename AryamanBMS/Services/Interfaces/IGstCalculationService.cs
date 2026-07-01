using AryamanBMS.Models;

namespace AryamanBMS.Services.Interfaces
{
    public interface IGstCalculationService
    {
        Task<bool> IsSnapshotLockedAsync(
            int month,
            int year);

        Task<decimal> GetOutputGSTAsync(
            int month,
            int year);

        Task<decimal> GetInputGSTAsync(
            int month,
            int year);

        Task<decimal> GetNetGSTAsync(
            int month,
            int year);

        Task<GstMonthlySnapshotModel> GenerateMonthlySnapshotAsync(
            int month,
            int year);
    }
}
