using System.Security.Claims;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Services
{
    public class ProjectAccessService : IProjectAccessService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IProjectMemberRepository _projectMemberRepository;

        public ProjectAccessService(
            IEmployeeRepository employeeRepository,
            IProjectRepository projectRepository,
            IProjectMemberRepository projectMemberRepository)
        {
            _employeeRepository = employeeRepository;
            _projectRepository = projectRepository;
            _projectMemberRepository = projectMemberRepository;
        }

        public bool IsAdminOrHR(ClaimsPrincipal user)
        {
            return user.IsInRole("Admin") ||
                   user.IsInRole("HR") ||
                   user.IsInRole("Master");
        }

        public async Task<int?> GetCurrentEmployeeIdAsync(
            ClaimsPrincipal user)
        {
            var userId =
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            var employee =
                await _employeeRepository.Employees
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.ApplicationUserId == userId);

            return employee?.Id;
        }

        public async Task<bool> CanAccessProjectAsync(
            ClaimsPrincipal user,
            int projectId)
        {
            if (IsAdminOrHR(user))
            {
                return true;
            }

            var employeeId = await GetCurrentEmployeeIdAsync(user);

            

            if (!employeeId.HasValue)
            {
                return false;
            }

            bool isProjectManager =
                await _projectRepository.Projects.AnyAsync(p =>
                    p.Id == projectId &&
                    p.ProjectManagerId == employeeId.Value);

            if (isProjectManager)
            {
                return true;
            }

            return await _projectMemberRepository.ProjectMembers
                .AnyAsync(pm =>
                    pm.ProjectId == projectId &&
                    pm.EmployeeId == employeeId.Value &&
                    pm.IsActive);
        }

        public async Task<IQueryable<ProjectModel>> ApplyProjectFilterAsync(
            ClaimsPrincipal user,
            IQueryable<ProjectModel> projects)
        {
            if (IsAdminOrHR(user))
            {
                return projects;
            }

            

            var employeeId =
                await GetCurrentEmployeeIdAsync(user);

            if (!employeeId.HasValue)
            {
                return projects.Where(p => false);
            }

            var memberProjectIds =
                _projectMemberRepository.ProjectMembers
                    .Where(pm =>
                        pm.EmployeeId == employeeId.Value &&
                        pm.IsActive)
                    .Select(pm => pm.ProjectId);

            return projects.Where(p =>
                p.ProjectManagerId == employeeId.Value ||
                memberProjectIds.Contains(p.Id));
        }
    }
}
