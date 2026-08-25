using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly ApplicationDbContext _context;

    public AttendanceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public IQueryable<AttendanceModel> Attendances =>
        _context.Attendances
            .Include(a => a.Employee);

    public async Task<List<AttendanceModel>> GetAllAsync()
    {
        return await Attendances.ToListAsync();
    }

    public async Task<AttendanceModel?> GetByIdAsync(int id)
    {
        return await _context.Attendances
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task AddAsync(AttendanceModel attendance)
    {
        await _context.Attendances.AddAsync(attendance);
    }

    public Task UpdateAsync(AttendanceModel attendance)
    {
        _context.Attendances.Update(attendance);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(AttendanceModel attendance)
    {
        _context.Attendances.Remove(attendance);
        return Task.CompletedTask;
    }

    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }
}