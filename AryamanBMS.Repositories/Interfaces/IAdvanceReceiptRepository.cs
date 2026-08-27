using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces;

public interface IAdvanceReceiptRepository
{
    Task<List<AdvanceReceiptModel>> GetAllAsync();
    Task<bool> PaymentReferenceExistsAsync(string paymentReference);
    Task CreateWithSequenceAsync(AdvanceReceiptModel receipt);
    Task<AdvanceReceiptModel?> GetAvailableByIdAsync(int id);
    Task<InvoiceModel?> GetIssuedInvoiceForClientAsync(int invoiceId, int clientId);
    Task<List<InvoiceModel>> GetOutstandingInvoicesForClientAsync(int clientId);
    Task SaveAdjustmentAsync();
    Task<List<ClientModel>> GetActiveClientsAsync();
    Task<List<ProjectModel>> GetActiveProjectsAsync();
}
