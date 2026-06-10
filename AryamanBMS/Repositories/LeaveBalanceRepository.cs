using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class LeaveBalanceRepository
        : ILeaveBalanceRepository
    {
        private readonly ApplicationDbContext _context;

        public LeaveBalanceRepository(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<LeaveBalanceModel>
            LeaveBalances =>
            _context.LeaveBalances;

        public async Task<LeaveBalanceModel?>
            GetByIdAsync(int id)
        {
            return await _context.LeaveBalances
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(
            LeaveBalanceModel leaveBalance)
        {
            await _context.LeaveBalances
                .AddAsync(leaveBalance);
        }

        public Task UpdateAsync(
            LeaveBalanceModel leaveBalance)
        {
            _context.LeaveBalances
                .Update(leaveBalance);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(
            LeaveBalanceModel leaveBalance)
        {
            _context.LeaveBalances
                .Remove(leaveBalance);

            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}