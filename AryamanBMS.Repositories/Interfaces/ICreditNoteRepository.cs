using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces;

public interface ICreditNoteRepository
{
    Task<List<CreditNoteModel>> GetAllAsync();

    Task<List<InvoiceModel>> GetIssuedInvoicesAsync();

    Task<InvoiceModel?> GetIssuedInvoiceAsync(int invoiceId);

    Task CreateWithInvoiceAdjustmentAsync(CreditNoteModel note, InvoiceModel invoice);
}
