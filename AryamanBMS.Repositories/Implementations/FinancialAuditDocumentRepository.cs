using AryamanBMS.Database.Context;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories.Implementations
{
    public class FinancialAuditDocumentRepository : IFinancialAuditDocumentRepository
    {
        private readonly FinancialAuditDocumentDbContext _context;

        public FinancialAuditDocumentRepository(
            FinancialAuditDocumentDbContext context)
        {
            _context = context;
        }

        private IQueryable<FinancialAuditDocumentModel> Documents =>
            _context.FinancialAuditDocuments;

        public async Task<List<FinancialAuditDocumentModel>> GetAllAsync()
        {
            return await Documents
                .OrderByDescending(x => x.UploadedOn)
                .ToListAsync();
        }

        public async Task<FinancialAuditDocumentModel?> GetByIdAsync(int id)
        {
            return await Documents
                .FirstOrDefaultAsync(x => x.FinancialAuditDocumentId == id);
        }

        public async Task<List<FinancialAuditDocumentModel>> GetByFinancialYearAsync(string financialYear)
        {
            return await Documents
                .Where(x => x.FinancialYear == financialYear)
                .OrderByDescending(x => x.UploadedOn)
                .ToListAsync();
        }

        public async Task<List<FinancialAuditDocumentModel>> GetByCategoryAsync(string documentCategory)
        {
            return await Documents
                .Where(x => x.DocumentCategory == documentCategory)
                .OrderByDescending(x => x.UploadedOn)
                .ToListAsync();
        }

        public async Task<bool> ActiveDuplicateExistsAsync(
            string documentCategory,
            string financialYear,
            string fileName,
            int? excludeId = null)
        {
            return await Documents.AnyAsync(x =>
                x.IsActive &&
                x.DocumentCategory == documentCategory &&
                x.FinancialYear == financialYear &&
                x.FileName == fileName &&
                (!excludeId.HasValue ||
                    x.FinancialAuditDocumentId != excludeId.Value));
        }

        public async Task AddAsync(FinancialAuditDocumentModel model)
        {
            model.UploadedOn = DateTime.Now;

            await _context.FinancialAuditDocuments.AddAsync(model);
        }

        public Task UpdateAsync(FinancialAuditDocumentModel model)
        {
            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}