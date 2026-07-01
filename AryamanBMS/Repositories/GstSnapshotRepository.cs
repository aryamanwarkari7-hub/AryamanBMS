
using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories;

public class GstSnapshotRepository : IGstSnapshotRepository
{
    private readonly ApplicationDbContext _context;

    public GstSnapshotRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    private IQueryable<GstMonthlySnapshotModel> Snapshots =>
        _context.GstMonthlySnapshots
            .Include(x => x.Returns)
            .Include(x => x.Challans)
            .Include(x => x.ItcRecords)
            .Include(x => x.Documents);

    public async Task<List<GstMonthlySnapshotModel>> GetAllAsync()
    {
        return await Snapshots
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .ToListAsync();
    }

    public async Task<GstMonthlySnapshotModel?> GetByIdAsync(int id)
    {
        return await Snapshots
            .FirstOrDefaultAsync(x => x.SnapshotId == id);
    }

    public async Task<GstMonthlySnapshotModel?> GetByMonthYearAsync(int month, int year)
    {
        return await Snapshots
            .FirstOrDefaultAsync(x => x.Month == month && x.Year == year);
    }

    public async Task AddAsync(GstMonthlySnapshotModel snapshot)
    {
        snapshot.GeneratedOn = DateTime.Now;

        await _context.GstMonthlySnapshots.AddAsync(snapshot);
    }

    public Task UpdateAsync(GstMonthlySnapshotModel snapshot)
    {
        _context.GstMonthlySnapshots.Update(snapshot);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(GstMonthlySnapshotModel snapshot)
    {
        _context.GstMonthlySnapshots.Remove(snapshot);

        return Task.CompletedTask;
    }

    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }
}

