using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class ProjectTaskProgressRepository
        : IProjectTaskProgressRepository
    {
        private readonly ApplicationDbContext _context;

        public ProjectTaskProgressRepository(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<ProjectTaskProgressModel> ProjectTaskProgresses =>
            _context.ProjectTaskProgresses
                .Include(p => p.ProjectTask)
                .ThenInclude(t => t!.Project);

        public async Task<ProjectTaskProgressModel?> GetByIdAsync(int id)
        {
            return await _context.ProjectTaskProgresses
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<ProjectTaskProgressModel?> GetDetailsAsync(int id)
        {
            return await _context.ProjectTaskProgresses
                .Include(p => p.ProjectTask)
                .ThenInclude(t => t!.Project)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task AddAsync(ProjectTaskProgressModel progress)
        {
            await _context.ProjectTaskProgresses.AddAsync(progress);
        }

        public Task UpdateAsync(ProjectTaskProgressModel progress)
        {
            _context.ProjectTaskProgresses.Update(progress);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ProjectTaskProgressModel progress)
        {
            _context.ProjectTaskProgresses.Remove(progress);
            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}