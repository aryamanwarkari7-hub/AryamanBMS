using AryamanBMS.Models;

public interface IPaymentReceiptRepository
{
    Task<List<PaymentReceiptModel>> GetAllAsync();

    Task<PaymentReceiptModel?> GetByIdAsync(int id);

    Task AddAsync(PaymentReceiptModel model);

    Task UpdateAsync(PaymentReceiptModel model);

    Task DeleteAsync(PaymentReceiptModel model);

    Task SaveAsync();

    Task<string> GenerateReceiptNoAsync();

    Task<List<ClientModel>> GetClientsAsync();

    Task<List<InvoiceModel>> GetInvoicesAsync();

    Task<List<InvoiceModel>> GetInvoicesByClientAsync(int clientId);

    Task UpdateInvoicePaymentAsync(int invoiceId);
}