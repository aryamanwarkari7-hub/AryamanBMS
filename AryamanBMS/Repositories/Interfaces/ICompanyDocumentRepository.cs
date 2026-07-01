using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    
        public interface ICompanyDocumentRepository
        {
            Task<List<CompanyDocumentModel>> GetAllAsync();

            Task<CompanyDocumentModel?> GetByIdAsync(int id);

            Task<List<CompanyDocumentModel>> GetByCategoryAsync(int categoryId);

            Task AddAsync(CompanyDocumentModel document);

            Task UpdateAsync(CompanyDocumentModel document);

            Task DeleteAsync(CompanyDocumentModel document);

            Task SaveAsync();
        }
    }

