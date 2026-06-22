using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IEmployeeDocumentRepository
    {
        IQueryable<EmployeeDocumentModel> Documents { get; }

        Task AddAsync(EmployeeDocumentModel document);
        Task DeleteAsync(EmployeeDocumentModel document);
        Task SaveAsync();
    }
}