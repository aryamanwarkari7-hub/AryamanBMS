using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface ILeaveBalanceRepository
    {
        IQueryable<LeaveBalanceModel> LeaveBalances { get; }

        Task<LeaveBalanceModel?> GetByIdAsync(int id);

        Task AddAsync(LeaveBalanceModel leaveBalance);

        Task UpdateAsync(LeaveBalanceModel leaveBalance);

        Task DeleteAsync(LeaveBalanceModel leaveBalance);

        Task SaveAsync();
    }
}