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

        public async Task<List<InvoiceModel>> GetForReceivablesAsync()
        {
            return await _context.Invoices
                .AsNoTracking()
                .Include(x => x.Client)
                .Include(x => x.Project)
                .Where(x =>
                    !x.IsDeleted &&
                    x.InvoiceStatus != "Cancelled" &&
                    x.InvoiceStatus != "Draft")
                .ToListAsync();
        }

        public async Task<List<InvoiceModel>> GetOutstandingForAgeingAsync()
        {
            return await _context.Invoices
                .AsNoTracking()
                .Include(x => x.Client)
                .Where(x =>
                    !x.IsDeleted &&
                    x.InvoiceStatus != "Cancelled" &&
                    x.InvoiceStatus != "Draft" &&
                    x.BalanceAmount > 0)
                .ToListAsync();
        }

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

        public Task<ClientModel?> GetClientWithCountryAsync(int clientId)
        {
            return _context.Clients
                .AsNoTracking()
                .Include(x => x.Country)
                .FirstOrDefaultAsync(x => x.ClientId == clientId);
        }

        public Task<bool> IsGstPeriodClosedAsync(DateTime invoiceDate)
        {
            return _context.GstMonthlySnapshots.AnyAsync(x =>
                x.Month == invoiceDate.Month &&
                x.Year == invoiceDate.Year &&
                (x.Status == FinancialConstants.GstSnapshotStatus.Filed ||
                 x.Status == FinancialConstants.GstSnapshotStatus.Locked ||
                 x.IsFiledPeriodLocked));
        }

        public Task<List<PurchaseOrderModel>> GetActivePurchaseOrdersAsync()
        {
            return _context.PurchaseOrders
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.OrderDate)
                .ToListAsync();
        }

        public Task<List<BillingMilestoneModel>> GetActiveBillingMilestonesAsync()
        {
            return _context.BillingMilestones
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.PurchaseWorkOrderId)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.MilestoneName)
                .ToListAsync();
        }

        public Task<PurchaseOrderModel?> GetActivePurchaseOrderAsync(int id)
        {
            return _context.PurchaseOrders
                .AsNoTracking()
                .Include(x => x.Client)
                .Include(x => x.Proposal)
                .FirstOrDefaultAsync(x => x.PurchaseOrderId == id && x.IsActive);
        }
        public Task<decimal> GetBilledTaxableAmountForPurchaseOrderAsync(int purchaseOrderId, int excludedInvoiceId) => _context.Invoices.AsNoTracking().Where(x => x.PurchaseWorkOrderId == purchaseOrderId && x.InvoiceId != excludedInvoiceId && !x.IsDeleted && x.InvoiceStatus != "Cancelled").SumAsync(x => x.SubTotal - x.Discount);
        public Task<BillingMilestoneModel?> GetActiveBillingMilestoneAsync(int billingMilestoneId) => _context.BillingMilestones.AsNoTracking().FirstOrDefaultAsync(x => x.BillingMilestoneId == billingMilestoneId && x.IsActive);
        public Task<bool> IsBillingMilestoneInvoicedAsync(int billingMilestoneId, int excludedInvoiceId) => _context.Invoices.AsNoTracking().AnyAsync(x => x.BillingMilestoneId == billingMilestoneId && x.InvoiceId != excludedInvoiceId && !x.IsDeleted && x.InvoiceStatus != "Cancelled");

        public Task<bool> HasCurrentDocumentAsync(int invoiceId, string documentFormat)
        {
            return _context.InvoiceDocumentVersions
                .AsNoTracking()
                .AnyAsync(x =>
                    x.InvoiceId == invoiceId &&
                    x.DocumentFormat == documentFormat &&
                    x.IsCurrent);
        }
        public Task<List<InvoiceDocumentVersionModel>> GetDocumentHistoryAsync(int invoiceId) => _context.InvoiceDocumentVersions.AsNoTracking().Where(x => x.InvoiceId == invoiceId).OrderByDescending(x => x.VersionNumber).ThenBy(x => x.DocumentFormat).ToListAsync();
        public Task<InvoiceDocumentVersionModel?> GetCurrentDocumentAsync(int invoiceId, string documentFormat) => _context.InvoiceDocumentVersions.AsNoTracking().Where(x => x.InvoiceId == invoiceId && x.DocumentFormat == documentFormat && x.IsCurrent).OrderByDescending(x => x.VersionNumber).FirstOrDefaultAsync();
        public Task<InvoiceDocumentVersionModel?> GetDocumentVersionAsync(int documentVersionId) => _context.InvoiceDocumentVersions.AsNoTracking().FirstOrDefaultAsync(x => x.InvoiceDocumentVersionId == documentVersionId);

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
