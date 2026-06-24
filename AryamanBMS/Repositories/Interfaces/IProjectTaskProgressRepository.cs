using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IProjectTaskProgressRepository
    {
        IQueryable<ProjectTaskProgressModel> ProjectTaskProgresses { get; }

        Task<ProjectTaskProgressModel?> GetByIdAsync(int id);

        Task<ProjectTaskProgressModel?> GetDetailsAsync(int id);

        Task AddAsync(ProjectTaskProgressModel progress);

        Task UpdateAsync(ProjectTaskProgressModel progress);

        Task DeleteAsync(ProjectTaskProgressModel progress);

        Task SaveAsync();
    }
}