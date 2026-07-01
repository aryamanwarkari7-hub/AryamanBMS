using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class ExpenseVoucherRepository : IExpenseVoucherRepository
    {
        private readonly ApplicationDbContext _context;

        public ExpenseVoucherRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ExpenseVoucherModel>> GetAllAsync()
        {
            return await _context.TableExpenseVouchers
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Include(x => x.Category)
                .OrderByDescending(x => x.VoucherDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<ExpenseVoucherModel>> GetByStatusAsync(string status)
        {
            return await _context.TableExpenseVouchers
                .AsNoTracking()
                .Where(x => x.Status == status && x.IsActive)
                .Include(x => x.Category)
                .OrderByDescending(x => x.VoucherDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<ExpenseVoucherModel>> GetByFinancialYearAsync(string financialYear)
        {
            return await _context.TableExpenseVouchers
                .AsNoTracking()
                .Where(x => x.FinancialYear == financialYear && x.IsActive)
                .Include(x => x.Category)
                .OrderByDescending(x => x.VoucherDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<ExpenseVoucherModel>> GetByCategoryAsync(int categoryId)
        {
            return await _context.TableExpenseVouchers
                .AsNoTracking()
                .Where(x => x.ExpenseCategoryId == categoryId && x.IsActive)
                .Include(x => x.Category)
                .OrderByDescending(x => x.VoucherDate)
                .ToListAsync();
        }

        public async Task<ExpenseVoucherModel?> GetByIdAsync(int id)
        {
            return await _context.TableExpenseVouchers
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.ExpenseVoucherId == id && x.IsActive);
        }

        public async Task<ExpenseVoucherModel?> GetByVoucherNumberAsync(string voucherNumber)
        {
            return await _context.TableExpenseVouchers
                .AsNoTracking()
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.VoucherNumber == voucherNumber && x.IsActive);
        }

        public async Task<bool> VoucherNumberExistsAsync(string voucherNumber, int? excludeId = null)
        {
            var query = _context.TableExpenseVouchers
                .Where(x => x.VoucherNumber == voucherNumber);

            if (excludeId.HasValue)
                query = query.Where(x => x.ExpenseVoucherId != excludeId);

            return await query.AnyAsync();
        }

        public async Task<int> GetNextVoucherSequenceAsync(string financialYear)
        {
            var sequence = await _context.FinancialSequences
                .FirstOrDefaultAsync(x =>
                    x.DocumentType == FinancialConstants.ExpenseVoucherPrefix &&
                    x.FinancialYear == financialYear);

            if (sequence == null)
            {
                sequence = new FinancialSequenceModel
                {
                    DocumentType = FinancialConstants.ExpenseVoucherPrefix,
                    FinancialYear = financialYear,
                    LastNumber = 1,
                    UpdatedOn = DateTime.Now
                };

                await _context.FinancialSequences.AddAsync(sequence);

                return sequence.LastNumber;
            }

            sequence.LastNumber += 1;
            sequence.UpdatedOn = DateTime.Now;

            _context.FinancialSequences.Update(sequence);

            return sequence.LastNumber;
        }
        public async Task AddAsync(ExpenseVoucherModel model)
        {
            model.CreatedOn = DateTime.Now;
            model.IsActive = true;
            await _context.TableExpenseVouchers.AddAsync(model);
        }

        public Task UpdateAsync(ExpenseVoucherModel model)
        {
            model.UpdatedOn = DateTime.Now;
            _context.TableExpenseVouchers.Update(model);
            return Task.CompletedTask;
        }

        public async Task ApproveAsync(int id, int approvedByUserId)
        {
            var voucher = await _context.TableExpenseVouchers.FindAsync(id);
            if (voucher != null)
            {
                voucher.Status = FinancialConstants.ExpenseVoucherStatus.Posted;
                voucher.ApprovedByUserId = approvedByUserId;
                voucher.ApprovedOn = DateTime.Now;
                voucher.UpdatedOn = DateTime.Now;
                _context.TableExpenseVouchers.Update(voucher);
            }
        }

        public async Task RejectAsync(int id)
        {
            var voucher = await _context.TableExpenseVouchers.FindAsync(id);
            if (voucher != null)
            {
                voucher.Status = FinancialConstants.ExpenseVoucherStatus.Cancelled;
                voucher.UpdatedOn = DateTime.Now;
                _context.TableExpenseVouchers.Update(voucher);
            }
        }

        public async Task SoftDeleteAsync(int id)
        {
            var voucher = await _context.TableExpenseVouchers.FindAsync(id);
            if (voucher != null)
            {
                voucher.IsActive = false;
                voucher.UpdatedOn = DateTime.Now;
                _context.TableExpenseVouchers.Update(voucher);
            }
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}