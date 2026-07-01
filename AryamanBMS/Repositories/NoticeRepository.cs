using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class NoticeRepository : INoticeRepository
    {
        private readonly ApplicationDbContext _context;

        public NoticeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Notice

        public async Task<List<NoticeModel>> GetAllAsync()
        {
            return await _context.Notices
                .Include(x => x.Documents)
                .OrderByDescending(x => x.ReceivedDate)
                .ToListAsync();
        }

        public async Task<NoticeModel?> GetByIdAsync(int id)
        {
            return await _context.Notices
                .Include(x => x.Documents)
                .FirstOrDefaultAsync(x => x.NoticeId == id);
        }

        public async Task<List<NoticeModel>> GetByDepartmentAsync(string department)
        {
            return await _context.Notices
                .Include(x => x.Documents)
                .Where(x => x.Department == department)
                .OrderByDescending(x => x.ReceivedDate)
                .ToListAsync();
        }

        public async Task<List<NoticeModel>> GetByStatusAsync(string status)
        {
            return await _context.Notices
                .Include(x => x.Documents)
                .Where(x => x.Status == status)
                .OrderByDescending(x => x.ReceivedDate)
                .ToListAsync();
        }

        public async Task AddAsync(NoticeModel notice)
        {
            notice.CreatedOn = DateTime.Now;
            await _context.Notices.AddAsync(notice);
        }

        public Task UpdateAsync(NoticeModel notice)
        {
            notice.UpdatedOn = DateTime.Now;

            _context.Notices.Update(notice);

            return Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            var notice = await _context.Notices.FindAsync(id);

            if (notice != null)
            {
                _context.Notices.Remove(notice);
                await _context.SaveChangesAsync();
            }
        }

        #endregion

        #region Documents

        public async Task AddDocumentAsync(NoticeDocumentModel document)
        {
            document.UploadedOn = DateTime.Now;

            await _context.NoticeDocuments.AddAsync(document);
        }

        public async Task<NoticeDocumentModel?> GetDocumentByIdAsync(int id)
        {
            return await _context.NoticeDocuments
                .FirstOrDefaultAsync(x => x.NoticeDocumentId == id);
        }

        public async Task DeleteDocumentAsync(int id)
        {
            var doc = await _context.NoticeDocuments.FindAsync(id);

            if (doc != null)
            {
                _context.NoticeDocuments.Remove(doc);
                await _context.SaveChangesAsync();
            }
        }

        #endregion

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}