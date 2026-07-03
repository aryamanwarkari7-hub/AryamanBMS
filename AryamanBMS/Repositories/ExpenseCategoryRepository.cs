using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class ExpenseCategoryRepository : IExpenseCategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public ExpenseCategoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ExpenseCategoryModel>> GetAllActiveAsync()
        {
            return await _context.ExpenseCategories
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.CategoryName)
                .ToListAsync();
        }

        public async Task<ExpenseCategoryModel?> GetByIdAsync(int id)
        {
            return await _context.ExpenseCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ExpenseCategoryId == id && x.IsActive);
        }

        public async Task<ExpenseCategoryModel?> GetByCategoryCodeAsync(string code)
        {
            return await _context.ExpenseCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CategoryCode == code && x.IsActive);
        }

        public async Task<bool> CategoryCodeExistsAsync(string code, int? excludeId = null)
        {
            var query = _context.ExpenseCategories
                .Where(x => x.CategoryCode == code);

            if (excludeId.HasValue)
                query = query.Where(x => x.ExpenseCategoryId != excludeId);

            return await query.AnyAsync();
        }

        public async Task AddAsync(ExpenseCategoryModel model)
        {
            model.CreatedOn = DateTime.Now;
            await _context.ExpenseCategories.AddAsync(model);
        }

        public Task UpdateAsync(ExpenseCategoryModel model)
        {
            model.UpdatedOn = DateTime.Now;
            _context.ExpenseCategories.Update(model);
            return Task.CompletedTask;
        }

        public async Task SoftDeleteAsync(int id)
        {
            var category = await _context.ExpenseCategories.FindAsync(id);
            if (category != null)
            {
                category.IsActive = false;
                category.UpdatedOn = DateTime.Now;
                _context.ExpenseCategories.Update(category);
            }
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}