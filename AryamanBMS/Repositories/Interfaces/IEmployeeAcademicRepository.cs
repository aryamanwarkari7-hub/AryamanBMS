using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IEmployeeAcademicRepository
    {
        IQueryable<EmployeeAcademicModel> Academics { get; }

        Task AddAsync(EmployeeAcademicModel academic);
        Task UpdateAsync(EmployeeAcademicModel academic);
        Task DeleteAsync(EmployeeAcademicModel academic);
        Task SaveAsync();
    }
}