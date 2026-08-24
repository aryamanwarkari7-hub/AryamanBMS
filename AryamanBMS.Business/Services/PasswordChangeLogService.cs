using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Business.Services
{
    public class PasswordChangeLogService
        : IPasswordChangeLogService
    {
        private readonly IPasswordChangeLogRepository
            _passwordChangeLogRepository;

        public PasswordChangeLogService(
            IPasswordChangeLogRepository passwordChangeLogRepository)
        {
            _passwordChangeLogRepository =
                passwordChangeLogRepository;
        }

        public async Task RecordAsync(
            string userId,
            string? userName,
            string? email,
            string? changedByUserId,
            string? changedByUserName,
            string changeType,
            string? ipAddress,
            string? userAgent)
        {
            var log = new PasswordChangeLogModel
            {
                UserId = userId,

                UserName = Trim(
                    userName,
                    150),

                Email = Trim(
                    email,
                    150),

                ChangedByUserId = Trim(
                    changedByUserId,
                    450),

                ChangedByUserName = Trim(
                    changedByUserName,
                    150),

                ChangeType = string.IsNullOrWhiteSpace(changeType)
                    ? "Unknown"
                    : Trim(changeType, 30)!,

                ChangedOn = DateTime.Now,

                IpAddress = Trim(
                    ipAddress,
                    100),

                UserAgent = Trim(
                    userAgent,
                    500)
            };

            await _passwordChangeLogRepository
                .AddAsync(log);
        }

        public async Task<List<PasswordChangeLogModel>> GetAllAsync()
        {
            return await _passwordChangeLogRepository.GetAllAsync();
        }

        public async Task<List<PasswordChangeLogModel>> GetRecentAsync(
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

            return await _passwordChangeLogRepository
                .GetRecentAsync(count);
        }

        private static string? Trim(
            string? value,
            int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            value = value.Trim();

            return value[..Math.Min(
                value.Length,
                maxLength)];
        }
    }
}