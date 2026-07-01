using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class GstDocumentRepository : IGstDocumentRepository
    {
        private readonly ApplicationDbContext _context;

        public GstDocumentRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        private IQueryable<GstDocumentModel> Documents =>
            _context.GstDocuments
                .Include(x => x.Snapshot);

        public async Task<List<GstDocumentModel>> GetAllAsync()
        {
            return await Documents
                .OrderByDescending(x => x.UploadedOn)
                .ToListAsync();
        }

        public async Task<GstDocumentModel?> GetByIdAsync(int id)
        {
            return await Documents
                .FirstOrDefaultAsync(x => x.GstDocumentId == id);
        }

        public async Task<List<GstDocumentModel>> GetBySnapshotAsync(int snapshotId)
        {
            return await Documents
                .Where(x => x.SnapshotId == snapshotId)
                .OrderByDescending(x => x.UploadedOn)
                .ToListAsync();
        }

        public async Task<List<GstDocumentModel>> GetByDocumentTypeAsync(string documentType)
        {
            return await Documents
                .Where(x => x.DocumentType == documentType)
                .OrderByDescending(x => x.UploadedOn)
                .ToListAsync();
        }

        public async Task AddAsync(GstDocumentModel model)
        {
            model.UploadedOn = DateTime.Now;

            await _context.GstDocuments.AddAsync(model);
        }

        public Task UpdateAsync(GstDocumentModel model)
        {
            _context.GstDocuments.Update(model);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(GstDocumentModel model)
        {
            _context.GstDocuments.Remove(model);

            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}