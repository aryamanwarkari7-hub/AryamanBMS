using AryamanBMS.Data;
using System.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class PurchaseOrderRepository : IPurchaseOrderRepository
    {
        private readonly ApplicationDbContext _context;

        public PurchaseOrderRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        private IQueryable<PurchaseOrderModel> Orders =>
            _context.PurchaseOrders
                .Include(o => o.Client)
                .Include(o => o.Proposal);

        public async Task<List<PurchaseOrderModel>> GetAllAsync()
        {
            return await Orders
                .OrderByDescending(o => o.OrderDate)
                .ThenBy(o => o.OrderNumber)
                .ToListAsync();
        }

        public async Task<PurchaseOrderModel?> GetByIdAsync(int id)
        {
            return await Orders
                .FirstOrDefaultAsync(o => o.PurchaseOrderId == id);
        }

        public async Task<List<PurchaseOrderModel>> GetByClientAsync(int clientId)
        {
            return await Orders
                .Where(o => o.ClientId == clientId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<PurchaseOrderModel>> GetByTypeAsync(string orderType)
        {
            return await Orders
                .Where(o => o.OrderType == orderType)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<PurchaseOrderModel>> GetByProposalAsync(int proposalId)
        {
            return await Orders
                .Where(o => o.ProposalId == proposalId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task AddAsync(PurchaseOrderModel order)
        {
            await _context.PurchaseOrders.AddAsync(order);
        }

        public Task UpdateAsync(PurchaseOrderModel order)
        {
            order.UpdatedOn = DateTime.Now;

            return Task.CompletedTask;
        }

        public Task DeleteAsync(PurchaseOrderModel order)
        {
            order.IsActive = false;
            order.UpdatedOn = DateTime.Now;

            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task CreateWithSequenceAsync(PurchaseOrderModel order)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                DateTime now = DateTime.Now;

                const string documentType = "PurchaseOrder";
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

                order.OrderNumber =
                    $"PO-{now:yyMM}-{sequence.LastNumber:0000}";

                order.CreatedOn = now;
                order.UpdatedOn = null;
                order.IsActive = true;

                await _context.PurchaseOrders.AddAsync(order);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

            public async Task<string> GenerateOrderNumberAsync()
        {
            DateTime now = DateTime.Now;

            const string documentType = "PurchaseOrder";
            string sequencePeriod = now.ToString("yyyyMM");

            int lastNumber = await _context.FinancialSequences
                .Where(x =>
                    x.DocumentType == documentType &&
                    x.FinancialYear == sequencePeriod)
                .Select(x => x.LastNumber)
                .FirstOrDefaultAsync();

            return $"PO-{now:yyMM}-{lastNumber + 1:0000}";
        }
        public async Task CreateFromProposalWithSequenceAsync(
    PurchaseOrderModel order,
    ProposalModel? proposal)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                DateTime now = DateTime.Now;

                const string documentType = "PurchaseOrder";
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

                order.OrderNumber =
                    $"{order.OrderType}-{now:yyMM}-{sequence.LastNumber:0000}";

                order.CreatedOn = now;
                order.UpdatedOn = null;
                order.IsActive = true;

                await _context.PurchaseOrders.AddAsync(order);

                if (proposal != null)
                {
                    proposal.IsConverted = true;
                    proposal.UpdatedOn = now;
                }

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
    
}
