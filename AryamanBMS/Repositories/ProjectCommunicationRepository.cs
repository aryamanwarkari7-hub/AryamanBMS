using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class ProjectCommunicationRepository
        : IProjectCommunicationRepository
    {
        private readonly ApplicationDbContext _context;

        public ProjectCommunicationRepository(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<ProjectCommunicationModel>
            ProjectCommunications =>
            _context.ProjectCommunications
                .Include(x => x.Project)
                .Include(x => x.CreatedByEmployee)
                .Include(x => x.CreatedByUser);

        public async Task<List<ProjectCommunicationModel>>
            GetByProjectIdAsync(int projectId)
        {
            return await ProjectCommunications
               .Include(x => x.Project)
               .Include(x => x.CreatedByEmployee)
               .Include(x => x.CreatedByUser)
               .Include(x => x.ClientCommunication)
               .Where(x =>
                   x.ProjectId == projectId &&
                   x.IsActive)
               .OrderByDescending(x => x.CreatedOn)
               .ToListAsync();
        }

        public async Task<ProjectCommunicationModel?>
            GetByIdAsync(int id)
        {
            return await ProjectCommunications
                .FirstOrDefaultAsync(x =>
                    x.Id == id);
        }

        public async Task AddAsync(
            ProjectCommunicationModel communication)
        {
            _context.ProjectCommunications
                .Add(communication);

            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(
            ProjectCommunicationModel communication)
        {
            _context.ProjectCommunications
                .Update(communication);

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(
            ProjectCommunicationModel communication)
        {
            communication.IsActive = false;
            communication.UpdatedOn = DateTime.Now;

            _context.ProjectCommunications
                .Update(communication);

            await _context.SaveChangesAsync();
        }


    }
}