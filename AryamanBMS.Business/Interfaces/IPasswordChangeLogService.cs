using AryamanBMS.Models;

namespace AryamanBMS.Business.Interfaces
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

        Task<List<PasswordChangeLogModel>> GetAllAsync();

        Task<List<PasswordChangeLogModel>> GetRecentAsync(
            int count = 100);
    }
}