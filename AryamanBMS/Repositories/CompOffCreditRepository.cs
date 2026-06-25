using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class CompOffCreditRepository
        : ICompOffCreditRepository
    {
        private readonly ApplicationDbContext _context;

        public CompOffCreditRepository(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<CompOffCreditModel> CompOffCredits =>
            _context.CompOffCredits;

        public async Task<CompOffCreditModel?> GetByIdAsync(int id)
        {
            return await _context.CompOffCredits
                .Include(x => x.Employee)
                .Include(x => x.Attendance)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(
            CompOffCreditModel compOffCredit)
        {
            await _context.CompOffCredits
                .AddAsync(compOffCredit);
        }

        public Task UpdateAsync(
            CompOffCreditModel compOffCredit)
        {
            _context.CompOffCredits.Update(compOffCredit);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(
            CompOffCreditModel compOffCredit)
        {
            _context.CompOffCredits.Remove(compOffCredit);

            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}