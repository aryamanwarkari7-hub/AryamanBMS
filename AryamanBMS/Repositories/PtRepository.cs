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
                .Include(x => x.Documents.Where(d => d.IsActive))
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .ToListAsync();
        }

        public async Task<PtMonthlySnapshotModel?> GetSnapshotByIdAsync(int id)
        {
            return await _context.PtMonthlySnapshots
                .Include(x => x.Challans)
                .Include(x => x.Documents.Where(d => d.IsActive))
                .FirstOrDefaultAsync(x => x.PtSnapshotId == id);
        }

        public async Task<PtMonthlySnapshotModel?> GetSnapshotByMonthYearAsync(int month, int year)
        {
            return await _context.PtMonthlySnapshots
                .Include(x => x.Challans)
                .Include(x => x.Documents.Where(d => d.IsActive))
                .FirstOrDefaultAsync(x => x.Month == month && x.Year == year);
        }

        public async Task<PtMonthlySnapshotModel> GenerateSnapshotAsync(int month, int year)
        {
            if (month < 1 || month > 12)
            {
                throw new InvalidOperationException(
                    "PT month must be between 1 and 12.");
            }

            if (year < 2000 ||
                year > DateTime.Today.Year + 1)
            {
                throw new InvalidOperationException(
                    "Invalid PT snapshot year.");
            }
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

            if (!salaryRecords.Any())
            {
                throw new InvalidOperationException(
                    $"No salary records found for {month}/{year}. PT snapshot cannot be generated.");
            }

            decimal totalPayable = salaryRecords.Sum(x => x.ProfessionalTax);
            int employeeCount = salaryRecords.Count;

            if (totalPayable <= 0)
            {
                throw new InvalidOperationException(
                    $"No PT payable amount found for {month}/{year}. Snapshot was not generated.");
            }

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

        public async Task<bool> MarkFiledAsync(
    int snapshotId,
    string filedByUserId)
        {
            var snapshot = await _context.PtMonthlySnapshots
                .FirstOrDefaultAsync(x =>
                    x.PtSnapshotId == snapshotId);

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
            var snapshot = await _context.PtMonthlySnapshots
                .Include(x => x.Challans)
                .FirstOrDefaultAsync(x =>
                    x.PtSnapshotId == snapshotId);

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
            document.IsActive = true;
            await _context.PtDocuments.AddAsync(document);
        }

        public async Task<PtDocumentModel?> GetDocumentByIdAsync(int id)
        {
            return await _context.PtDocuments
                .Include(x => x.Snapshot)
                .FirstOrDefaultAsync(x => x.PtDocumentId == id);
        }

        public Task DeleteDocumentAsync(PtDocumentModel document)
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
