using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces;

public interface IVendorRepository
{
    Task<List<VendorModel>> GetActiveAsync();
    Task<VendorModel?> GetActiveByIdAsync(int id);
    Task<VendorModel?> GetTrackedActiveByIdAsync(int id);
    Task<VendorModel?> GetTrackedByIdAsync(int id);
    Task<bool> VendorCodeExistsAsync(string vendorCode, int? excludingVendorId = null);
    Task AddAsync(VendorModel vendor);
    Task SaveAsync();
}
