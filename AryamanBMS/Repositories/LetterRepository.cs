using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class LetterRepository : ILetterRepository
    {
        private readonly ApplicationDbContext _context;

        public LetterRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<LetterModel> Letters =>
            _context.Letters
                .Include(x => x.Employee);

        public async Task<LetterModel?> GetByIdAsync(int id)
        {
            return await _context.Letters
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddAsync(LetterModel letter)
        {
            await _context.Letters.AddAsync(letter);
        }

        public Task UpdateAsync(LetterModel letter)
        {
            _context.Letters.Update(letter);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(LetterModel letter)
        {
            _context.Letters.Remove(letter);

            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}