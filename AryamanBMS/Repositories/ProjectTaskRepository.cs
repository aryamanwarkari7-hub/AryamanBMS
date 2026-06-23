using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class ProjectTaskRepository : IProjectTaskRepository
    {
        private readonly ApplicationDbContext _context;

        public ProjectTaskRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<ProjectTaskModel> ProjectTasks =>
            _context.ProjectTasks
                .Include(t => t.Project)
                .Include(t => t.AssignedEmployee)
                .AsNoTracking();

        public async Task<ProjectTaskModel?> GetByIdAsync(int id)
        {
            return await _context.ProjectTasks
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<ProjectTaskModel?> GetDetailsAsync(int id)
        {
            return await _context.ProjectTasks
                .Include(t => t.Project)
                .Include(t => t.AssignedEmployee)
                    .ThenInclude(e => e.Department)
                .Include(t => t.AssignedEmployee)
                    .ThenInclude(e => e.Designation)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task AddAsync(ProjectTaskModel task)
        {
            await _context.ProjectTasks.AddAsync(task);
        }

        public Task UpdateAsync(ProjectTaskModel task)
        {
            _context.ProjectTasks.Update(task);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ProjectTaskModel task)
        {
            _context.ProjectTasks.Remove(task);
            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}