using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class GstItcRepository : IGstItcRepository
    {
        private readonly ApplicationDbContext _context;

        public GstItcRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        private IQueryable<GstItcRecordModel> Records =>
            _context.GstItcRecords
                .Include(x => x.Snapshot);

        public async Task<List<GstItcRecordModel>> GetAllAsync()
        {
            return await Records
                .OrderByDescending(x => x.InvoiceDate)
                .ToListAsync();
        }

        public async Task<GstItcRecordModel?> GetByIdAsync(int id)
        {
            return await Records
                .FirstOrDefaultAsync(x => x.ItcRecordId == id);
        }

        public async Task<List<GstItcRecordModel>> GetBySnapshotAsync(int snapshotId)
        {
            return await Records
                .Where(x => x.SnapshotId == snapshotId)
                .OrderByDescending(x => x.InvoiceDate)
                .ToListAsync();
        }

        public async Task<List<GstItcRecordModel>> GetByVendorAsync(string vendorName)
        {
            return await Records
                .Where(x => x.VendorName == vendorName)
                .OrderByDescending(x => x.InvoiceDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalItcAsync(int snapshotId)
        {
            return await Records
                .Where(x => x.SnapshotId == snapshotId)
                .SumAsync(x => x.CGST + x.SGST + x.IGST);
        }

        public async Task AddAsync(GstItcRecordModel model)
        {
            model.CreatedOn = DateTime.Now;

            await _context.GstItcRecords.AddAsync(model);
        }

        public Task UpdateAsync(GstItcRecordModel model)
        {
            model.UpdatedOn = DateTime.Now;

            _context.GstItcRecords.Update(model);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(GstItcRecordModel model)
        {
            throw new InvalidOperationException(
                "GST ITC records cannot be deleted directly.");
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}