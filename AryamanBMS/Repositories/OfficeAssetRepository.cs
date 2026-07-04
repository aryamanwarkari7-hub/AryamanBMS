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
                .Include(x => x.AssignedEmployee)
                .OrderByDescending(x => x.PurchaseDate)
                .ToListAsync();
        }

        public async Task<OfficeAssetModel?> GetByIdAsync(int id)
        {
            return await Assets
                .Include(x => x.AssignedEmployee)
                .FirstOrDefaultAsync(x => x.OfficeAssetId == id);
        }

        public async Task<bool> AssetCodeExistsAsync(
            string assetCode,
            int? excludeId = null)
        {
            return await Assets.AnyAsync(x =>
                x.AssetCode != null &&
                x.AssetCode == assetCode &&
                (!excludeId.HasValue || x.OfficeAssetId != excludeId.Value));
        }

        public async Task<List<OfficeAssetModel>> GetByFinancialYearAsync(
            string financialYear)
        {
            return await Assets
                .Include(x => x.AssignedEmployee)
                .Where(x => x.FinancialYear == financialYear)
                .OrderByDescending(x => x.PurchaseDate)
                .ToListAsync();
        }

        public async Task<List<OfficeAssetModel>> GetByCategoryAsync(
            string assetCategory)
        {
            return await Assets
                .Include(x => x.AssignedEmployee)
                .Where(x => x.AssetCategory == assetCategory)
                .OrderByDescending(x => x.PurchaseDate)
                .ToListAsync();
        }

        public async Task<List<OfficeAssetModel>> GetByStatusAsync(string status)
        {
            return await Assets
                .Include(x => x.AssignedEmployee)
                .Where(x => x.Status == status)
                .OrderByDescending(x => x.PurchaseDate)
                .ToListAsync();
        }

        public async Task<List<EmployeeModel>> GetActiveEmployeesAsync()
        {
            return await _context.Employees
                .Where(x => x.IsActive)
                .OrderBy(x => x.FirstName)
                .ThenBy(x => x.LastName)
                .ToListAsync();
        }

        public async Task<OfficeAssetAssignmentHistoryModel?> GetActiveAssignmentAsync(
            int officeAssetId)
        {
            return await _context.OfficeAssetAssignmentHistories
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x =>
                    x.OfficeAssetId == officeAssetId &&
                    x.IsActive);
        }

        public async Task<List<OfficeAssetAssignmentHistoryModel>> GetAssignmentHistoryAsync(
            int officeAssetId)
        {
            return await _context.OfficeAssetAssignmentHistories
                .Include(x => x.Employee)
                .Where(x => x.OfficeAssetId == officeAssetId)
                .OrderByDescending(x => x.AssignedOn)
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

            return Task.CompletedTask;
        }

        public async Task AssignAsync(
            int officeAssetId,
            int employeeId,
            string assignedByUserId,
            string? conditionOnAssignment,
            string? remarks)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var asset = await _context.OfficeAssets
                .FirstAsync(x => x.OfficeAssetId == officeAssetId);

            var employee = await _context.Employees
                .FirstAsync(x => x.Id == employeeId);

            var activeAssignment = await _context.OfficeAssetAssignmentHistories
                .FirstOrDefaultAsync(x =>
                    x.OfficeAssetId == officeAssetId &&
                    x.IsActive);

            if (activeAssignment != null)
            {
                throw new InvalidOperationException(
                    "This asset is already assigned.");
            }

            var history = new OfficeAssetAssignmentHistoryModel
            {
                OfficeAssetId = officeAssetId,
                EmployeeId = employeeId,
                AssignedByUserId = assignedByUserId,
                ConditionOnAssignment = conditionOnAssignment,
                Remarks = remarks,
                IsActive = true,
                AssignedOn = DateTime.Now,
                CreatedOn = DateTime.Now
            };

            asset.AssignedEmployeeId = employee.Id;
            asset.AssignedTo = employee.FullName;
            asset.Status = "InUse";
            asset.UpdatedOn = DateTime.Now;

            await _context.OfficeAssetAssignmentHistories.AddAsync(history);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task ReturnAsync(
            int officeAssetId,
            string returnedByUserId,
            string? conditionOnReturn,
            string? remarks)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var asset = await _context.OfficeAssets
                .FirstAsync(x => x.OfficeAssetId == officeAssetId);

            var activeAssignment = await _context.OfficeAssetAssignmentHistories
                .FirstOrDefaultAsync(x =>
                    x.OfficeAssetId == officeAssetId &&
                    x.IsActive);

            if (activeAssignment == null)
            {
                throw new InvalidOperationException(
                    "This asset has no active assignment.");
            }

            activeAssignment.IsActive = false;
            activeAssignment.ReturnedOn = DateTime.Now;
            activeAssignment.ReturnedByUserId = returnedByUserId;
            activeAssignment.ConditionOnReturn = conditionOnReturn;

            if (!string.IsNullOrWhiteSpace(remarks))
            {
                activeAssignment.Remarks = string.IsNullOrWhiteSpace(activeAssignment.Remarks)
                    ? remarks
                    : $"{activeAssignment.Remarks} | Return: {remarks}";
            }

            asset.AssignedEmployeeId = null;
            asset.AssignedTo = null;
            asset.Status = "Idle";
            asset.UpdatedOn = DateTime.Now;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
