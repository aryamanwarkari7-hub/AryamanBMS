using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IWorkingDayOverrideRepository
    {
        Task<List<WorkingDayOverrideModel>> GetAllAsync(
            string status,
            string sortBy,
            string sortOrder);

        Task<List<WorkingDayOverrideModel>> GetForExportAsync();

        Task<WorkingDayOverrideModel?> GetByIdAsync(int id);

        Task<bool> ExistsForDateAsync(
            DateTime overrideDate,
            int? excludeId = null);

        Task AddAsync(WorkingDayOverrideModel item);

        Task SaveAsync();
    }
}