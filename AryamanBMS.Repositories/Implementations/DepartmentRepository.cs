using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly ApplicationDbContext _context;

    public DepartmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public IQueryable<DepartmentModel> Departments =>
        _context.Departments;

    public async Task<List<DepartmentModel>> GetAllAsync()
    {
        return await _context.Departments.ToListAsync();
    }

    public async Task<DepartmentModel?> GetByIdAsync(int id)
    {
        return await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task AddAsync(DepartmentModel department)
    {
        await _context.Departments.AddAsync(department);
    }

    public Task UpdateAsync(DepartmentModel department)
    {
        _context.Departments.Update(department);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(DepartmentModel department)
    {
        _context.Departments.Remove(department);
        return Task.CompletedTask;
    }

    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }
}