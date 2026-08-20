using AryamanBMS.Models;

namespace AryamanBMS.Services.Interfaces
{
    public interface IPasswordChangeLogService
    {
        Task RecordAsync(
            string userId,
            string? userName,
            string? email,
            string? changedByUserId,
            string? changedByUserName,
            string changeType,
            string? ipAddress,
            string? userAgent);

        Task<List<PasswordChangeLogModel>> GetRecentAsync(
            int count = 100);
    }
}