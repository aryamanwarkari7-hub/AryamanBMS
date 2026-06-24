using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IProjectRiskRepository
    {
        IQueryable<ProjectRiskModel> ProjectRisks { get; }

        Task<ProjectRiskModel?> GetByIdAsync(int id);

        Task<ProjectRiskModel?> GetDetailsAsync(int id);

        Task<List<ProjectModel>> GetActiveProjectsAsync();

        Task<List<EmployeeModel>> GetActiveEmployeesAsync();

        Task AddAsync(ProjectRiskModel risk);

        Task UpdateAsync(ProjectRiskModel risk);

        Task DeleteAsync(ProjectRiskModel risk);

        Task SaveAsync();
    }
}