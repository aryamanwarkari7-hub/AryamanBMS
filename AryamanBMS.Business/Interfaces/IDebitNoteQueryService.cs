using AryamanBMS.Models;
namespace AryamanBMS.Business.Interfaces;
public interface IDebitNoteQueryService { Task<List<DebitNoteModel>> GetAllAsync(string? search, string sortBy, string sortOrder); Task<List<InvoiceModel>> GetIssuedInvoicesAsync(); }
