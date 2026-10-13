using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces;

public interface IBillingMilestoneRepository
{
    Task<List<BillingMilestoneModel>> GetAllWithDetailsAsync();
    Task<BillingMilestoneModel?> GetByIdAsync(int id);
    Task<List<PurchaseOrderModel>> GetActivePurchaseOrdersAsync();
    Task<PurchaseOrderModel?> GetActivePurchaseOrderAsync(int id);
    Task<List<BillingMilestoneProjectOption>> GetProjectsByPurchaseOrderAsync(int purchaseOrderId);
    Task<bool> ProjectMatchesOrderClientAsync(int projectId, int clientId);
    Task<decimal> GetOtherActiveMilestoneTotalAsync(int purchaseWorkOrderId, int billingMilestoneId);
    Task<decimal> GetBilledValueAsync(int billingMilestoneId);
    Task AddAsync(BillingMilestoneModel milestone);
    Task SaveAsync();
}
