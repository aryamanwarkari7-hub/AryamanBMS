using AryamanBMS.Data;
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
            _context.PurchaseOrders.Update(order);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(PurchaseOrderModel order)
        {
            _context.PurchaseOrders.Remove(order);
            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
