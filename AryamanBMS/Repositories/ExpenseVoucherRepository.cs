using AryamanBMS.Data;
using System.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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
            return await _context.ExpenseVouchers
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Include(x => x.Category)
                .OrderByDescending(x => x.VoucherDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<ExpenseVoucherModel>> GetByStatusAsync(string status)
        {
            return await _context.ExpenseVouchers
                .AsNoTracking()
                .Where(x => x.Status == status && x.IsActive)
                .Include(x => x.Category)
                .OrderByDescending(x => x.VoucherDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<ExpenseVoucherModel>> GetByFinancialYearAsync(string financialYear)
        {
            return await _context.ExpenseVouchers
                .AsNoTracking()
                .Where(x => x.FinancialYear == financialYear && x.IsActive)
                .Include(x => x.Category)
                .OrderByDescending(x => x.VoucherDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<ExpenseVoucherModel>> GetByCategoryAsync(int categoryId)
        {
            return await _context.ExpenseVouchers
                .AsNoTracking()
                .Where(x => x.ExpenseCategoryId == categoryId && x.IsActive)
                .Include(x => x.Category)
                .OrderByDescending(x => x.VoucherDate)
                .ToListAsync();
        }

        public async Task<ExpenseVoucherModel?> GetByIdAsync(int id)
        {
            return await _context.ExpenseVouchers
                .Include(x => x.Category)
                .Include(x => x.Documents.Where(d => d.IsActive))
                .FirstOrDefaultAsync(x => x.ExpenseVoucherId == id && x.IsActive);
        }

        public async Task<ExpenseVoucherModel?> GetByVoucherNumberAsync(string voucherNumber)
        {
            return await _context.ExpenseVouchers
                .AsNoTracking()
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.VoucherNumber == voucherNumber && x.IsActive);
        }

        public async Task<bool> VoucherNumberExistsAsync(string voucherNumber, int? excludeId = null)
        {
            var query = _context.ExpenseVouchers
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

            return sequence.LastNumber;
        }
        public async Task AddAsync(ExpenseVoucherModel model)
        {
            model.CreatedOn = DateTime.Now;
            model.IsActive = true;
            await _context.ExpenseVouchers.AddAsync(model);
        }

        public Task UpdateAsync(ExpenseVoucherModel model)
        {
            model.UpdatedOn = DateTime.Now;

            return Task.CompletedTask;
        }

        public async Task<bool> ApproveAsync(
    int id,
    string approvedByUserId)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var voucher =
                    await _context.ExpenseVouchers
                        .FirstOrDefaultAsync(x =>
                            x.ExpenseVoucherId == id &&
                            x.IsActive);

                if (voucher == null)
                    return false;

                if (voucher.Status !=
                    FinancialConstants.ExpenseVoucherStatus.Draft)
                {
                    return false;
                }

                voucher.Status =
                    FinancialConstants.ExpenseVoucherStatus.Posted;

                voucher.ApprovedByUserId =
                    approvedByUserId;

                voucher.ApprovedOn =
                    DateTime.Now;

                voucher.RejectionReason = null;
                voucher.RejectedByUserId = null;
                voucher.RejectedOn = null;
                voucher.UpdatedOn = DateTime.Now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> RejectAsync(
    int id,
    string rejectedByUserId,
    string rejectionReason)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var voucher =
                    await _context.ExpenseVouchers
                        .FirstOrDefaultAsync(x =>
                            x.ExpenseVoucherId == id &&
                            x.IsActive);

                if (voucher == null)
                    return false;

                if (voucher.Status !=
                    FinancialConstants.ExpenseVoucherStatus.Draft)
                {
                    return false;
                }

                voucher.Status =
                    FinancialConstants.ExpenseVoucherStatus.Rejected;

                voucher.RejectionReason =
                    rejectionReason.Trim();

                voucher.RejectedByUserId =
                    rejectedByUserId;

                voucher.RejectedOn =
                    DateTime.Now;

                voucher.ApprovedByUserId = null;
                voucher.ApprovedOn = null;
                voucher.UpdatedOn = DateTime.Now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task SoftDeleteAsync(int id)
        {
            var voucher = await _context.ExpenseVouchers.FindAsync(id);
            if (voucher != null)
            {
                voucher.IsActive = false;
                voucher.UpdatedOn = DateTime.Now;
            }
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
        public async Task CreateWithSequenceAsync( ExpenseVoucherModel model)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                DateTime now = DateTime.Now;

                string documentType =
                    FinancialConstants.ExpenseVoucherPrefix;

                var sequence = await _context.FinancialSequences
                    .FirstOrDefaultAsync(x =>
                        x.DocumentType == documentType &&
                        x.FinancialYear == model.FinancialYear);

                if (sequence == null)
                {
                    sequence = new FinancialSequenceModel
                    {
                        DocumentType = documentType,
                        FinancialYear = model.FinancialYear,
                        LastNumber = 1,
                        UpdatedOn = now
                    };

                    await _context.FinancialSequences.AddAsync(
                        sequence);
                }
                else
                {
                    sequence.LastNumber++;
                    sequence.UpdatedOn = now;
                }

                model.VoucherNumber =
                    $"{documentType}-{model.FinancialYear}-" +
                    $"{sequence.LastNumber:0000}";

                model.CreatedOn = now;
                model.UpdatedOn = null;
                model.IsActive = true;
                model.Status =
                    FinancialConstants.ExpenseVoucherStatus.Draft;

                await _context.ExpenseVouchers.AddAsync(model);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> VendorInvoiceExistsAsync(
            string? vendorName,
            string invoiceNumber,
            int? excludeId = null)
        {
            string normalizedVendor =
                NormalizeLookupValue(vendorName);

            string normalizedInvoice =
                NormalizeLookupValue(invoiceNumber);

            if (string.IsNullOrWhiteSpace(
                    normalizedInvoice))
            {
                return false;
            }

            var vouchers =
                await _context.ExpenseVouchers
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive &&
                        x.InvoiceNumber != null)
                    .Select(x => new
                    {
                        x.ExpenseVoucherId,
                        x.VendorName,
                        x.InvoiceNumber
                    })
                    .ToListAsync();

            return vouchers.Any(x =>
                (!excludeId.HasValue ||
                 x.ExpenseVoucherId != excludeId.Value) &&

                NormalizeLookupValue(x.VendorName) ==
                    normalizedVendor &&

                NormalizeLookupValue(x.InvoiceNumber) ==
                    normalizedInvoice);
        }

        public async Task<ExpenseVoucherDocumentModel?> GetDocumentByIdAsync(int id)
        {
            return await _context.ExpenseVoucherDocuments
                .Include(x => x.ExpenseVoucher)
                .FirstOrDefaultAsync(x =>
                    x.ExpenseVoucherDocumentId == id &&
                    x.IsActive);
        }

        public async Task AddDocumentAsync(ExpenseVoucherDocumentModel document)
        {
            document.UploadedOn = DateTime.Now;
            document.IsActive = true;

            await _context.ExpenseVoucherDocuments.AddAsync(document);
        }

        public Task DeleteDocumentAsync(ExpenseVoucherDocumentModel document)
        {
            document.IsActive = false;

            return Task.CompletedTask;
        }

        private static string NormalizeLookupValue(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return string.Concat(
                value
                    .Trim()
                    .ToUpperInvariant()
                    .Where(x => !char.IsWhiteSpace(x)));
        }
    }
}
