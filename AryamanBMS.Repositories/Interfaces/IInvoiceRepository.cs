using AryamanBMS.Models;


namespace AryamanBMS.Repositories.Interfaces
{
    public interface IInvoiceRepository
    {
        Task<List<InvoiceModel>> GetForReceivablesAsync();

        Task<List<InvoiceModel>> GetOutstandingForAgeingAsync();

        IQueryable<InvoiceModel> Invoices { get; }

        Task<List<InvoiceModel>> GetAllAsync();

        Task<InvoiceModel?> GetByIdAsync(int id);

        Task<List<ClientModel>> GetClientsAsync();

        Task<ClientModel?> GetClientWithCountryAsync(int clientId);

        Task<bool> IsGstPeriodClosedAsync(DateTime invoiceDate);

        Task<List<ProjectModel>> GetProjectsAsync();

        Task<List<PurchaseOrderModel>> GetActivePurchaseOrdersAsync();

        Task<List<BillingMilestoneModel>> GetActiveBillingMilestonesAsync();

        Task<PurchaseOrderModel?> GetActivePurchaseOrderAsync(int id);
        Task<decimal> GetBilledTaxableAmountForPurchaseOrderAsync(int purchaseOrderId, int excludedInvoiceId);
        Task<BillingMilestoneModel?> GetActiveBillingMilestoneAsync(int billingMilestoneId);
        Task<bool> IsBillingMilestoneInvoicedAsync(int billingMilestoneId, int excludedInvoiceId);

        Task<bool> HasCurrentDocumentAsync(int invoiceId, string documentFormat);
        Task<List<InvoiceDocumentVersionModel>> GetDocumentHistoryAsync(int invoiceId);
        Task<InvoiceDocumentVersionModel?> GetCurrentDocumentAsync(int invoiceId, string documentFormat);
        Task<InvoiceDocumentVersionModel?> GetDocumentVersionAsync(int documentVersionId);

        Task AddAsync(InvoiceModel invoice);

        Task UpdateAsync(InvoiceModel invoice);

        Task DeleteAsync(InvoiceModel invoice);

        Task<string> GenerateInvoiceNoAsync();

        Task SaveAsync();

        Task CreateWithSequenceAsync(InvoiceModel invoice);
        
    }
}
