using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class EsicRepository : IEsicRepository
    {
        private readonly ApplicationDbContext _context;

        public EsicRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<EsicMonthlySnapshotModel>> GetAllSnapshotsAsync()
        {
            return await _context.EsicMonthlySnapshots
                .Include(x => x.Challans)
                .Include(x => x.Documents.Where(d => d.IsActive))
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToListAsync();
        }

        public async Task<EsicMonthlySnapshotModel?> GetSnapshotByIdAsync(int id)
        {
            return await _context.EsicMonthlySnapshots
                .Include(x => x.Challans)
                .Include(x => x.Documents.Where(d => d.IsActive))
                .FirstOrDefaultAsync(x => x.EsicSnapshotId == id);
        }

        public async Task<EsicMonthlySnapshotModel?> GetSnapshotByMonthYearAsync(int month, int year)
        {
            return await _context.EsicMonthlySnapshots
                .Include(x => x.Challans)
                .Include(x => x.Documents.Where(d => d.IsActive))
                .FirstOrDefaultAsync(x => x.Month == month && x.Year == year);
        }

        public async Task<EsicMonthlySnapshotModel> GenerateSnapshotAsync(int month, int year)
        {

            if (month < 1 || month > 12)
            {
                throw new InvalidOperationException(
                    "ESIC month must be between 1 and 12.");
            }

            if (year < 2000 ||
                year > DateTime.Today.Year + 1)
            {
                throw new InvalidOperationException(
                    "Invalid ESIC snapshot year.");
            }

            var existing = await _context.EsicMonthlySnapshots
                .FirstOrDefaultAsync(x => x.Month == month && x.Year == year);

            if (existing != null && existing.Status != FinancialConstants.StatutoryStatus.Pending)
            {
                throw new InvalidOperationException(
                    $"ESIC snapshot for {month}/{year} is already '{existing.Status}' and cannot be regenerated.");
            }

            var salaryRecords = await _context.SalaryRecords
              .Where(x => x.Month == month && x.Year == year)
              .ToListAsync();

            if (!salaryRecords.Any())
            {
                throw new InvalidOperationException(
                    $"No salary records found for {month}/{year}. ESIC snapshot cannot be generated.");
            }

            decimal employeeTotal = salaryRecords.Sum(x => x.EsicDeduction);
            decimal employerTotal = salaryRecords.Sum(x => x.EmployerEsic);
            int employeeCount = salaryRecords.Count;

            if (employeeTotal + employerTotal <= 0)
            {
                throw new InvalidOperationException(
                    $"No ESIC payable amount found for {month}/{year}. Snapshot was not generated.");
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

                _context.EsicMonthlySnapshots.Update(existing);
                await _context.SaveChangesAsync();
                return existing;
            }

            var snapshot = new EsicMonthlySnapshotModel
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

            await _context.EsicMonthlySnapshots.AddAsync(snapshot);
            await _context.SaveChangesAsync();
            return snapshot;
        }

        public async Task<bool> MarkFiledAsync(
    int snapshotId,
    string filedByUserId)
        {
            var snapshot = await _context.EsicMonthlySnapshots
                .FirstOrDefaultAsync(x =>
                    x.EsicSnapshotId == snapshotId);

            if (snapshot == null)
                return false;

            if (snapshot.Status !=
                FinancialConstants.StatutoryStatus.Pending)
            {
                return false;
            }

            snapshot.Status =
                FinancialConstants.StatutoryStatus.Filed;

            snapshot.FiledByUserId = filedByUserId;
            snapshot.FiledOn = DateTime.Now;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> MarkPaidAsync(
            int snapshotId,
            string paidByUserId)
        {
            var snapshot = await _context.EsicMonthlySnapshots
                .Include(x => x.Challans)
                .FirstOrDefaultAsync(x =>
                    x.EsicSnapshotId == snapshotId);

            if (snapshot == null)
                return false;

            if (snapshot.Status !=
                FinancialConstants.StatutoryStatus.Filed)
            {
                return false;
            }

            decimal paidAmount =
                snapshot.Challans.Sum(x => x.AmountPaid);

            if (paidAmount < snapshot.TotalPayable)
                return false;

            snapshot.Status =
                FinancialConstants.StatutoryStatus.Paid;

            snapshot.PaidByUserId = paidByUserId;
            snapshot.PaidOn = DateTime.Now;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task AddChallanAsync(EsicChallanModel challan)
        {
            challan.CreatedOn = DateTime.Now;
            await _context.EsicChallans.AddAsync(challan);
        }

        public Task UpdateChallanAsync(EsicChallanModel challan)
        {
            challan.UpdatedOn = DateTime.Now;
            _context.EsicChallans.Update(challan);
            return Task.CompletedTask;
        }

        public async Task<EsicChallanModel?> GetChallanByIdAsync(int id)
        {
            return await _context.EsicChallans
                .Include(x => x.Snapshot)
                .FirstOrDefaultAsync(x => x.EsicChallanId == id);
        }

        public async Task AddDocumentAsync(EsicDocumentModel document)
        {
            document.UploadedOn = DateTime.Now;
            document.IsActive = true;
            await _context.EsicDocuments.AddAsync(document);
        }

        public async Task<EsicDocumentModel?> GetDocumentByIdAsync(int id)
        {
            return await _context.EsicDocuments
                .Include(x => x.Snapshot)
                .FirstOrDefaultAsync(x => x.EsicDocumentId == id);
        }

        public Task DeleteDocumentAsync(EsicDocumentModel document)
        {
            document.IsActive = false;
            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
