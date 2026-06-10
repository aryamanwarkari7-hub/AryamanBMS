using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface ILeaveApplicationRepository
    {
        IQueryable<LeaveApplicationModel> LeaveApplications { get; }

        Task<LeaveApplicationModel?> GetByIdAsync(int id);

        Task AddAsync(LeaveApplicationModel leaveApplication);

        Task UpdateAsync(LeaveApplicationModel leaveApplication);

        Task DeleteAsync(LeaveApplicationModel leaveApplication);

        Task SaveAsync();
    }
}