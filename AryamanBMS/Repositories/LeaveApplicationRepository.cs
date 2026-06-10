using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class LeaveApplicationRepository
        : ILeaveApplicationRepository
    {
        private readonly ApplicationDbContext _context;

        public LeaveApplicationRepository(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<LeaveApplicationModel>
            LeaveApplications =>
            _context.LeaveApplications;

        public async Task<LeaveApplicationModel?>
            GetByIdAsync(int id)
        {
            return await _context.LeaveApplications
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(
            LeaveApplicationModel leaveApplication)
        {
            await _context.LeaveApplications
                .AddAsync(leaveApplication);
        }

        public Task UpdateAsync(
            LeaveApplicationModel leaveApplication)
        {
            _context.LeaveApplications
                .Update(leaveApplication);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(
            LeaveApplicationModel leaveApplication)
        {
            _context.LeaveApplications
                .Remove(leaveApplication);

            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}