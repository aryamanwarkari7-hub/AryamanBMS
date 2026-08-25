using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Repositories.Implementations
{
    public class EmployeePreviousEmploymentRepository
        : IEmployeePreviousEmploymentRepository
    {
        private readonly ApplicationDbContext _context;

        public EmployeePreviousEmploymentRepository(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<EmployeePreviousEmploymentModel>
            PreviousEmployments =>
                _context.EmployeePreviousEmployments;

        public async Task AddAsync(
            EmployeePreviousEmploymentModel previousEmployment)
        {
            await _context.EmployeePreviousEmployments
                .AddAsync(previousEmployment);
        }

        public Task UpdateAsync(
            EmployeePreviousEmploymentModel previousEmployment)
        {
            _context.EmployeePreviousEmployments
                .Update(previousEmployment);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(
            EmployeePreviousEmploymentModel previousEmployment)
        {
            _context.EmployeePreviousEmployments
                .Remove(previousEmployment);

            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}