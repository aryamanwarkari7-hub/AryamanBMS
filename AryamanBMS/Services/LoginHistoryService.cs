using AryamanBMS.Models;
using AryamanBMS.Repositories.Implementations;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using System.Threading.Tasks;

namespace AryamanBMS.Services
{
    public class LoginHistoryService : ILoginHistoryService
    {
        private readonly ILoginHistoryRepository _loginHistoryRepository;

        public LoginHistoryService(
            ILoginHistoryRepository loginHistoryRepository)
        {
            _loginHistoryRepository = loginHistoryRepository;
        }

        #region Recording

        public async Task RecordAsync(
            string attemptedUserName,
            string eventType,
            bool isSuccessful,
            string? userId = null,
            string? failureReason = null,
            string? ipAddress = null,
            string? userAgent = null,
            string? deviceId = null)
        {
            var trimmedUserName =
                attemptedUserName?.Trim() ?? string.Empty;

            var trimmedEventType =
                eventType?.Trim() ?? string.Empty;

            var history = new LoginHistoryModel
            {
                UserId = string.IsNullOrWhiteSpace(userId)
                    ? null
                    : userId,

                AttemptedUserName =
                    string.IsNullOrWhiteSpace(trimmedUserName)
                        ? "Unknown"
                        : trimmedUserName[
                            ..Math.Min(
                                trimmedUserName.Length,
                                256)],

                EventType =
                    string.IsNullOrWhiteSpace(trimmedEventType)
                        ? "Unknown"
                        : trimmedEventType[
                            ..Math.Min(
                                trimmedEventType.Length,
                                50)],

                IsSuccessful = isSuccessful,

                FailureReason =
                    string.IsNullOrWhiteSpace(failureReason)
                        ? null
                        : failureReason.Trim()[
                            ..Math.Min(
                                failureReason.Trim().Length,
                                250)],

                IpAddress =
                    string.IsNullOrWhiteSpace(ipAddress)
                        ? null
                        : ipAddress.Trim()[
                            ..Math.Min(
                                ipAddress.Trim().Length,
                                45)],

                UserAgent =
                    string.IsNullOrWhiteSpace(userAgent)
                        ? null
                        : userAgent.Trim()[
                            ..Math.Min(
                                userAgent.Trim().Length,
                                500)],
                DeviceId =
                   string.IsNullOrWhiteSpace(deviceId)
                       ? null
                       : deviceId.Trim()[..Math.Min(deviceId.Trim().Length, 100)],

                OccurredOn = DateTime.Now
            };

            await _loginHistoryRepository
                .AddAsync(history);
        }

        #endregion

        #region Login Checks

        public async Task<bool> HasSuccessfulLoginTodayAsync(
            string userId)
        {
            return await _loginHistoryRepository
                .HasSuccessfulLoginTodayAsync(userId);
        }

        #endregion

        #region History Queries

        public async Task<List<LoginHistoryModel>> GetRecentAsync(
            int count = 100)
        {
            if (count <= 0)
            {
                count = 100;
            }

            if (count > 500)
            {
                count = 500;
            }

            return await _loginHistoryRepository
                .GetRecentAsync(count);
        }

        public async Task<List<int>> GetAvailableYearsAsync()
        {
            return await _loginHistoryRepository
                .GetAvailableYearsAsync();
        }

        public async Task<(List<LoginHistoryModel> Records, int TotalRecords)>
            SearchAsync(
                string? searchText,
                string? eventType,
                string? result,
                int? month,
                int? year,
                int page,
                int pageSize)
        {
            if (page < 1)
            {
                page = 1;
            }

            if (pageSize <= 0)
            {
                pageSize = 15;
            }

            return await _loginHistoryRepository
                .SearchAsync(
                    searchText,
                    eventType,
                    result,
                    month,
                    year,
                    page,
                    pageSize);
        }

        #endregion
    }
}