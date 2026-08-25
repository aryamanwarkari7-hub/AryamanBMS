using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories;

public class VendorRepository : IVendorRepository
{
    private readonly ApplicationDbContext _context;

    public VendorRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<VendorModel>> GetActiveAsync()
    {
        return await _context.Vendors
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync();
    }

    public async Task<VendorModel?> GetActiveByIdAsync(int id)
    {
        return await _context.Vendors
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.VendorId == id && x.IsActive);
    }

    public async Task<VendorModel?> GetTrackedActiveByIdAsync(int id)
    {
        return await _context.Vendors
            .FirstOrDefaultAsync(x => x.VendorId == id && x.IsActive);
    }

    public async Task<VendorModel?> GetTrackedByIdAsync(int id)
    {
        return await _context.Vendors.FindAsync(id);
    }

    public async Task<bool> VendorCodeExistsAsync(
        string vendorCode,
        int? excludingVendorId = null)
    {
        return await _context.Vendors.AnyAsync(x =>
            x.VendorCode == vendorCode &&
            (!excludingVendorId.HasValue ||
                x.VendorId != excludingVendorId.Value));
    }

    public async Task AddAsync(VendorModel vendor)
    {
        await _context.Vendors.AddAsync(vendor);
    }

    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }
}
