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
                .Include(x => x.Documents.Where(d => d.IsActive))
                .OrderByDescending(x => x.ReceivedDate)
                .ToListAsync();
        }

        public async Task<NoticeModel?> GetByIdAsync(int id)
        {
            return await _context.Notices
                .Include(x => x.Documents.Where(d => d.IsActive))
                .FirstOrDefaultAsync(x => x.NoticeId == id);
        }

        public async Task<List<NoticeModel>> GetByDepartmentAsync(string department)
        {
            return await _context.Notices
                .Include(x => x.Documents.Where(d => d.IsActive))
                .Where(x => x.Department == department)
                .OrderByDescending(x => x.ReceivedDate)
                .ToListAsync();
        }

        public async Task<List<NoticeModel>> GetByStatusAsync(string status)
        {
            return await _context.Notices
                .Include(x => x.Documents.Where(d => d.IsActive))
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

            return Task.CompletedTask;
        }

        #endregion

        #region Documents

        public async Task AddDocumentAsync(NoticeDocumentModel document)
        {
            document.UploadedOn = DateTime.Now;
            document.IsActive = true;

            await _context.NoticeDocuments.AddAsync(document);
        }

        public async Task<NoticeDocumentModel?> GetDocumentByIdAsync(int id)
        {
            return await _context.NoticeDocuments
                .Include(x => x.Notice)
                .FirstOrDefaultAsync(x => x.NoticeDocumentId == id);
        }

        public Task DeleteDocumentAsync(NoticeDocumentModel document)
        {
            document.IsActive = false;
            return Task.CompletedTask;
        }

        #endregion

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
