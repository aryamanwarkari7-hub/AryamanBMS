using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Repositories
{
    public class CompOffUsageRepository
        : ICompOffUsageRepository
    {
        private readonly ApplicationDbContext _context;

        public CompOffUsageRepository(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<CompOffUsageModel> CompOffUsages =>
            _context.CompOffUsages;

        public async Task AddAsync(
            CompOffUsageModel usage)
        {
            await _context.CompOffUsages.AddAsync(usage);
        }

        public Task UpdateAsync(
            CompOffUsageModel usage)
        {
            _context.CompOffUsages.Update(usage);

            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}