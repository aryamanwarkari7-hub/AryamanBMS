using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IDailyWorkReportRepository
    {
        Task<DailyWorkReportModel?> GetByIdAsync(int id, bool asNoTracking = false);
        Task<DailyWorkReportModel?> GetByEmployeeAndDateAsync(int employeeId, DateTime date);
        Task<List<DailyWorkReportModel>> GetByEmployeeAsync(
            int employeeId,
            DateTime? fromDate = null,
            DateTime? toDate = null
        );
        Task<List<DailyWorkReportModel>> GetReportsAsync(
            int? employeeId = null,
            int? projectId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null
        );
        Task<List<DailyWorkReportPlanModel>> GetMostRecentPreviousPlansAsync(
            int employeeId,
            DateTime beforeDate
        );
        Task<DailyWorkReportModel> CreateAsync(DailyWorkReportModel report);
        Task<DailyWorkReportModel> UpdateAsync(DailyWorkReportModel report);
        Task DeleteAsync(int id);
        Task<bool> ExistsForDateAsync(int employeeId, DateTime date, int? excludeReportId = null);
    }
}
