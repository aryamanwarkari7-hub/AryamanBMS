using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IProjectRepository
    {
        IQueryable<ProjectModel> Projects { get; }

        Task<ProjectModel?> GetByIdAsync(int id);

        Task<ProjectModel?> GetDetailsAsync(int id);

        Task AddAsync(ProjectModel project);

        Task UpdateAsync(ProjectModel project);

        Task DeleteAsync(ProjectModel project);

        Task SaveAsync();
    }
}