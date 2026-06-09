using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly ApplicationDbContext _context;

    public EmployeeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public IQueryable<EmployeeModel> Employees =>
        _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .Include(e => e.ApplicationUser);

    public async Task<List<EmployeeModel>> GetAllAsync()
    {
        return await Employees.ToListAsync();
    }

    public async Task<EmployeeModel?> GetByIdAsync(int id)
    {
        return await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<EmployeeModel?> GetDetailsAsync(int id)
    {
        return await Employees
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task AddAsync(EmployeeModel employee)
    {
        await _context.Employees.AddAsync(employee);
    }

    public Task UpdateAsync(EmployeeModel employee)
    {
        _context.Employees.Update(employee);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(EmployeeModel employee)
    {
        _context.Employees.Remove(employee);
        return Task.CompletedTask;
    }

    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }
}