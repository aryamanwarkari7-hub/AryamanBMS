using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class PfRepository : IPfRepository
    {
        private readonly ApplicationDbContext _context;

        public PfRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PfMonthlySnapshotModel>> GetAllSnapshotsAsync()
        {
            return await _context.PfMonthlySnapshots
                .Include(x => x.Challans)
                .Include(x => x.Documents)
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToListAsync();
        }

        public async Task<PfMonthlySnapshotModel?> GetSnapshotByIdAsync(int id)
        {
            return await _context.PfMonthlySnapshots
                .Include(x => x.Challans)
                .Include(x => x.Documents)
                .FirstOrDefaultAsync(x => x.PfSnapshotId == id);
        }

        public async Task<PfMonthlySnapshotModel?> GetSnapshotByMonthYearAsync(int month, int year)
        {
            return await _context.PfMonthlySnapshots
                .Include(x => x.Challans)
                .Include(x => x.Documents)
                .FirstOrDefaultAsync(x => x.Month == month && x.Year == year);
        }

        public async Task<PfMonthlySnapshotModel> GenerateSnapshotAsync(int month, int year)
        {
            var existing = await _context.PfMonthlySnapshots
                .FirstOrDefaultAsync(x => x.Month == month && x.Year == year);

            if (existing != null && existing.Status != FinancialConstants.StatutoryStatus.Pending)
            {
                throw new InvalidOperationException(
                    $"PF snapshot for {month}/{year} is already '{existing.Status}' and cannot be regenerated.");
            }

            var salaryRecords = await _context.SalaryRecords
                .Where(x => x.Month == month && x.Year == year)
                .ToListAsync();

            if (!salaryRecords.Any())
            {
                throw new InvalidOperationException(
                    $"No salary records found for {month}/{year}. PF snapshot cannot be generated.");
            }

            decimal employeeTotal = salaryRecords.Sum(x => x.PfDeduction);
            decimal employerTotal = salaryRecords.Sum(x => x.EmployerPf);
            int employeeCount = salaryRecords.Count;

            if (employeeTotal + employerTotal <= 0)
            {
                throw new InvalidOperationException(
                    $"No PF payable amount found for {month}/{year}. Snapshot was not generated.");
            }

            int fyStart = month >= FinancialConstants.FinancialYearStartMonth ? year : year - 1;
            string financialYear = $"{fyStart}-{(fyStart + 1).ToString().Substring(2)}";

            if (existing != null)
            {
                existing.EmployeeDeductionTotal = employeeTotal;
                existing.EmployerContributionTotal = employerTotal;
                existing.TotalPayable = employeeTotal + employerTotal;
                existing.EmployeeCount = employeeCount;
                existing.FinancialYear = financialYear;
                existing.GeneratedOn = DateTime.Now;

                _context.PfMonthlySnapshots.Update(existing);
                await _context.SaveChangesAsync();
                return existing;
            }

            var snapshot = new PfMonthlySnapshotModel
            {
                Month = month,
                Year = year,
                FinancialYear = financialYear,
                EmployeeDeductionTotal = employeeTotal,
                EmployerContributionTotal = employerTotal,
                TotalPayable = employeeTotal + employerTotal,
                EmployeeCount = employeeCount,
                Status = FinancialConstants.StatutoryStatus.Pending,
                GeneratedOn = DateTime.Now
            };

            await _context.PfMonthlySnapshots.AddAsync(snapshot);
            await _context.SaveChangesAsync();
            return snapshot;
        }

        public async Task UpdateSnapshotStatusAsync(int snapshotId, string status)
        {
            var snapshot = await _context.PfMonthlySnapshots.FindAsync(snapshotId);
            if (snapshot != null)
            {
                snapshot.Status = status;
                _context.PfMonthlySnapshots.Update(snapshot);
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddChallanAsync(PfChallanModel challan)
        {
            challan.CreatedOn = DateTime.Now;
            await _context.PfChallans.AddAsync(challan);
        }

        public Task UpdateChallanAsync(PfChallanModel challan)
        {
            challan.UpdatedOn = DateTime.Now;
            _context.PfChallans.Update(challan);
            return Task.CompletedTask;
        }

        public async Task<PfChallanModel?> GetChallanByIdAsync(int id)
        {
            return await _context.PfChallans
                .Include(x => x.Snapshot)
                .FirstOrDefaultAsync(x => x.PfChallanId == id);
        }

        public async Task AddDocumentAsync(PfDocumentModel document)
        {
            document.UploadedOn = DateTime.Now;
            await _context.PfDocuments.AddAsync(document);
        }

        public async Task<PfDocumentModel?> GetDocumentByIdAsync(int id)
        {
            return await _context.PfDocuments
                .FirstOrDefaultAsync(x => x.PfDocumentId == id);
        }

        public async Task DeleteDocumentAsync(int id)
        {
            var doc = await _context.PfDocuments.FindAsync(id);
            if (doc != null)
            {
                _context.PfDocuments.Remove(doc);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}