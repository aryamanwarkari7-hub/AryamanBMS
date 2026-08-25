using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Repositories
{
    public class LeaveApplicationDayRepository : ILeaveApplicationDayRepository
    {
        private readonly ApplicationDbContext _context;

        public LeaveApplicationDayRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<LeaveApplicationDayModel> LeaveApplicationDays =>
            _context.LeaveApplicationDays;

        public async Task AddAsync(LeaveApplicationDayModel leaveDay)
        {
            await _context.LeaveApplicationDays.AddAsync(leaveDay);
        }

        public async Task AddRangeAsync(IEnumerable<LeaveApplicationDayModel> leaveDays)
        {
            await _context.LeaveApplicationDays.AddRangeAsync(leaveDays);
        }

        public Task DeleteRangeAsync(IEnumerable<LeaveApplicationDayModel> leaveDays)
        {
            _context.LeaveApplicationDays.RemoveRange(leaveDays);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(LeaveApplicationDayModel leaveDay)
        {
            _context.LeaveApplicationDays.Update(leaveDay);
            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}