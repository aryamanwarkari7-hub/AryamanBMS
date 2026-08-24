using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IPasswordChangeLogRepository
    {
        Task AddAsync(PasswordChangeLogModel log);

        Task<List<PasswordChangeLogModel>> GetAllAsync();

        Task<List<PasswordChangeLogModel>> GetRecentAsync(
            int count = 100);
    }
}