using AryamanBMS.Models;

namespace AryamanBMS.Business.Interfaces;

public interface IBillingMilestoneService
{
    Task<List<BillingMilestoneModel>> GetAllAsync(int? purchaseWorkOrderId, string? search, string sortBy, string sortOrder);
    Task<BillingMilestoneModel?> GetByIdAsync(int id);
    Task<List<PurchaseOrderModel>> GetActivePurchaseOrdersAsync();
    Task<List<BillingMilestoneProjectOption>> GetProjectsByPurchaseOrderAsync(int purchaseOrderId);
    Task<IReadOnlyDictionary<string, string>> ValidateAsync(BillingMilestoneModel milestone);
    Task CreateAsync(BillingMilestoneModel milestone);
    Task UpdateAsync(BillingMilestoneModel existing, BillingMilestoneModel milestone);
    Task<BillingMilestoneModel?> ToggleActiveAsync(int id);
}
