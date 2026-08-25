using AryamanBMS.Database.Context;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories.Implementations
{
    public class GstLutDocumentRepository
        : IGstLutDocumentRepository
    {
        private readonly GstConfigurationDbContext _context;

        public GstLutDocumentRepository(
            GstConfigurationDbContext context)
        {
            _context = context;
        }

        public async Task<List<GstLutDocumentModel>>
            GetActiveByConfigurationIdAsync(
                int gstConfigurationId)
        {
            return await _context.GstLutDocuments
                .AsNoTracking()
                .Where(x =>
                    x.GstConfigurationId == gstConfigurationId &&
                    x.IsActive)
                .OrderByDescending(x => x.UploadedOn)
                .ToListAsync();
        }

        public async Task<GstLutDocumentModel?>
            GetActiveByIdAsync(int id)
        {
            return await _context.GstLutDocuments
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.GstLutDocumentId == id &&
                    x.IsActive);
        }

        public async Task DeactivateActiveByConfigurationIdAsync(
            int gstConfigurationId)
        {
            var documents = await _context.GstLutDocuments
                .Where(x =>
                    x.GstConfigurationId == gstConfigurationId &&
                    x.IsActive)
                .ToListAsync();

            foreach (var document in documents)
            {
                document.IsActive = false;
            }
        }

        public async Task AddAsync(
            GstLutDocumentModel document)
        {
            await _context.GstLutDocuments.AddAsync(document);
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}