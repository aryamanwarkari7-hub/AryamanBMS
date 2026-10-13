using System.Data;
using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories.Implementations;

public class VendorPaymentRepository : IVendorPaymentRepository
{
    private readonly ApplicationDbContext _context;

    public VendorPaymentRepository(ApplicationDbContext context) => _context = context;

    public Task<List<VendorPaymentModel>> GetActiveWithDetailsAsync() =>
        _context.VendorPayments
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Include(x => x.Vendor)
            .Include(x => x.ExpenseVoucher)
            .ToListAsync();

    public Task<List<VendorModel>> GetActiveVendorsAsync() =>
        _context.Vendors
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.VendorName)
            .ToListAsync();

    public Task<List<ExpenseVoucherModel>> GetPayablePostedVouchersAsync() =>
        _context.ExpenseVouchers
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                x.Status == FinancialConstants.ExpenseVoucherStatus.Posted &&
                x.PaymentStatus != FinancialConstants.PaymentStatus.Paid)
            .Include(x => x.Vendor)
            .OrderByDescending(x => x.ExpenseVoucherId)
            .ToListAsync();

    public Task<ExpenseVoucherModel?> GetActiveVoucherWithPaymentsAsync(int expenseVoucherId) =>
        _context.ExpenseVouchers
            .Include(x => x.VendorPayments.Where(payment => payment.IsActive))
            .FirstOrDefaultAsync(x =>
                x.ExpenseVoucherId == expenseVoucherId &&
                x.IsActive);

    public Task<bool> TransactionReferenceExistsAsync(string transactionReference) =>
        _context.VendorPayments
            .AsNoTracking()
            .AnyAsync(x =>
                x.IsActive &&
                x.TransactionReference == transactionReference);

    public Task<int> GetPaymentCountForFinancialYearAsync(string financialYear) =>
        _context.VendorPayments
            .CountAsync(x => x.PaymentNo.Contains(financialYear));

    public async Task SavePaymentAndVoucherAsync(
        VendorPaymentModel payment,
        ExpenseVoucherModel voucher)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable);

        try
        {
            await _context.VendorPayments.AddAsync(payment);
            _context.ExpenseVouchers.Update(voucher);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
