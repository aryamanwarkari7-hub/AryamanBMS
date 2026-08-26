using AryamanBMS.Models;

namespace AryamanBMS.Business.Interfaces;

public interface IExpenseVoucherCreateService
{
    Task<ExpenseVoucherCreateValidationData> ValidateAsync(
        ExpenseVoucherModel voucher);

    Task<ExpenseVoucherCreateValidationData> ValidateForUpdateAsync(
        ExpenseVoucherModel voucher,
        ExpenseVoucherModel existingVoucher);

    Task CreateAsync(
        ExpenseVoucherModel voucher,
        ExpenseCategoryModel? category,
        string userId,
        string financialYear);

    Task UpdateAsync(
        ExpenseVoucherModel existingVoucher,
        ExpenseVoucherModel voucher,
        ExpenseCategoryModel? category);
}
