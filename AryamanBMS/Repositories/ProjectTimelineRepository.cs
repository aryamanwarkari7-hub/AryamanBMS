using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class ProjectTimelineRepository
        : IProjectTimelineRepository
    {
        private readonly ApplicationDbContext _context;

        public ProjectTimelineRepository(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<ProjectTimelineModel> ProjectTimelines =>
            _context.ProjectTimelines;

        public async Task AddAsync(
            ProjectTimelineModel timeline)
        {
            await _context.ProjectTimelines.AddAsync(timeline);
        }

        public async Task<ProjectTimelineModel?> GetByIdAsync(
            int id)
        {
            return await _context.ProjectTimelines
                .Include(x => x.Project)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}