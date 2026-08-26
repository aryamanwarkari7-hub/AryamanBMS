using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
namespace AryamanBMS.Business.Services;
public class CreditNoteService : ICreditNoteService
{
    private readonly ICreditNoteRepository _repository;
    public CreditNoteService(ICreditNoteRepository repository) => _repository = repository;
    public async Task<CreditNoteValidationData> ValidateAsync(CreditNoteModel note) { note.Reason = note.Reason?.Trim() ?? string.Empty; note.TotalCredit = Math.Round(note.TaxableValueReduction + note.CGSTAdjustment + note.SGSTAdjustment + note.IGSTAdjustment, 2); var errors = new Dictionary<string,string>(); var invoice = await _repository.GetIssuedInvoiceAsync(note.OriginalInvoiceId); if (invoice == null) errors[nameof(note.OriginalInvoiceId)] = "Select a valid issued invoice."; if (string.IsNullOrWhiteSpace(note.Reason)) errors[nameof(note.Reason)] = "Reason is required."; if (note.TotalCredit <= 0) errors[nameof(note.TotalCredit)] = "Credit note total must be greater than zero."; if (invoice != null && note.TotalCredit > invoice.GrandTotal) errors[nameof(note.TotalCredit)] = "Credit note cannot exceed invoice total."; return new CreditNoteValidationData { Errors = errors }; }
    public async Task CreateAsync(CreditNoteModel note, string? userId) { var invoice = await _repository.GetIssuedInvoiceAsync(note.OriginalInvoiceId) ?? throw new InvalidOperationException("Issued invoice not found."); note.CreatedByUserId = userId; note.ApprovedByUserId = userId; note.ApprovedOn = note.CreatedOn = DateTime.Now; invoice.GrandTotal = Math.Max(0, Math.Round(invoice.GrandTotal - note.TotalCredit, 2)); invoice.BalanceAmount = Math.Max(0, Math.Round(invoice.GrandTotal - invoice.PaidAmount, 2)); invoice.PaymentStatus = invoice.BalanceAmount <= 0 ? "Paid" : invoice.DueDate.HasValue && invoice.DueDate.Value.Date < DateTime.Today ? "Overdue" : invoice.PaidAmount > 0 ? "Partially Paid" : "Unpaid"; await _repository.CreateWithInvoiceAdjustmentAsync(note, invoice); }
}
