using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Business.Services;

public class ExpenseVoucherDocumentService : IExpenseVoucherDocumentService
{
    private readonly IExpenseVoucherRepository _voucherRepository;

    public ExpenseVoucherDocumentService(IExpenseVoucherRepository voucherRepository)
    {
        _voucherRepository = voucherRepository;
    }

    public async Task CreateAsync(ExpenseVoucherDocumentModel document)
    {
        await _voucherRepository.AddDocumentAsync(document);
        await _voucherRepository.SaveAsync();
    }

    public async Task<bool> DeleteAsync(ExpenseVoucherDocumentModel document)
    {
        if (document.ExpenseVoucher?.Status != FinancialConstants.ExpenseVoucherStatus.Draft)
        {
            return false;
        }

        await _voucherRepository.DeleteDocumentAsync(document);
        await _voucherRepository.SaveAsync();

        return true;
    }
}
