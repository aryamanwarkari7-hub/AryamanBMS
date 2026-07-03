using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class FinancialAuditDocumentRepository : IFinancialAuditDocumentRepository
    {
        private readonly ApplicationDbContext _context;

        public FinancialAuditDocumentRepository(ApplicationDbContext context)
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

        public async Task AddAsync(FinancialAuditDocumentModel model)
        {
            model.UploadedOn = DateTime.Now;

            await _context.FinancialAuditDocuments.AddAsync(model);
        }

        public Task UpdateAsync(FinancialAuditDocumentModel model)
        {
            _context.FinancialAuditDocuments.Update(model);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(FinancialAuditDocumentModel model)
        {
            _context.FinancialAuditDocuments.Remove(model);

            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}