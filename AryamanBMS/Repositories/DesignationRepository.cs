using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories;

public class DesignationRepository : IDesignationRepository
{
    private readonly ApplicationDbContext _context;

    public DesignationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public IQueryable<DesignationModel> Designations =>
     _context.Designations;

    public async Task<List<DesignationModel>> GetAllAsync()
    {
        return await _context.Designations
            .Include(d => d.Department)
            .ToListAsync();
    }

    public async Task<DesignationModel?> GetByIdAsync(int id)
    {
        return await _context.Designations
            .Include(d => d.Department)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task AddAsync(DesignationModel designation)
    {
        await _context.Designations.AddAsync(designation);
    }

    public Task UpdateAsync(DesignationModel designation)
    {
        _context.Designations.Update(designation);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(DesignationModel designation)
    {
        _context.Designations.Remove(designation);
        return Task.CompletedTask;
    }

    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }
}