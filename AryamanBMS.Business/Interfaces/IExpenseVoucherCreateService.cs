using AryamanBMS.Models;

namespace AryamanBMS.Business.Interfaces;

public interface IExpenseVoucherCreateService
{
    Task<ExpenseVoucherCreateValidationData> ValidateAsync(
        ExpenseVoucherModel voucher);

    Task CreateAsync(
        ExpenseVoucherModel voucher,
        ExpenseCategoryModel? category,
        string userId,
        string financialYear);
}
