using AryamanBMS.Models;
using System.Threading.Tasks;

namespace AryamanBMS.Business.Interfaces
{
    public interface ILoginHistoryService
    {
        #region Recording

        Task RecordAsync(
          string attemptedUserName,
          string eventType,
          bool isSuccessful,
          string? userId = null,
          string? failureReason = null,
          string? ipAddress = null,
          string? userAgent = null,
          string? deviceId = null);

        #endregion

        #region Login Checks

        Task<bool> HasSuccessfulLoginTodayAsync(
            string userId);

        #endregion

        #region History Queries

        Task<List<LoginHistoryModel>> GetRecentAsync(
            int count = 100);

        Task<List<int>> GetAvailableYearsAsync();

        Task<(List<LoginHistoryModel> Records, int TotalRecords)>
            SearchAsync(
                string? searchText,
                string? eventType,
                string? result,
                int? month,
                int? year,
                int page,
                int pageSize);

        #endregion
    }
}