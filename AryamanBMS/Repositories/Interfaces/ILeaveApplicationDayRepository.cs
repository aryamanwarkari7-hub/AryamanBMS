using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface ILeaveApplicationDayRepository
    {
        IQueryable<LeaveApplicationDayModel> LeaveApplicationDays { get; }

        Task AddAsync(LeaveApplicationDayModel leaveDay);
        Task AddRangeAsync(IEnumerable<LeaveApplicationDayModel> leaveDays);
        Task DeleteRangeAsync(IEnumerable<LeaveApplicationDayModel> leaveDays);
        Task UpdateAsync(LeaveApplicationDayModel leaveDay);
        Task SaveAsync();
    }
}