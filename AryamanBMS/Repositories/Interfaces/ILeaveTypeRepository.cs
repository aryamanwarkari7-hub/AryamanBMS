using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface ILeaveTypeRepository
    {
        IQueryable<LeaveTypeModel> LeaveTypes { get; }

        Task<LeaveTypeModel?> GetByIdAsync(int id);

        Task AddAsync(LeaveTypeModel leaveType);

        Task UpdateAsync(LeaveTypeModel leaveType);

        Task DeleteAsync(LeaveTypeModel leaveType);

        Task SaveAsync();
    }
}