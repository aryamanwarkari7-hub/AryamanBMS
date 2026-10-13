using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories.Implementations;

public class BillingMilestoneRepository : IBillingMilestoneRepository
{
    private readonly ApplicationDbContext _context;

    public BillingMilestoneRepository(ApplicationDbContext context) => _context = context;

    public Task<List<BillingMilestoneModel>> GetAllWithDetailsAsync() =>
        _context.BillingMilestones.AsNoTracking()
            .Include(x => x.PurchaseWorkOrder).ThenInclude(x => x!.Client)
            .Include(x => x.Project).ToListAsync();

    public Task<BillingMilestoneModel?> GetByIdAsync(int id) =>
        _context.BillingMilestones.FirstOrDefaultAsync(x => x.BillingMilestoneId == id);

    public Task<List<PurchaseOrderModel>> GetActivePurchaseOrdersAsync() =>
        _context.PurchaseOrders.AsNoTracking().Include(x => x.Client)
            .Where(x => x.IsActive && x.Status != "Cancelled")
            .OrderByDescending(x => x.OrderDate).ThenBy(x => x.OrderNumber).ToListAsync();

    public Task<PurchaseOrderModel?> GetActivePurchaseOrderAsync(int id) =>
        _context.PurchaseOrders.AsNoTracking().FirstOrDefaultAsync(x =>
            x.PurchaseOrderId == id && x.IsActive && x.Status != "Cancelled");

    public async Task<List<BillingMilestoneProjectOption>> GetProjectsByPurchaseOrderAsync(int purchaseOrderId)
    {
        var order = await GetActivePurchaseOrderAsync(purchaseOrderId);
        if (order == null) return [];

        // Intentionally preserves the existing controller relationship rule.
        return await _context.Projects.AsNoTracking().Where(x => x.Id == order.ClientId)
            .OrderBy(x => x.ProjectName)
            .Select(x => new BillingMilestoneProjectOption(x.Id, x.ProjectCode, x.ProjectName))
            .ToListAsync();
    }

    public Task<bool> ProjectMatchesOrderClientAsync(int projectId, int clientId) =>
        _context.Projects.AsNoTracking().AnyAsync(x => x.Id == projectId && x.Id == clientId);

    public Task<decimal> GetOtherActiveMilestoneTotalAsync(int purchaseWorkOrderId, int billingMilestoneId) =>
        _context.BillingMilestones.AsNoTracking().Where(x =>
            x.PurchaseWorkOrderId == purchaseWorkOrderId &&
            x.BillingMilestoneId != billingMilestoneId && x.IsActive)
            .SumAsync(x => x.MilestoneValue);

    public Task<decimal> GetBilledValueAsync(int billingMilestoneId) =>
        _context.Invoices.AsNoTracking().Where(x =>
            x.BillingMilestoneId == billingMilestoneId && !x.IsDeleted && x.InvoiceStatus != "Cancelled")
            .SumAsync(x => x.GrandTotal);

    public Task AddAsync(BillingMilestoneModel milestone) =>
        _context.BillingMilestones.AddAsync(milestone).AsTask();

    public Task SaveAsync() => _context.SaveChangesAsync();
}
