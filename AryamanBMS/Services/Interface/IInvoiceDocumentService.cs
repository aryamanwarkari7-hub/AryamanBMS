using AryamanBMS.Models;

namespace AryamanBMS.Services.Interfaces
{
    public interface IInvoiceDocumentService
    {
        Task<IReadOnlyList<InvoiceDocumentVersionModel>>
            GenerateAsync(
                int invoiceId,
                string generatedByUserId);
    }
}