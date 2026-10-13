using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces;

public interface IVendorPaymentRepository
{
    Task<List<VendorPaymentModel>> GetActiveWithDetailsAsync();
    Task<List<VendorModel>> GetActiveVendorsAsync();
    Task<List<ExpenseVoucherModel>> GetPayablePostedVouchersAsync();
    Task<ExpenseVoucherModel?> GetActiveVoucherWithPaymentsAsync(int expenseVoucherId);
    Task<bool> TransactionReferenceExistsAsync(string transactionReference);
    Task<int> GetPaymentCountForFinancialYearAsync(string financialYear);
    Task SavePaymentAndVoucherAsync(VendorPaymentModel payment, ExpenseVoucherModel voucher);
}
