using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IPurchaseOrderRepository
    {
        Task<List<PurchaseOrderModel>> GetAllAsync();
        Task<PurchaseOrderModel?> GetByIdAsync(int id);
        Task<List<PurchaseOrderModel>> GetByClientAsync(int clientId);
        Task<List<PurchaseOrderModel>> GetByTypeAsync(string orderType);   // "PO" or "WO"
        Task<List<PurchaseOrderModel>> GetByProposalAsync(int proposalId);
        Task AddAsync(PurchaseOrderModel order);
        Task UpdateAsync(PurchaseOrderModel order);
        Task DeleteAsync(PurchaseOrderModel order);
        Task SaveAsync();
        Task<string> GenerateOrderNumberAsync();
        Task CreateWithSequenceAsync(PurchaseOrderModel order);
        Task CreateFromProposalWithSequenceAsync(
    PurchaseOrderModel order,ProposalModel? proposal);
    }
}
