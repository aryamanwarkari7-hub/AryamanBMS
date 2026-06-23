using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class ProjectFlowRepository : IProjectFlowRepository
    {
        private readonly ApplicationDbContext _context;

        public ProjectFlowRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<ProjectFlowModel> ProjectFlows =>
            _context.ProjectFlows
                .Include(pf => pf.Project)
                .AsNoTracking();

        public async Task<ProjectFlowModel?> GetByIdAsync(int id)
        {
            return await _context.ProjectFlows
                .FirstOrDefaultAsync(pf => pf.Id == id);
        }

        public async Task<ProjectFlowModel?> GetDetailsAsync(int id)
        {
            return await _context.ProjectFlows
                .Include(pf => pf.Project)
                .FirstOrDefaultAsync(pf => pf.Id == id);
        }

        public async Task AddAsync(ProjectFlowModel flow)
        {
            await _context.ProjectFlows.AddAsync(flow);
        }

        public Task UpdateAsync(ProjectFlowModel flow)
        {
            _context.ProjectFlows.Update(flow);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ProjectFlowModel flow)
        {
            _context.ProjectFlows.Remove(flow);
            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}