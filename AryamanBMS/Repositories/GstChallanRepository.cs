
using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class GstChallanRepository : IGstChallanRepository
    {
        private readonly ApplicationDbContext _context;

        public GstChallanRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        private IQueryable<GstChallanModel> Challans =>
            _context.GstChallans
                .Include(x => x.Snapshot);

        public async Task<List<GstChallanModel>> GetAllAsync()
        {
            return await Challans
                .OrderByDescending(x => x.PaymentDate)
                .ThenByDescending(x => x.CreatedOn)
                .ToListAsync();
        }

        public async Task<GstChallanModel?> GetByIdAsync(int id)
        {
            return await Challans
                .FirstOrDefaultAsync(x => x.ChallanId == id);
        }

        public async Task<List<GstChallanModel>> GetBySnapshotAsync(int snapshotId)
        {
            return await Challans
                .Where(x => x.SnapshotId == snapshotId)
                .OrderByDescending(x => x.PaymentDate)
                .ToListAsync();
        }

        public async Task<GstChallanModel?> GetByChallanNumberAsync(string challanNumber)
        {
            return await Challans
                .FirstOrDefaultAsync(x => x.ChallanNumber == challanNumber);
        }

        public async Task AddAsync(GstChallanModel model)
        {
            model.CreatedOn = DateTime.Now;

            await _context.GstChallans.AddAsync(model);
        }

        public Task UpdateAsync(GstChallanModel model)
        {
            model.UpdatedOn = DateTime.Now;

            _context.GstChallans.Update(model);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(GstChallanModel model)
        {
            _context.GstChallans.Remove(model);

            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

