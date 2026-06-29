using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class ProjectMemberRepository : IProjectMemberRepository
    {
        private readonly ApplicationDbContext _context;

        public ProjectMemberRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<ProjectMemberModel> ProjectMembers =>
            _context.ProjectMembers
                .Include(pm => pm.Project)
                .Include(pm => pm.Employee)
                    .ThenInclude(e => e!.Department)
                .Include(pm => pm.Employee)
                    .ThenInclude(e => e!.Designation)
                .AsNoTracking();

        public async Task<ProjectMemberModel?> GetByIdAsync(int id)
        {
            return await _context.ProjectMembers
                .Include(pm => pm.Project)
                .Include(pm => pm.Employee)
                .FirstOrDefaultAsync(pm => pm.Id == id);
        }

        public async Task<ProjectMemberModel?> GetByProjectAndEmployeeAsync(
            int projectId,
            int employeeId)
        {
            return await _context.ProjectMembers
                .FirstOrDefaultAsync(pm =>
                    pm.ProjectId == projectId &&
                    pm.EmployeeId == employeeId);
        }

        public async Task AddAsync(ProjectMemberModel member)
        {
            await _context.ProjectMembers.AddAsync(member);
        }

        public Task UpdateAsync(ProjectMemberModel member)
        {
            // Prevent EF Core from attaching navigation objects again.
            member.Project = null;
            member.Employee = null;

            _context.Entry(member).State =
                EntityState.Modified;

            return Task.CompletedTask;
        }

        public Task DeleteAsync(ProjectMemberModel member)
        {
            _context.ProjectMembers.Remove(member);
            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task UpdateMemberRoleAsync(
    int projectId,
    int employeeId,
    string roleInProject)
        {
            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm =>
                    pm.ProjectId == projectId &&
                    pm.EmployeeId == employeeId);

            if (member == null)
                return;

            member.RoleInProject = roleInProject;
            member.IsActive = true;
        }
    }
}