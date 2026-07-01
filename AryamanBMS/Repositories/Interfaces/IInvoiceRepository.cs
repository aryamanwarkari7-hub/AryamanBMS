using AryamanBMS.Models;


namespace AryamanBMS.Repositories.Interfaces
{
    public interface IInvoiceRepository
    {
        IQueryable<InvoiceModel> Invoices { get; }

        Task<List<InvoiceModel>> GetAllAsync();

        Task<InvoiceModel?> GetByIdAsync(int id);

        Task<List<ClientModel>> GetClientsAsync();

        Task AddAsync(InvoiceModel invoice);

        Task UpdateAsync(InvoiceModel invoice);

        Task DeleteAsync(InvoiceModel invoice);

        Task<string> GenerateInvoiceNoAsync();

        Task SaveAsync();
    }
}