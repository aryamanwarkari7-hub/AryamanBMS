
   using AryamanBMS.Database.Context;
    using global::AryamanBMS.Models;
    using global::AryamanBMS.Repositories.Interfaces;
    using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories.Implementations
    {
        public class CompanyDocumentRepository : ICompanyDocumentRepository
        {
            private readonly CompanyDocumentDbContext _context;

            public CompanyDocumentRepository(CompanyDocumentDbContext context)
            {
                _context = context;
            }

            public IQueryable<CompanyDocumentModel> CompanyDocuments =>
                _context.CompanyDocuments
                    .Include(d => d.Category);

            public async Task<List<CompanyDocumentModel>> GetAllAsync()
            {
                return await CompanyDocuments
                    .OrderBy(d => d.DocumentName)
                    .ToListAsync();
            }

            public async Task<CompanyDocumentModel?> GetByIdAsync(int id)
            {
                return await CompanyDocuments
                    .FirstOrDefaultAsync(d => d.CompanyDocumentId == id);
            }

            public async Task<List<CompanyDocumentModel>> GetByCategoryAsync(int categoryId)
            {
                return await CompanyDocuments
                    .Where(d => d.DocumentCategoryId == categoryId)
                    .OrderBy(d => d.DocumentName)
                    .ToListAsync();
            }

            public async Task AddAsync(CompanyDocumentModel document)
            {
                await _context.CompanyDocuments.AddAsync(document);
            }

            public Task UpdateAsync(CompanyDocumentModel document)
            {
                document.UpdatedOn = DateTime.Now;

                return Task.CompletedTask;
            }

            public async Task SaveAsync()
            {
                await _context.SaveChangesAsync();
            }
        }
    }

