using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IProjectTaskRepository
    {
        IQueryable<ProjectTaskModel> ProjectTasks { get; }

        Task<ProjectTaskModel?> GetByIdAsync(int id);

        Task<ProjectTaskModel?> GetDetailsAsync(int id);

        Task AddAsync(ProjectTaskModel task);

        Task UpdateAsync(ProjectTaskModel task);

        Task DeleteAsync(ProjectTaskModel task);

        Task SaveAsync();
    }
}