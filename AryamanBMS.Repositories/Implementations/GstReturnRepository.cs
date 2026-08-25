using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class GstReturnRepository : IGstReturnRepository
    {
        private readonly ApplicationDbContext _context;

        public GstReturnRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        private IQueryable<GstReturnModel> Returns =>
           _context.GstReturns
           .Include(x => x.Snapshot);

        public async Task<List<GstReturnModel>> GetAllAsync()
        {
            return await Returns
                .OrderByDescending(x => x.FiledDate)
                .ToListAsync();
        }

        public async Task<GstReturnModel?> GetByIdAsync(int id)
        {
            return await Returns
                .FirstOrDefaultAsync(x => x.GstReturnId == id);
        }

        public async Task<List<GstReturnModel>> GetBySnapshotAsync(int snapshotId)
        {
            return await Returns
                .Where(x => x.SnapshotId == snapshotId)
                .OrderBy(x => x.ReturnType)
                .ToListAsync();
        }

        public async Task<GstReturnModel?> GetByReturnTypeAsync(int snapshotId, string returnType)
        {
            return await Returns
                .FirstOrDefaultAsync(x =>
                   x.SnapshotId == snapshotId &&
                    x.ReturnType == returnType);
        }

        public async Task AddAsync(GstReturnModel model)
        {
            bool duplicateExists = await _context.GstReturns
                .AnyAsync(x =>
                    x.SnapshotId == model.SnapshotId &&
                    x.ReturnType == model.ReturnType);

            if (duplicateExists)
            {
                throw new InvalidOperationException(
                    $"A {model.ReturnType} return already exists for this GST snapshot.");
            }

            model.CreatedOn = DateTime.Now;

            await _context.GstReturns.AddAsync(model);
        }

        public Task UpdateAsync(GstReturnModel model)
        {
            model.UpdatedOn = DateTime.Now;

            _context.GstReturns.Update(model);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(GstReturnModel model)
        {
            if (string.Equals(
                    model.Status,
                    "Filed",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Filed GST returns cannot be deleted.");
            }

            model.Status = "Cancelled";
            model.UpdatedOn = DateTime.Now;

            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}

