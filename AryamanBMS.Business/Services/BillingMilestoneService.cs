using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Business.Services;

public class BillingMilestoneService : IBillingMilestoneService
{
    private readonly IBillingMilestoneRepository _repository;

    public BillingMilestoneService(IBillingMilestoneRepository repository) => _repository = repository;

    public async Task<List<BillingMilestoneModel>> GetAllAsync(int? purchaseWorkOrderId, string? search, string sortBy, string sortOrder)
    {
        var milestones = await _repository.GetAllWithDetailsAsync();
        if (purchaseWorkOrderId.HasValue)
            milestones = milestones.Where(x => x.PurchaseWorkOrderId == purchaseWorkOrderId.Value).ToList();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            milestones = milestones.Where(x => Contains(x.MilestoneName, keyword) || Contains(x.MilestoneDescription, keyword) ||
                Contains(x.CompletionStatus, keyword) || Contains(x.PurchaseWorkOrder?.OrderNumber, keyword) ||
                Contains(x.PurchaseWorkOrder?.Client?.ClientName, keyword) || Contains(x.Project?.ProjectName, keyword)).ToList();
        }
        var descending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        return sortBy switch
        {
            "Milestone" => descending ? milestones.OrderByDescending(x => x.MilestoneName).ToList() : milestones.OrderBy(x => x.MilestoneName).ToList(),
            "Client" => descending ? milestones.OrderByDescending(x => x.PurchaseWorkOrder?.Client?.ClientName).ToList() : milestones.OrderBy(x => x.PurchaseWorkOrder?.Client?.ClientName).ToList(),
            "Project" => descending ? milestones.OrderByDescending(x => x.Project?.ProjectName).ToList() : milestones.OrderBy(x => x.Project?.ProjectName).ToList(),
            "Value" => descending ? milestones.OrderByDescending(x => x.MilestoneValue).ToList() : milestones.OrderBy(x => x.MilestoneValue).ToList(),
            "Billed" => descending ? milestones.OrderByDescending(x => x.BilledValue).ToList() : milestones.OrderBy(x => x.BilledValue).ToList(),
            "Remaining" => descending ? milestones.OrderByDescending(x => x.RemainingBillableValue).ToList() : milestones.OrderBy(x => x.RemainingBillableValue).ToList(),
            "Status" => descending ? milestones.OrderByDescending(x => x.CompletionStatus).ToList() : milestones.OrderBy(x => x.CompletionStatus).ToList(),
            "Active" => descending ? milestones.OrderByDescending(x => x.IsActive).ToList() : milestones.OrderBy(x => x.IsActive).ToList(),
            _ => descending
                ? milestones.OrderByDescending(x => x.PurchaseWorkOrder?.OrderNumber).ThenByDescending(x => x.SortOrder).ThenByDescending(x => x.MilestoneName).ToList()
                : milestones.OrderByDescending(x => x.IsActive).ThenBy(x => x.PurchaseWorkOrder?.OrderNumber).ThenBy(x => x.SortOrder).ThenBy(x => x.MilestoneName).ToList()
        };
    }

    public Task<BillingMilestoneModel?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);
    public Task<List<PurchaseOrderModel>> GetActivePurchaseOrdersAsync() => _repository.GetActivePurchaseOrdersAsync();
    public Task<List<BillingMilestoneProjectOption>> GetProjectsByPurchaseOrderAsync(int purchaseOrderId) => _repository.GetProjectsByPurchaseOrderAsync(purchaseOrderId);

    public async Task<IReadOnlyDictionary<string, string>> ValidateAsync(BillingMilestoneModel milestone)
    {
        Normalize(milestone);
        var errors = new Dictionary<string, string>();
        if (milestone.PurchaseWorkOrderId <= 0)
        {
            errors[nameof(milestone.PurchaseWorkOrderId)] = "Purchase / Work Order is required.";
            return errors;
        }
        var order = await _repository.GetActivePurchaseOrderAsync(milestone.PurchaseWorkOrderId);
        if (order == null)
        {
            errors[nameof(milestone.PurchaseWorkOrderId)] = "Selected Purchase / Work Order is invalid.";
            return errors;
        }
        if (!order.OrderAmount.HasValue || order.OrderAmount.Value <= 0)
            errors[nameof(milestone.PurchaseWorkOrderId)] = "Purchase / Work Order amount is required before milestones.";
        if (milestone.ProjectId.HasValue && !await _repository.ProjectMatchesOrderClientAsync(milestone.ProjectId.Value, order.ClientId))
            errors[nameof(milestone.ProjectId)] = "Selected project does not belong to the PO / WO client.";
        if (string.IsNullOrWhiteSpace(milestone.MilestoneName))
            errors[nameof(milestone.MilestoneName)] = "Milestone name is required.";
        if (milestone.MilestoneValue <= 0)
            errors[nameof(milestone.MilestoneValue)] = "Milestone value must be greater than zero.";
        if (milestone.CompletionStatus is not ("Pending" or "Approved" or "Completed" or "On Hold"))
            errors[nameof(milestone.CompletionStatus)] = "Invalid milestone status.";
        var otherTotal = await _repository.GetOtherActiveMilestoneTotalAsync(milestone.PurchaseWorkOrderId, milestone.BillingMilestoneId);
        if (order.OrderAmount.HasValue && otherTotal + milestone.MilestoneValue > order.OrderAmount.Value)
        {
            var available = order.OrderAmount.Value - otherTotal;
            errors[nameof(milestone.MilestoneValue)] = $"Milestone exceeds remaining PO / WO value. Available amount is {available:N2}.";
        }
        return errors;
    }

    public async Task CreateAsync(BillingMilestoneModel milestone)
    {
        milestone.CreatedOn = DateTime.Now;
        milestone.IsActive = true;
        UpdateComputedValues(milestone);
        await _repository.AddAsync(milestone);
        await _repository.SaveAsync();
    }

    public async Task UpdateAsync(BillingMilestoneModel existing, BillingMilestoneModel milestone)
    {
        existing.PurchaseWorkOrderId = milestone.PurchaseWorkOrderId;
        existing.ProjectId = milestone.ProjectId;
        existing.MilestoneName = milestone.MilestoneName;
        existing.MilestoneDescription = milestone.MilestoneDescription;
        existing.MilestoneValue = milestone.MilestoneValue;
        existing.CompletionStatus = milestone.CompletionStatus;
        existing.ApprovalDate = milestone.ApprovalDate;
        existing.SortOrder = milestone.SortOrder;
        existing.IsActive = milestone.IsActive;
        existing.UpdatedOn = DateTime.Now;
        existing.BilledValue = Math.Round(await _repository.GetBilledValueAsync(existing.BillingMilestoneId), 2);
        UpdateComputedValues(existing);
        await _repository.SaveAsync();
    }

    public async Task<BillingMilestoneModel?> ToggleActiveAsync(int id)
    {
        var milestone = await _repository.GetByIdAsync(id);
        if (milestone == null) return null;
        milestone.IsActive = !milestone.IsActive;
        milestone.UpdatedOn = DateTime.Now;
        await _repository.SaveAsync();
        return milestone;
    }

    private static bool Contains(string? value, string keyword) => !string.IsNullOrWhiteSpace(value) && value.ToLower().Contains(keyword);
    private static void Normalize(BillingMilestoneModel model)
    {
        model.MilestoneName = model.MilestoneName?.Trim() ?? string.Empty;
        model.MilestoneDescription = string.IsNullOrWhiteSpace(model.MilestoneDescription) ? null : model.MilestoneDescription.Trim();
        model.CompletionStatus = string.IsNullOrWhiteSpace(model.CompletionStatus) ? "Pending" : model.CompletionStatus.Trim();
        model.MilestoneValue = Math.Round(model.MilestoneValue, 2);
        model.BilledValue = Math.Round(model.BilledValue, 2);
    }
    private static void UpdateComputedValues(BillingMilestoneModel model) => model.RemainingBillableValue = Math.Max(0, Math.Round(model.MilestoneValue - model.BilledValue, 2));
}
