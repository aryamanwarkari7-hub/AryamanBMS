using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class LeaveTypeRepository
        : ILeaveTypeRepository
    {
        private readonly ApplicationDbContext _context;

        public LeaveTypeRepository(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<LeaveTypeModel> LeaveTypes => _context.LeaveTypes;

        public async Task<LeaveTypeModel?> GetByIdAsync(
            int id)
        {
            return await _context.LeaveTypes
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(
            LeaveTypeModel leaveType)
        {
            await _context.LeaveTypes
                .AddAsync(leaveType);
        }

        public Task UpdateAsync(
            LeaveTypeModel leaveType)
        {
            _context.LeaveTypes.Update(leaveType);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(
            LeaveTypeModel leaveType)
        {
            _context.LeaveTypes.Remove(leaveType);

            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}