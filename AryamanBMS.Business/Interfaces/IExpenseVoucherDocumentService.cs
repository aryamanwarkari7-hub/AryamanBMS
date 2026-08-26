using AryamanBMS.Models;

namespace AryamanBMS.Business.Interfaces;

public interface IExpenseVoucherDocumentService
{
    Task CreateAsync(ExpenseVoucherDocumentModel document);

    Task<bool> DeleteAsync(ExpenseVoucherDocumentModel document);
}
