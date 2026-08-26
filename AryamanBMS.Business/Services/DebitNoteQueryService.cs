using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
namespace AryamanBMS.Business.Services;
public class DebitNoteQueryService : IDebitNoteQueryService
{
    private readonly IDebitNoteRepository _repository;
    public DebitNoteQueryService(IDebitNoteRepository repository) => _repository = repository;
    public async Task<List<DebitNoteModel>> GetAllAsync(string? search, string sortBy, string sortOrder)
    {
        var notes = await _repository.GetAllAsync(); if (!string.IsNullOrWhiteSpace(search)) { var k = search.Trim().ToLower(); notes = notes.Where(x => x.DebitNoteNo.ToLower().Contains(k) || x.Reason.ToLower().Contains(k) || (x.OriginalInvoice?.InvoiceNo.ToLower().Contains(k) ?? false) || (x.OriginalInvoice?.Client?.ClientName.ToLower().Contains(k) ?? false)).ToList(); } bool d = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase); return sortBy switch { "DebitNoteNo" => d ? notes.OrderByDescending(x => x.DebitNoteNo).ToList() : notes.OrderBy(x => x.DebitNoteNo).ToList(), "Invoice" => d ? notes.OrderByDescending(x => x.OriginalInvoice!.InvoiceNo).ToList() : notes.OrderBy(x => x.OriginalInvoice!.InvoiceNo).ToList(), "Client" => d ? notes.OrderByDescending(x => x.OriginalInvoice!.Client!.ClientName).ToList() : notes.OrderBy(x => x.OriginalInvoice!.Client!.ClientName).ToList(), "TotalDebit" => d ? notes.OrderByDescending(x => x.TotalDebit).ToList() : notes.OrderBy(x => x.TotalDebit).ToList(), "ApprovedOn" => d ? notes.OrderByDescending(x => x.ApprovedOn).ToList() : notes.OrderBy(x => x.ApprovedOn).ToList(), _ => d ? notes.OrderByDescending(x => x.CreatedOn).ToList() : notes.OrderBy(x => x.CreatedOn).ToList() };
    }
    public Task<List<InvoiceModel>> GetIssuedInvoicesAsync() => _repository.GetIssuedInvoicesAsync();
}
