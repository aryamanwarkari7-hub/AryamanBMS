using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Repositories
{
    public class EmployeeDocumentRepository
        : IEmployeeDocumentRepository
    {
        private readonly ApplicationDbContext _context;

        public EmployeeDocumentRepository(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<EmployeeDocumentModel> Documents =>
            _context.EmployeeDocuments;

        public async Task AddAsync(
            EmployeeDocumentModel document)
        {
            await _context.EmployeeDocuments.AddAsync(document);
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public Task DeleteAsync(EmployeeDocumentModel document)
        {
            _context.EmployeeDocuments.Remove(document);
            return Task.CompletedTask;
        }
    }
}