using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly ApplicationDbContext _context;

        public ProjectRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<ProjectModel> Projects =>
            _context.Projects
                .Include(p => p.ProjectManager)
                .AsNoTracking();

        public async Task<ProjectModel?> GetByIdAsync(int id)
        {
            return await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<ProjectModel?> GetDetailsAsync(int id)
        {
            return await _context.Projects
                .Include(p => p.ProjectManager)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task AddAsync(ProjectModel project)
        {
            await _context.Projects.AddAsync(project);
        }

        public Task UpdateAsync(ProjectModel project)
        {
            _context.Projects.Update(project);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ProjectModel project)
        {
            _context.Projects.Remove(project);
            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}