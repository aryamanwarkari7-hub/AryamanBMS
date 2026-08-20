using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface ILoginHistoryRepository
    {
        Task AddAsync(LoginHistoryModel history);

        Task<List<LoginHistoryModel>> GetRecentAsync(
            int count);

        Task<bool> HasSuccessfulLoginTodayAsync(
            string userId);

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
    }
}