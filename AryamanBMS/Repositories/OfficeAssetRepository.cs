using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class OfficeAssetRepository : IOfficeAssetRepository
    {
        private readonly ApplicationDbContext _context;

        public OfficeAssetRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        private IQueryable<OfficeAssetModel> Assets =>
            _context.OfficeAssets;

        public async Task<List<OfficeAssetModel>> GetAllAsync()
        {
            return await Assets
                .OrderByDescending(x => x.PurchaseDate)
                .ToListAsync();
        }

        public async Task<OfficeAssetModel?> GetByIdAsync(int id)
        {
            return await Assets
                .FirstOrDefaultAsync(x => x.OfficeAssetId == id);
        }

        public async Task<List<OfficeAssetModel>> GetByFinancialYearAsync(string financialYear)
        {
            return await Assets
                .Where(x => x.FinancialYear == financialYear)
                .OrderByDescending(x => x.PurchaseDate)
                .ToListAsync();
        }

        public async Task<List<OfficeAssetModel>> GetByCategoryAsync(string assetCategory)
        {
            return await Assets
                .Where(x => x.AssetCategory == assetCategory)
                .OrderByDescending(x => x.PurchaseDate)
                .ToListAsync();
        }

        public async Task<List<OfficeAssetModel>> GetByStatusAsync(string status)
        {
            return await Assets
                .Where(x => x.Status == status)
                .OrderByDescending(x => x.PurchaseDate)
                .ToListAsync();
        }

        public async Task AddAsync(OfficeAssetModel model)
        {
            model.CreatedOn = DateTime.Now;

            await _context.OfficeAssets.AddAsync(model);
        }

        public Task UpdateAsync(OfficeAssetModel model)
        {
            model.UpdatedOn = DateTime.Now;

            _context.OfficeAssets.Update(model);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(OfficeAssetModel model)
        {
            _context.OfficeAssets.Remove(model);

            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}