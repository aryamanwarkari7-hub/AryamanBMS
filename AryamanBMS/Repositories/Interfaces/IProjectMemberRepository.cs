using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IProjectMemberRepository
    {
        IQueryable<ProjectMemberModel> ProjectMembers { get; }

        Task<ProjectMemberModel?> GetByIdAsync(int id);

        Task<ProjectMemberModel?> GetByProjectAndEmployeeAsync(
            int projectId,
            int employeeId);

        Task AddAsync(ProjectMemberModel member);

        Task UpdateAsync(ProjectMemberModel member);

        Task DeleteAsync(ProjectMemberModel member);

        Task SaveAsync();

        Task UpdateMemberRoleAsync(
    int projectId,
    int employeeId,
    string roleInProject);
    }
}