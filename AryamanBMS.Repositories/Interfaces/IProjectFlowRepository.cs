using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IProjectFlowRepository
    {
        IQueryable<ProjectFlowModel> ProjectFlows { get; }

        Task<ProjectFlowModel?> GetByIdAsync(int id);

        Task<ProjectFlowModel?> GetDetailsAsync(int id);

        Task AddAsync(ProjectFlowModel flow);

        Task UpdateAsync(ProjectFlowModel flow);

        Task DeleteAsync(ProjectFlowModel flow);

        Task SaveAsync();
    }
}