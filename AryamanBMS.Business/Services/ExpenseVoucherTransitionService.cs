using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Business.Services;

public class ExpenseVoucherTransitionService : IExpenseVoucherTransitionService
{
    private readonly IExpenseVoucherRepository _voucherRepository;

    public ExpenseVoucherTransitionService(IExpenseVoucherRepository voucherRepository)
    {
        _voucherRepository = voucherRepository;
    }

    public async Task<ExpenseVoucherTransitionResult> SubmitAsync(ExpenseVoucherModel voucher, string userId)
    {
        if (voucher.Status != FinancialConstants.ExpenseVoucherStatus.Draft)
            return Fail("Expense claim could not be submitted.");
        return await _voucherRepository.SubmitAsync(voucher.ExpenseVoucherId, userId) ? Ok() : Fail("Expense claim could not be submitted.");
    }

    public async Task<ExpenseVoucherTransitionResult> ApproveAsync(ExpenseVoucherModel voucher, string userId)
    {
        if (voucher.Status != FinancialConstants.ExpenseVoucherStatus.Submitted)
            return Fail("Only submitted expense vouchers can be approved.");
        return await _voucherRepository.ApproveAsync(voucher.ExpenseVoucherId, userId) ? Ok() : Fail("Expense voucher could not be approved.");
    }

    public async Task<ExpenseVoucherTransitionResult> RejectAsync(ExpenseVoucherModel voucher, string userId, string reason)
    {
        if (voucher.Status != FinancialConstants.ExpenseVoucherStatus.Submitted)
            return Fail("Only submitted expense vouchers can be rejected.");
        if (string.IsNullOrWhiteSpace(reason)) return Fail("Rejection reason is required.");
        if (reason.Trim().Length > 500) return Fail("Rejection reason cannot exceed 500 characters.");
        return await _voucherRepository.RejectAsync(voucher.ExpenseVoucherId, userId, reason) ? Ok() : Fail("Expense voucher could not be rejected.");
    }

    public async Task<ExpenseVoucherTransitionResult> PostAsync(ExpenseVoucherModel voucher, string userId)
    {
        return await _voucherRepository.PostAsync(voucher.ExpenseVoucherId, userId) ? Ok() : Fail("Only approved expense vouchers can be posted.");
    }

    public async Task<ExpenseVoucherTransitionResult> ReverseAsync(ExpenseVoucherModel voucher, string userId, string reason)
    {
        if (voucher.Status != FinancialConstants.ExpenseVoucherStatus.Posted) return Fail("Only posted expense vouchers can be reversed.");
        if (voucher.IsReversed) return Fail("This expense voucher is already reversed.");
        if (string.IsNullOrWhiteSpace(reason)) return Fail("Reversal reason is required.");
        voucher.IsReversed = true; voucher.ReversalReason = reason.Trim(); voucher.ReversedByUserId = userId; voucher.ReversedOn = DateTime.Now; voucher.Status = FinancialConstants.ExpenseVoucherStatus.Reversed; voucher.ApprovalStatus = FinancialConstants.ExpenseVoucherStatus.Reversed;
        await _voucherRepository.UpdateAsync(voucher); await _voucherRepository.SaveAsync(); return Ok();
    }

    public async Task<ExpenseVoucherTransitionResult> DeleteDraftAsync(ExpenseVoucherModel voucher)
    {
        if (voucher.Status != FinancialConstants.ExpenseVoucherStatus.Draft) return Fail("Only accessible draft expense vouchers can be deleted.");
        await _voucherRepository.SoftDeleteAsync(voucher.ExpenseVoucherId); await _voucherRepository.SaveAsync(); return Ok();
    }

    private static ExpenseVoucherTransitionResult Ok() => new() { Succeeded = true };
    private static ExpenseVoucherTransitionResult Fail(string message) => new() { ErrorMessage = message };
}
