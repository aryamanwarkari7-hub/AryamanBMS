using AryamanBMS.Models;

namespace AryamanBMS.Business.Interfaces;

public interface IVendorPaymentService
{
    Task<List<VendorPaymentModel>> GetAllAsync(
        int? vendorId,
        string? search,
        DateTime? fromDate,
        DateTime? toDate,
        string sortBy,
        string sortOrder);

    Task<List<VendorModel>> GetActiveVendorsAsync();
    Task<List<ExpenseVoucherModel>> GetPayablePostedVouchersAsync();
    Task<VendorPaymentModel> CreateDraftAsync(int? expenseVoucherId);
    Task<IReadOnlyDictionary<string, string>> ValidateAsync(VendorPaymentModel payment);
    Task<VendorPaymentModel> CreateAsync(VendorPaymentModel payment, string paidByUserId);
}
