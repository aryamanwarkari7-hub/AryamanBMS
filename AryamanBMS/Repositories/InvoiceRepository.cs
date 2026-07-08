using AryamanBMS.Data;
using System.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly ApplicationDbContext _context;


        public InvoiceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<InvoiceModel> Invoices => _context.Invoices;

        public async Task<List<InvoiceModel>> GetAllAsync()
        {
            var invoices = await Invoices
                .Include(i => i.Client)
                .Include(i => i.Project)
                .Include(i => i.InvoiceDetails)
                .Where(i => !i.IsDeleted)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();

            bool statusChanged = false;

            foreach (var invoice in invoices)
            {
                string oldStatus = invoice.PaymentStatus;

                RefreshPaymentStatus(invoice);

                if (oldStatus != invoice.PaymentStatus)
                {
                    statusChanged = true;
                }
            }

            if (statusChanged)
            {
                await _context.SaveChangesAsync();
            }

            return invoices;
        }

        public async Task<InvoiceModel?> GetByIdAsync(int id)
        {
            var invoice = await Invoices
                .Include(i => i.Client)
                .Include(i => i.Project)
                .Include(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(i =>
                    i.InvoiceId == id &&
                    !i.IsDeleted);

            if (invoice == null)
            {
                return null;
            }

            string oldStatus = invoice.PaymentStatus;

            RefreshPaymentStatus(invoice);

            if (oldStatus != invoice.PaymentStatus)
            {
                await _context.SaveChangesAsync();
            }

            return invoice;
        }

        public async Task<List<ClientModel>> GetClientsAsync()
        {
            return await _context.Clients

                .Where(c => c.IsActive)

                .OrderBy(c => c.ClientName)

                .ToListAsync();
        }

        public async Task AddAsync(InvoiceModel invoice)
        {
            invoice.CreatedOn = DateTime.Now;

            invoice.IsDeleted = false;


            if (invoice.PaidAmount < 0)
                invoice.PaidAmount = 0;


            invoice.BalanceAmount =
                invoice.GrandTotal - invoice.PaidAmount;



            await _context.Invoices.AddAsync(invoice);
        }

        public async Task<List<ProjectModel>> GetProjectsAsync()
        {
            return await _context.Projects
                .Where(x => x.IsActive)
                .OrderBy(x => x.ProjectName)
                .ToListAsync();
        }

        public Task UpdateAsync(InvoiceModel invoice)
        {
            invoice.ModifiedOn = DateTime.Now;

            invoice.BalanceAmount =
                Math.Max(
                    0,
                    invoice.GrandTotal - invoice.PaidAmount);

            return Task.CompletedTask;
        }

        public Task DeleteAsync(InvoiceModel invoice)
        {
            invoice.InvoiceStatus = "Cancelled";
            invoice.IsDeleted = false;
            invoice.ModifiedOn = DateTime.Now;

            return Task.CompletedTask;
        }

        public async Task<string> GenerateInvoiceNoAsync()
        {
            DateTime now = DateTime.Now;

            string documentType = "Invoice";
            string sequencePeriod = now.ToString("yyyyMM");

            int lastNumber = await _context.FinancialSequences
                .Where(x =>
                    x.DocumentType == documentType &&
                    x.FinancialYear == sequencePeriod)
                .Select(x => x.LastNumber)
                .FirstOrDefaultAsync();

            return $"INV-{now:yyMM}-{lastNumber + 1:0000}";
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task CreateWithSequenceAsync(InvoiceModel invoice)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                DateTime now = DateTime.Now;

                string documentType = "Invoice";
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

                invoice.InvoiceNo =
                    $"INV-{now:yyMM}-{sequence.LastNumber:0000}";

                invoice.CreatedOn = now;
                invoice.ModifiedOn = null;
                invoice.IsDeleted = false;
                invoice.PaidAmount = 0;
                invoice.BalanceAmount = invoice.GrandTotal;

                await _context.Invoices.AddAsync(invoice);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static void RefreshPaymentStatus( InvoiceModel invoice)
        {
            if (invoice.InvoiceStatus == "Cancelled")
            {
                return;
            }

            if (invoice.BalanceAmount <= 0)
            {
                invoice.PaymentStatus = "Paid";
            }
            else if (invoice.DueDate.HasValue &&
                     invoice.DueDate.Value.Date < DateTime.Today)
            {
                invoice.PaymentStatus = "Overdue";
            }
            else if (invoice.PaidAmount > 0)
            {
                invoice.PaymentStatus = "Partially Paid";
            }
            else
            {
                invoice.PaymentStatus = "Unpaid";
            }
        }
    }
}