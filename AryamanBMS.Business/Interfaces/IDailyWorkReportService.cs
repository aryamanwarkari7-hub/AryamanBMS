using AryamanBMS.Models;

namespace AryamanBMS.Business.Interfaces
{
    public interface IDailyWorkReportService
    {
        Task<DailyWorkReportModel?> GetReportByIdAsync(int id);
        Task<DailyWorkReportModel?> GetReportByEmployeeAndDateAsync(int employeeId, DateTime date);
        Task<List<DailyWorkReportModel>> GetEmployeeReportsAsync(
            int employeeId,
            DateTime? fromDate = null,
            DateTime? toDate = null
        );
        Task<List<DailyWorkReportModel>> GetTeamReportsAsync(
            int? employeeId = null,
            int? projectId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null
        );
        Task<List<DailyWorkReportPlanModel>> GetPreviousPlansToImportAsync(
            int employeeId,
            DateTime date
        );
        Task<(bool Success, string? Error, DailyWorkReportModel? Report)> SaveReportAsync(
            DailyWorkReportModel model,
            string currentUserId
        );
        Task<bool> DeleteReportAsync(int id, string currentUserId, bool isAdmin);
    }
}
