using AryamanBMS.Data;
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
            try
            {
                model.CreatedOn = DateTime.Now;
                model.IsActive = true;

                await _context.PaymentReceipts.AddAsync(model);

                await _context.SaveChangesAsync();

                await UpdateInvoicePaymentAsync(model.InvoiceId);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.InnerException?.Message ?? ex.Message);
            }
        }

        public async Task UpdateAsync(PaymentReceiptModel model)
        {
            model.UpdatedOn = DateTime.Now;

            _context.PaymentReceipts.Update(model);

            await SaveAsync();

            await UpdateInvoicePaymentAsync(model.InvoiceId);
        }

        public async Task DeleteAsync(PaymentReceiptModel model)
        {
            int invoiceId = model.InvoiceId;

            _context.PaymentReceipts.Remove(model);

            await SaveAsync();

            await UpdateInvoicePaymentAsync(invoiceId);
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<List<ClientModel>> GetClientsAsync()
        {
            return await _context.Clients
                .Where(x => x.IsActive)
                .OrderBy(x => x.ClientName)
                .ToListAsync();
        }

        public async Task<List<InvoiceModel>> GetInvoicesAsync()
        {
            return await _context.Invoices
                .Include(x => x.Client)
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.InvoiceDate)
                .ToListAsync();
        }
        public async Task<List<InvoiceModel>> GetInvoicesByClientAsync(int clientId)
        {
            return await _context.Invoices
                .Where(x => x.ClientId == clientId &&
                            !x.IsDeleted &&
                            x.BalanceAmount > 0)
                .OrderByDescending(x => x.InvoiceDate)
                .ToListAsync();
        }

        public async Task<string> GenerateReceiptNoAsync()
        {
            var lastReceipt = await _context.PaymentReceipts
                .OrderByDescending(x => x.PaymentReceiptId)
                .FirstOrDefaultAsync();

            if (lastReceipt == null)
                return "RCPT-000001";

            int number = 1;

            if (!string.IsNullOrWhiteSpace(lastReceipt.ReceiptNo))
            {
                var parts = lastReceipt.ReceiptNo.Split('-');

                if (parts.Length > 1)
                    int.TryParse(parts[1], out number);

                number++;
            }

            return $"RCPT-{number:000000}";
        }

        public async Task UpdateInvoicePaymentAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(x => x.InvoiceId == invoiceId);

            if (invoice == null)
                return;

            decimal paid = await _context.PaymentReceipts
                .Where(x => x.InvoiceId == invoiceId &&
                            !x.IsCancelled &&
                            x.IsActive)
                .SumAsync(x => (decimal?)x.AmountReceived) ?? 0;

            invoice.PaidAmount = paid;

            invoice.BalanceAmount = invoice.GrandTotal - paid;

            if (invoice.BalanceAmount <= 0)
            {
                invoice.InvoiceStatus = "Paid";
            }
            else if (paid > 0)
            {
                invoice.InvoiceStatus = "Partially Paid";
            }
            else
            {
                invoice.InvoiceStatus = "Pending";
            }

            _context.Invoices.Update(invoice);

            await _context.SaveChangesAsync();
        }
    }
}