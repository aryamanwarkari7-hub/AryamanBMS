using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class PtRepository : IPtRepository
    {
        private readonly ApplicationDbContext _context;

        public PtRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PtMonthlySnapshotModel>> GetAllSnapshotsAsync()
        {
            return await _context.PtMonthlySnapshots
                .Include(x => x.Challans)
                .Include(x => x.Documents)
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToListAsync();
        }

        public async Task<PtMonthlySnapshotModel?> GetSnapshotByIdAsync(int id)
        {
            return await _context.PtMonthlySnapshots
                .Include(x => x.Challans)
                .Include(x => x.Documents)
                .FirstOrDefaultAsync(x => x.PtSnapshotId == id);
        }

        public async Task<PtMonthlySnapshotModel?> GetSnapshotByMonthYearAsync(int month, int year)
        {
            return await _context.PtMonthlySnapshots
                .Include(x => x.Challans)
                .Include(x => x.Documents)
                .FirstOrDefaultAsync(x => x.Month == month && x.Year == year);
        }

        public async Task<PtMonthlySnapshotModel> GenerateSnapshotAsync(int month, int year)
        {
            var existing = await _context.PtMonthlySnapshots
                .FirstOrDefaultAsync(x => x.Month == month && x.Year == year);

            if (existing != null && existing.Status != FinancialConstants.StatutoryStatus.Pending)
            {
                throw new InvalidOperationException(
                    $"PT snapshot for {month}/{year} is already '{existing.Status}' and cannot be regenerated.");
            }

            var salaryRecords = await _context.SalaryRecords
                .Where(x => x.Month == month && x.Year == year)
                .ToListAsync();

            decimal totalPayable = salaryRecords.Sum(x => x.ProfessionalTax);
            int employeeCount = salaryRecords.Count;

            int fyStart = month >= FinancialConstants.FinancialYearStartMonth ? year : year - 1;
            string financialYear = $"{fyStart}-{(fyStart + 1).ToString().Substring(2)}";

            if (existing != null)
            {
                existing.TotalPayable = totalPayable;
                existing.EmployeeCount = employeeCount;
                existing.FinancialYear = financialYear;
                existing.GeneratedOn = DateTime.Now;

                _context.PtMonthlySnapshots.Update(existing);
                await _context.SaveChangesAsync();
                return existing;
            }

            var snapshot = new PtMonthlySnapshotModel
            {
                Month = month,
                Year = year,
                FinancialYear = financialYear,
                TotalPayable = totalPayable,
                EmployeeCount = employeeCount,
                Status = FinancialConstants.StatutoryStatus.Pending,
                GeneratedOn = DateTime.Now
            };

            await _context.PtMonthlySnapshots.AddAsync(snapshot);
            await _context.SaveChangesAsync();
            return snapshot;
        }

        public async Task UpdateSnapshotStatusAsync(int snapshotId, string status)
        {
            var snapshot = await _context.PtMonthlySnapshots.FindAsync(snapshotId);
            if (snapshot != null)
            {
                snapshot.Status = status;
                _context.PtMonthlySnapshots.Update(snapshot);
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddChallanAsync(PtChallanModel challan)
        {
            challan.CreatedOn = DateTime.Now;
            await _context.PtChallans.AddAsync(challan);
        }

        public Task UpdateChallanAsync(PtChallanModel challan)
        {
            challan.UpdatedOn = DateTime.Now;
            _context.PtChallans.Update(challan);
            return Task.CompletedTask;
        }

        public async Task<PtChallanModel?> GetChallanByIdAsync(int id)
        {
            return await _context.PtChallans
                .Include(x => x.Snapshot)
                .FirstOrDefaultAsync(x => x.PtChallanId == id);
        }

        public async Task AddDocumentAsync(PtDocumentModel document)
        {
            document.UploadedOn = DateTime.Now;
            await _context.PtDocuments.AddAsync(document);
        }

        public async Task<PtDocumentModel?> GetDocumentByIdAsync(int id)
        {
            return await _context.PtDocuments
                .FirstOrDefaultAsync(x => x.PtDocumentId == id);
        }

        public async Task DeleteDocumentAsync(int id)
        {
            var doc = await _context.PtDocuments.FindAsync(id);
            if (doc != null)
            {
                _context.PtDocuments.Remove(doc);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}