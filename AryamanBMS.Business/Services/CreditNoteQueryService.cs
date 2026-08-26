using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Business.Services;

public class CreditNoteQueryService : ICreditNoteQueryService
{
    private readonly ICreditNoteRepository _repository;

    public CreditNoteQueryService(ICreditNoteRepository repository) => _repository = repository;

    public async Task<List<CreditNoteModel>> GetAllAsync(string? search, string sortBy, string sortOrder)
    {
        var notes = await _repository.GetAllAsync();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            notes = notes.Where(x => x.CreditNoteNo.ToLower().Contains(keyword) || x.Reason.ToLower().Contains(keyword) || (x.GSTPeriod?.ToLower().Contains(keyword) ?? false) || (x.OriginalInvoice?.InvoiceNo.ToLower().Contains(keyword) ?? false) || (x.OriginalInvoice?.Client?.ClientName.ToLower().Contains(keyword) ?? false)).ToList();
        }
        bool desc = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        return sortBy switch
        {
            "CreditNoteNo" => desc ? notes.OrderByDescending(x => x.CreditNoteNo).ToList() : notes.OrderBy(x => x.CreditNoteNo).ToList(),
            "Invoice" => desc ? notes.OrderByDescending(x => x.OriginalInvoice!.InvoiceNo).ToList() : notes.OrderBy(x => x.OriginalInvoice!.InvoiceNo).ToList(),
            "Client" => desc ? notes.OrderByDescending(x => x.OriginalInvoice!.Client!.ClientName).ToList() : notes.OrderBy(x => x.OriginalInvoice!.Client!.ClientName).ToList(),
            "TotalCredit" => desc ? notes.OrderByDescending(x => x.TotalCredit).ToList() : notes.OrderBy(x => x.TotalCredit).ToList(),
            "ApprovedOn" => desc ? notes.OrderByDescending(x => x.ApprovedOn).ToList() : notes.OrderBy(x => x.ApprovedOn).ToList(),
            _ => desc ? notes.OrderByDescending(x => x.CreatedOn).ToList() : notes.OrderBy(x => x.CreatedOn).ToList()
        };
    }

    public Task<List<InvoiceModel>> GetIssuedInvoicesAsync() => _repository.GetIssuedInvoicesAsync();
}
