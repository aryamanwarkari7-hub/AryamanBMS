using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class CompanyDocumentCategoryRepository
        : ICompanyDocumentCategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public CompanyDocumentCategoryRepository(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CompanyDocumentCategoryModel>> GetAllAsync()
        {
            return await _context.CompanyDocumentCategories
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.CategoryName)
                .ToListAsync();
        }

        public async Task<CompanyDocumentCategoryModel?> GetByIdAsync(int id)
        {
            return await _context.CompanyDocumentCategories
                .FirstOrDefaultAsync(x =>
                    x.DocumentCategoryId == id);
        }

        public async Task AddAsync(
            CompanyDocumentCategoryModel category)
        {
            await _context.CompanyDocumentCategories
                .AddAsync(category);
        }

        public Task UpdateAsync(
            CompanyDocumentCategoryModel category)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(
            CompanyDocumentCategoryModel category)
        {
            _context.CompanyDocumentCategories
                .Remove(category);

            return Task.CompletedTask;
        }

        public async Task<bool> IsCategoryInUseAsync(
            int categoryId)
        {
            return await _context.CompanyDocuments
                .AnyAsync(x =>
                    x.DocumentCategoryId == categoryId);
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
