using AryamanBMS.Models;

namespace AryamanBMS.Services.Interfaces
{
    public interface ILoginHistoryService
    {
        Task RecordAsync(
            string attemptedUserName,
            string eventType,
            bool isSuccessful,
            string? userId = null,
            string? failureReason = null,
            string? ipAddress = null,
            string? userAgent = null);

        Task<List<LoginHistoryModel>> GetRecentAsync(
            int count = 100);

        Task<bool> HasSuccessfulLoginTodayAsync(string userId);
    }
}