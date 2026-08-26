using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces;

public interface ICreditNoteRepository
{
    Task<List<CreditNoteModel>> GetAllAsync();

    Task<List<InvoiceModel>> GetIssuedInvoicesAsync();
}
