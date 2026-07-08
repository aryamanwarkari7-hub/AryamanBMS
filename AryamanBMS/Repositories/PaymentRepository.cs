using AryamanBMS.Data;
using System.Data;
using AryamanBMS.Models;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class PaymentRepository : IPaymentReceiptRepository
    {
        private readonly ApplicationDbContext _context;

        public PaymentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PaymentReceiptModel>> GetAllAsync()
        {
            return await _context.PaymentReceipts
                .Include(x => x.Client)
                .Include(x => x.Invoice)
                .OrderByDescending(x => x.ReceiptDate)
                .ToListAsync();
        }

        public async Task<PaymentReceiptModel?> GetByIdAsync(int id)
        {
            return await _context.PaymentReceipts
                .Include(x => x.Client)
                .Include(x => x.Invoice)
                .FirstOrDefaultAsync(x => x.PaymentReceiptId == id);
        }
        public async Task AddAsync(PaymentReceiptModel model)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                DateTime now = DateTime.Now;

                var invoice = await _context.Invoices
    .FirstOrDefaultAsync(x =>
        x.InvoiceId == model.InvoiceId);

                if (invoice == null ||
                    invoice.IsDeleted ||
                    invoice.InvoiceStatus != "Issued")
                {
                    throw new InvalidOperationException(
                        "Payments can only be recorded against a valid issued invoice.");
                }

                if (invoice.ClientId != model.ClientId)
                {
                    throw new InvalidOperationException(
                        "The selected invoice does not belong to this client.");
                }

                decimal alreadyReceived =
                    await _context.PaymentReceipts
                        .Where(x =>
                            x.InvoiceId == model.InvoiceId &&
                            x.IsActive &&
                            !x.IsCancelled)
                        .SumAsync(x =>
                            (decimal?)x.AmountReceived) ?? 0;

                decimal availableBalance =
                    Math.Max(
                        0,
                        invoice.GrandTotal - alreadyReceived);

                if (model.AmountReceived <= 0)
                {
                    throw new InvalidOperationException(
                        "Payment amount must be greater than zero.");
                }

                if (model.AmountReceived > availableBalance)
                {
                    throw new InvalidOperationException(
                        $"Payment amount cannot exceed the available balance of ₹{availableBalance:N2}.");
                }

                const string documentType = "PaymentReceipt";
                string sequencePeriod = now.ToString("yyyyMM");

                var sequence = await _context.FinancialSequences
                    .FirstOrDefaultAsync(x =>
                        x.DocumentType == documentType &&
                        x.FinancialYear == sequencePeriod);

                if (sequence == null)
                {
                    sequence = new FinancialSequenceModel
                    {
                        DocumentType = documentType,
                        FinancialYear = sequencePeriod,
                        LastNumber = 1,
                        UpdatedOn = now
                    };

                    await _context.FinancialSequences.AddAsync(sequence);
                }
                else
                {
                    sequence.LastNumber++;
                    sequence.UpdatedOn = now;
                }

                model.ReceiptNo =
                    $"REC-{now:yyMM}-{sequence.LastNumber:0000}";

                model.CreatedOn = now;
                model.UpdatedOn = null;
                model.IsActive = true;
                model.IsCancelled = false;

                await _context.PaymentReceipts.AddAsync(model);

                await _context.SaveChangesAsync();

                await RecalculateInvoicePaymentAsync(model.InvoiceId);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateAsync(
    PaymentReceiptModel model)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var invoice = await _context.Invoices
                    .FirstOrDefaultAsync(x =>
                        x.InvoiceId == model.InvoiceId);

                if (invoice == null ||
                    invoice.IsDeleted ||
                    invoice.InvoiceStatus != "Issued")
                {
                    throw new InvalidOperationException(
                        "Payments can only be recorded against a valid issued invoice.");
                }

                decimal otherReceiptsTotal =
                    await _context.PaymentReceipts
                        .Where(x =>
                            x.InvoiceId == model.InvoiceId &&
                            x.PaymentReceiptId != model.PaymentReceiptId &&
                            x.IsActive &&
                            !x.IsCancelled)
                        .SumAsync(x =>
                            (decimal?)x.AmountReceived) ?? 0;

                decimal availableBalance =
                    Math.Max(
                        0,
                        invoice.GrandTotal -
                        otherReceiptsTotal);

                if (model.AmountReceived <= 0)
                {
                    throw new InvalidOperationException(
                        "Payment amount must be greater than zero.");
                }

                if (model.AmountReceived > availableBalance)
                {
                    throw new InvalidOperationException(
                        $"Payment amount cannot exceed the available balance of ₹{availableBalance:N2}.");
                }

                model.UpdatedOn = DateTime.Now;

                await _context.SaveChangesAsync();

                await RecalculateInvoicePaymentAsync(
                    model.InvoiceId);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> CancelAsync(
            int paymentReceiptId,
            string cancelledByUserId,
            string cancellationReason)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var payment =
                    await _context.PaymentReceipts
                        .FirstOrDefaultAsync(x =>
                            x.PaymentReceiptId ==
                            paymentReceiptId);

                if (payment == null ||
                    payment.IsCancelled)
                {
                    return false;
                }

                payment.IsCancelled = true;
                payment.IsActive = false;
                payment.CancellationReason =
                    cancellationReason.Trim();
                payment.CancelledByUserId =
                    cancelledByUserId;
                payment.CancelledOn =
                    DateTime.Now;
                payment.UpdatedOn =
                    DateTime.Now;

                await _context.SaveChangesAsync();

                await RecalculateInvoicePaymentAsync(
                    payment.InvoiceId);

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

        public async Task<List<ClientModel>> GetClientsAsync()
        {
            return await _context.Clients
                .Where(x => x.IsActive)
                .OrderBy(x => x.ClientName)
                .ToListAsync();
        }

        public async Task<List<InvoiceModel>>GetInvoicesAsync()
        {
            return await _context.Invoices
                .Include(x => x.Client)
                .Where(x =>
                    !x.IsDeleted &&
                    x.InvoiceStatus == "Issued" &&
                    x.BalanceAmount > 0)
                .OrderByDescending(x =>
                    x.InvoiceDate)
                .ToListAsync();
        }

        public async Task<List<InvoiceModel>> GetInvoicesByClientAsync(int clientId)
        {
            return await _context.Invoices
                .Where(x =>
                    x.ClientId == clientId &&
                    !x.IsDeleted &&
                    x.InvoiceStatus == "Issued")
                .OrderByDescending(x => x.InvoiceDate)
                .ToListAsync();
        }

        public async Task<string> GenerateReceiptNoAsync()
        {
            DateTime now = DateTime.Now;

            const string documentType = "PaymentReceipt";
            string sequencePeriod = now.ToString("yyyyMM");

            int lastNumber = await _context.FinancialSequences
                .Where(x =>
                    x.DocumentType == documentType &&
                    x.FinancialYear == sequencePeriod)
                .Select(x => x.LastNumber)
                .FirstOrDefaultAsync();

            return $"REC-{now:yyMM}-{lastNumber + 1:0000}";
        }
        private async Task RecalculateInvoicePaymentAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(x =>
                    x.InvoiceId == invoiceId &&
                    !x.IsDeleted);

            if (invoice == null)
                return;

            decimal paidAmount =
                await _context.PaymentReceipts
                    .Where(x =>
                        x.InvoiceId == invoiceId &&
                        x.IsActive &&
                        !x.IsCancelled)
                    .SumAsync(x =>
                        (decimal?)x.AmountReceived) ?? 0;

            invoice.PaidAmount = paidAmount;

            invoice.BalanceAmount =
                Math.Max(
                    0,
                    invoice.GrandTotal - paidAmount);

            if (invoice.BalanceAmount <= 0)
            {
                invoice.PaymentStatus = "Paid";
            }
            else if (invoice.DueDate.HasValue &&
             invoice.DueDate.Value.Date < DateTime.Today)
            {
                invoice.PaymentStatus = "Overdue";
            }
            else if (paidAmount > 0)
            {
                invoice.PaymentStatus = "Partially Paid";
            }
            else
            {
                invoice.PaymentStatus = "Unpaid";
            }

            invoice.ModifiedOn = DateTime.Now;
        }

        public async Task<bool> TransactionReferenceExistsAsync(
             string? transactionNo,
             string? referenceNo,
             int? excludePaymentReceiptId = null)
        {
            string normalizedTransaction =
                (transactionNo ?? string.Empty).Trim().ToLower();

            string normalizedReference =
                (referenceNo ?? string.Empty).Trim().ToLower();

            if (string.IsNullOrWhiteSpace(normalizedTransaction) &&
                string.IsNullOrWhiteSpace(normalizedReference))
            {
                return false;
            }

            return await _context.PaymentReceipts.AnyAsync(x =>
                !x.IsCancelled &&
                x.PaymentReceiptId != excludePaymentReceiptId &&
                (
                    (!string.IsNullOrWhiteSpace(normalizedTransaction) &&
                     x.TransactionNo != null &&
                     x.TransactionNo.ToLower() == normalizedTransaction)
                    ||
                    (!string.IsNullOrWhiteSpace(normalizedReference) &&
                     x.ReferenceNo != null &&
                     x.ReferenceNo.ToLower() == normalizedReference)
                ));
        }
    }
}