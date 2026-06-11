using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class SalaryRecordRepository
        : ISalaryRecordRepository
    {
        private readonly ApplicationDbContext _context;

        public SalaryRecordRepository(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<SalaryRecordModel> SalaryRecords =>
            _context.SalaryRecords
                .Include(x => x.Employee);

        public async Task<SalaryRecordModel?> GetByIdAsync(
            int id)
        {
            return await _context.SalaryRecords
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(
            SalaryRecordModel salaryRecord)
        {
            await _context.SalaryRecords
                .AddAsync(salaryRecord);
        }

        public Task UpdateAsync(
            SalaryRecordModel salaryRecord)
        {
            _context.SalaryRecords
                .Update(salaryRecord);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(
            SalaryRecordModel salaryRecord)
        {
            _context.SalaryRecords
                .Remove(salaryRecord);

            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}