using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces;

public interface IEmployeeRepository
{
    IQueryable<EmployeeModel> Employees { get; }
    Task<List<EmployeeModel>> GetAllAsync();

    Task<EmployeeModel?> GetByIdAsync(int id);

    Task<EmployeeModel?> GetDetailsAsync(int id);

    Task AddAsync(EmployeeModel employee);

    Task UpdateAsync(EmployeeModel employee);

    Task DeleteAsync(EmployeeModel employee);

    Task SaveAsync();
}