using AryamanBMS.Models;
namespace AryamanBMS.Repositories.Interfaces;
public interface IDebitNoteRepository
{
    Task<List<DebitNoteModel>> GetAllAsync();
    Task<List<InvoiceModel>> GetIssuedInvoicesAsync();
    Task<InvoiceModel?> GetIssuedInvoiceAsync(int invoiceId);
    Task<int> GetDebitNoteCountAsync();
    Task CreateWithInvoiceAdjustmentAsync(DebitNoteModel note);
}
