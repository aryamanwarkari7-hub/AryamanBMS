using AryamanBMS.Models;

namespace AryamanBMS.Business.Interfaces;

public interface IVendorService
{
    Task<List<VendorModel>> GetActiveAsync(
        string? search,
        string sortBy,
        string sortOrder);

    Task<VendorModel?> GetActiveByIdAsync(int id);

    Task<IReadOnlyDictionary<string, string>> ValidateForCreateAsync(
        VendorModel vendor);

    Task<IReadOnlyDictionary<string, string>> ValidateForUpdateAsync(
        VendorModel vendor);

    Task CreateAsync(VendorModel vendor, string? createdByUserId);

    Task<VendorModel?> UpdateAsync(VendorModel vendor);

    Task<bool> DeactivateAsync(int id);
}
