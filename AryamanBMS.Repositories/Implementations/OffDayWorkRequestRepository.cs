using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories.Implementations
{
    public class OffDayWorkRequestRepository
        : IOffDayWorkRequestRepository
    {
        private readonly ApplicationDbContext _context;

        public OffDayWorkRequestRepository(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<OffDayWorkRequestModel> Requests =>
            _context.OffDayWorkRequests;

        public async Task<OffDayWorkRequestModel?> GetByIdAsync(
            int id)
        {
            return await _context.OffDayWorkRequests
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(
    OffDayWorkRequestModel request)
        {
            await _context.OffDayWorkRequests.AddAsync(request);
        }

        public Task UpdateAsync(
            OffDayWorkRequestModel request)
        {
            _context.OffDayWorkRequests.Update(request);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(
            OffDayWorkRequestModel request)
        {
            _context.OffDayWorkRequests.Remove(request);
            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}