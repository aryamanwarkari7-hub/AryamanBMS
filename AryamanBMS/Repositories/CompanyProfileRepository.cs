using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class CompanyProfileRepository : ICompanyProfileRepository
    {
        private readonly ApplicationDbContext _context;

        public CompanyProfileRepository(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CompanyProfileModel?> GetActiveAsync()
        {
            return await _context.CompanyProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IsActive);
        }

        public async Task<bool> ExistsAsync()
        {
            return await _context.CompanyProfiles
                .AnyAsync();
        }

        public async Task AddAsync(
            CompanyProfileModel model)
        {
            model.CreatedOn = DateTime.Now;

            await _context.CompanyProfiles
                .AddAsync(model);
        }

        public Task UpdateAsync(
            CompanyProfileModel model)
        {
            model.UpdatedOn = DateTime.Now;

            _context.CompanyProfiles
                .Update(model);

            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}