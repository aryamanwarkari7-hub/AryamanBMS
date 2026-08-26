using AryamanBMS.Models;

namespace AryamanBMS.Business.Interfaces;

public interface ICreditNoteQueryService
{
    Task<List<CreditNoteModel>> GetAllAsync(string? search, string sortBy, string sortOrder);
    Task<List<InvoiceModel>> GetIssuedInvoicesAsync();
}
