using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface INoticeRepository
    {
        // Notice
        Task<List<NoticeModel>> GetAllAsync();

        Task<NoticeModel?> GetByIdAsync(int id);

        Task<List<NoticeModel>> GetByDepartmentAsync(string department);

        Task<List<NoticeModel>> GetByStatusAsync(string status);

        Task AddAsync(NoticeModel notice);

        Task UpdateAsync(NoticeModel notice);

        // Documents
        Task AddDocumentAsync(NoticeDocumentModel document);

        Task<NoticeDocumentModel?> GetDocumentByIdAsync(int id);

        Task DeleteDocumentAsync(NoticeDocumentModel document);

        Task SaveAsync();
    }
}
