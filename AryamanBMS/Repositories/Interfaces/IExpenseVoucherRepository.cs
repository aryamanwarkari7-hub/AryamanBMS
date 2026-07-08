using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IExpenseVoucherRepository
    {
        Task<IEnumerable<ExpenseVoucherModel>> GetAllAsync();

        Task<IEnumerable<ExpenseVoucherModel>> GetByStatusAsync(string status);

        Task<IEnumerable<ExpenseVoucherModel>> GetByFinancialYearAsync(string financialYear);

        Task<IEnumerable<ExpenseVoucherModel>> GetByCategoryAsync(int categoryId);

        Task<ExpenseVoucherModel?> GetByIdAsync(int id);

        Task<ExpenseVoucherModel?> GetByVoucherNumberAsync(string voucherNumber);

        Task<bool> VoucherNumberExistsAsync(string voucherNumber, int? excludeId = null);

        Task<int> GetNextVoucherSequenceAsync(string financialYear);

        Task AddAsync(ExpenseVoucherModel model);

        Task UpdateAsync(ExpenseVoucherModel model);

        Task<bool> ApproveAsync(
            int id,
            string approvedByUserId);

        Task<bool> RejectAsync(
            int id,
            string rejectedByUserId,
            string rejectionReason);

        Task<bool> SubmitAsync(int id, string submittedByUserId);

        Task<bool> PostAsync(int id, string postedByUserId);

        Task SoftDeleteAsync(int id);

        Task SaveAsync();

        Task CreateWithSequenceAsync(ExpenseVoucherModel model);

        Task<bool> VendorInvoiceExistsAsync(
            int? vendorId,
            string? vendorGstin,
            string invoiceNumber,
            string financialYear,
            int? excludeId = null);

        Task<ExpenseVoucherDocumentModel?> GetDocumentByIdAsync(int id);

        Task AddDocumentAsync(ExpenseVoucherDocumentModel document);

        Task DeleteDocumentAsync(ExpenseVoucherDocumentModel document);


    }
}
