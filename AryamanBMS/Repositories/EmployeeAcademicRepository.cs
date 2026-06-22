using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Repositories
{
    public class EmployeeAcademicRepository
        : IEmployeeAcademicRepository
    {
        private readonly ApplicationDbContext _context;

        public EmployeeAcademicRepository(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<EmployeeAcademicModel> Academics =>
            _context.EmployeeAcademics;

        public async Task AddAsync(
            EmployeeAcademicModel academic)
        {
            await _context.EmployeeAcademics.AddAsync(academic);
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public Task UpdateAsync(EmployeeAcademicModel academic)
        {
            _context.EmployeeAcademics.Update(academic);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(EmployeeAcademicModel academic)
        {
            _context.EmployeeAcademics.Remove(academic);
            return Task.CompletedTask;
        }
    }
}