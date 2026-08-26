using AryamanBMS.Models;

namespace AryamanBMS.Business.Interfaces;

public interface IExpenseVoucherTransitionService
{
    Task<ExpenseVoucherTransitionResult> SubmitAsync(ExpenseVoucherModel voucher, string userId);
    Task<ExpenseVoucherTransitionResult> ApproveAsync(ExpenseVoucherModel voucher, string userId);
    Task<ExpenseVoucherTransitionResult> RejectAsync(ExpenseVoucherModel voucher, string userId, string reason);
    Task<ExpenseVoucherTransitionResult> PostAsync(ExpenseVoucherModel voucher, string userId);
    Task<ExpenseVoucherTransitionResult> ReverseAsync(ExpenseVoucherModel voucher, string userId, string reason);
    Task<ExpenseVoucherTransitionResult> DeleteDraftAsync(ExpenseVoucherModel voucher);
}
