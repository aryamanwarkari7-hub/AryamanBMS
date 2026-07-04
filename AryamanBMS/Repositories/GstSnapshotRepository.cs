
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
        if (string.Equals(
                snapshot.Status,
                "Filed",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Filed GST snapshots cannot be deleted.");
        }

        throw new InvalidOperationException(
            "GST snapshots should be recalculated, not deleted.");
    }

    public async Task<bool> LockAsync(
    int month,
    int year,
    string filedByUserId)
    {
        var snapshot = await _context.GstMonthlySnapshots
            .FirstOrDefaultAsync(x =>
                x.Month == month &&
                x.Year == year);

        if (snapshot == null)
            return false;

        if (string.Equals(
                snapshot.Status,
                "Filed",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        snapshot.Status = "Filed";
        snapshot.FiledOn = DateTime.Now;
        snapshot.FiledByUserId = filedByUserId;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ReopenAsync(
    int month,
    int year,
    string reopenedByUserId,
    string reason)
    {
        var snapshot = await _context.GstMonthlySnapshots
            .FirstOrDefaultAsync(x =>
                x.Month == month &&
                x.Year == year);

        if (snapshot == null)
            return false;

        if (!string.Equals(
                snapshot.Status,
                "Filed",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        snapshot.Status = "Calculated";
        snapshot.ReopenedByUserId = reopenedByUserId;
        snapshot.ReopenedOn = DateTime.Now;
        snapshot.ReopenReason = reason.Trim();

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }
}

