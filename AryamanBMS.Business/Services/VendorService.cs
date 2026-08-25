using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using System.Text.RegularExpressions;

namespace AryamanBMS.Business.Services;

public class VendorService : IVendorService
{
    private readonly IVendorRepository _vendorRepository;

    private static readonly Regex GstinRegex =
        new(
            @"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public VendorService(IVendorRepository vendorRepository)
    {
        _vendorRepository = vendorRepository;
    }

    public async Task<List<VendorModel>> GetActiveAsync(
        string? search,
        string sortBy,
        string sortOrder)
    {
        var vendors = await _vendorRepository.GetActiveAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim().ToLower();

            vendors = vendors
                .Where(x =>
                    x.VendorCode.ToLower().Contains(keyword) ||
                    x.VendorName.ToLower().Contains(keyword) ||
                    (!string.IsNullOrWhiteSpace(x.GSTIN) &&
                        x.GSTIN.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(x.PAN) &&
                        x.PAN.ToLower().Contains(keyword)))
                .ToList();
        }

        bool descending = string.Equals(
            sortOrder,
            "desc",
            StringComparison.OrdinalIgnoreCase);

        return sortBy switch
        {
            "VendorCode" => descending
                ? vendors.OrderByDescending(x => x.VendorCode).ToList()
                : vendors.OrderBy(x => x.VendorCode).ToList(),

            "GSTIN" => descending
                ? vendors.OrderByDescending(x => x.GSTIN).ToList()
                : vendors.OrderBy(x => x.GSTIN).ToList(),

            "PAN" => descending
                ? vendors.OrderByDescending(x => x.PAN).ToList()
                : vendors.OrderBy(x => x.PAN).ToList(),

            "Status" => descending
                ? vendors.OrderByDescending(x => x.IsActive).ToList()
                : vendors.OrderBy(x => x.IsActive).ToList(),

            _ => descending
                ? vendors.OrderByDescending(x => x.VendorName).ToList()
                : vendors.OrderBy(x => x.VendorName).ToList()
        };
    }

    public async Task<VendorModel?> GetActiveByIdAsync(int id)
    {
        return await _vendorRepository.GetActiveByIdAsync(id);
    }

    public async Task<IReadOnlyDictionary<string, string>> ValidateForCreateAsync(
        VendorModel vendor)
    {
        Normalize(vendor);

        var errors = Validate(vendor);

        if (await _vendorRepository.VendorCodeExistsAsync(vendor.VendorCode))
        {
            errors[nameof(vendor.VendorCode)] =
                "This vendor code already exists.";
        }

        return errors;
    }

    public async Task<IReadOnlyDictionary<string, string>> ValidateForUpdateAsync(
        VendorModel vendor)
    {
        Normalize(vendor);

        var errors = Validate(vendor);

        if (await _vendorRepository.VendorCodeExistsAsync(
                vendor.VendorCode,
                vendor.VendorId))
        {
            errors[nameof(vendor.VendorCode)] =
                "This vendor code already exists.";
        }

        return errors;
    }

    public async Task CreateAsync(
        VendorModel vendor,
        string? createdByUserId)
    {
        vendor.CreatedByUserId = createdByUserId;
        vendor.CreatedOn = DateTime.Now;
        vendor.IsActive = true;

        await _vendorRepository.AddAsync(vendor);
        await _vendorRepository.SaveAsync();
    }

    public async Task<VendorModel?> UpdateAsync(VendorModel vendor)
    {
        var existing = await _vendorRepository
            .GetTrackedActiveByIdAsync(vendor.VendorId);

        if (existing == null)
        {
            return null;
        }

        existing.VendorCode = vendor.VendorCode;
        existing.VendorName = vendor.VendorName;
        existing.GSTIN = vendor.GSTIN;
        existing.PAN = vendor.PAN;
        existing.State = vendor.State;
        existing.StateCode = vendor.StateCode;
        existing.Address = vendor.Address;
        existing.RegistrationType = vendor.RegistrationType;
        existing.PaymentTerms = vendor.PaymentTerms;
        existing.BankDetails = vendor.BankDetails;
        existing.IsActive = vendor.IsActive;
        existing.UpdatedOn = DateTime.Now;

        await _vendorRepository.SaveAsync();

        return existing;
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        var vendor = await _vendorRepository.GetTrackedByIdAsync(id);

        if (vendor == null)
        {
            return false;
        }

        vendor.IsActive = false;
        vendor.UpdatedOn = DateTime.Now;

        await _vendorRepository.SaveAsync();

        return true;
    }

    private static Dictionary<string, string> Validate(VendorModel vendor)
    {
        var errors = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(vendor.GSTIN) &&
            !GstinRegex.IsMatch(vendor.GSTIN))
        {
            errors[nameof(vendor.GSTIN)] =
                "Enter a valid 15-character GSTIN.";
        }

        return errors;
    }

    private static void Normalize(VendorModel vendor)
    {
        vendor.VendorCode =
            vendor.VendorCode?.Trim().ToUpperInvariant() ?? string.Empty;
        vendor.VendorName = vendor.VendorName?.Trim() ?? string.Empty;
        vendor.GSTIN = NormalizeOptional(vendor.GSTIN, true);
        vendor.PAN = NormalizeOptional(vendor.PAN, true);
        vendor.State = NormalizeOptional(vendor.State);
        vendor.StateCode = NormalizeOptional(vendor.StateCode);
        vendor.Address = NormalizeOptional(vendor.Address);
        vendor.RegistrationType = NormalizeOptional(vendor.RegistrationType);
        vendor.PaymentTerms = NormalizeOptional(vendor.PaymentTerms);
        vendor.BankDetails = NormalizeOptional(vendor.BankDetails);
    }

    private static string? NormalizeOptional(
        string? value,
        bool uppercase = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Trim();

        return uppercase
            ? value.ToUpperInvariant()
            : value;
    }
}
