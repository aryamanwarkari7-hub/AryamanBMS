using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces;

public interface IDepartmentRepository
{
    IQueryable<DepartmentModel> Departments { get; }

    Task<List<DepartmentModel>> GetAllAsync();

    Task<DepartmentModel?> GetByIdAsync(int id);

    Task AddAsync(DepartmentModel department);

    Task UpdateAsync(DepartmentModel department);

    Task DeleteAsync(DepartmentModel department);

    Task SaveAsync();
}