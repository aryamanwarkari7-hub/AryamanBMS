using AryamanBMS.Database.Context;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories.Implementations
{
    public class PasswordChangeLogRepository
        : IPasswordChangeLogRepository
    {
        private readonly PasswordChangeLogDbContext _context;

        public PasswordChangeLogRepository(
            PasswordChangeLogDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(
            PasswordChangeLogModel log)
        {
            _context.PasswordChangeLogs.Add(log);

            await _context.SaveChangesAsync();
        }

        public async Task<List<PasswordChangeLogModel>> GetAllAsync()
        {
            return await _context.PasswordChangeLogs
                .AsNoTracking()
                .OrderByDescending(x => x.ChangedOn)
                .ToListAsync();
        }

        public async Task<List<PasswordChangeLogModel>> GetRecentAsync(
            int count = 100)
        {
            if (count <= 0)
            {
                count = 100;
            }

            if (count > 500)
            {
                count = 500;
            }

            return await _context.PasswordChangeLogs
                .AsNoTracking()
                .OrderByDescending(x => x.ChangedOn)
                .Take(count)
                .ToListAsync();
        }
    }
}