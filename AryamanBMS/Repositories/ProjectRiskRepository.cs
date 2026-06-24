using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class ProjectRiskRepository : IProjectRiskRepository
    {
        private readonly ApplicationDbContext _context;

        public ProjectRiskRepository(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<ProjectRiskModel> ProjectRisks =>
            _context.ProjectRisks
                .Include(r => r.Project)
                .Include(r => r.RiskOwnerEmployee);

        public async Task<ProjectRiskModel?> GetByIdAsync(int id)
        {
            return await _context.ProjectRisks
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<ProjectRiskModel?> GetDetailsAsync(int id)
        {
            return await _context.ProjectRisks
                .Include(r => r.Project)
                .Include(r => r.RiskOwnerEmployee)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<List<ProjectModel>> GetActiveProjectsAsync()
        {
            return await _context.Projects
                .Where(p => p.IsActive)
                .OrderBy(p => p.ProjectName)
                .ToListAsync();
        }

        public async Task<List<EmployeeModel>> GetActiveEmployeesAsync()
        {
            return await _context.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToListAsync();
        }

        public async Task AddAsync(ProjectRiskModel risk)
        {
            await _context.ProjectRisks.AddAsync(risk);
        }

        public Task UpdateAsync(ProjectRiskModel risk)
        {
            _context.ProjectRisks.Update(risk);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ProjectRiskModel risk)
        {
            _context.ProjectRisks.Remove(risk);
            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}