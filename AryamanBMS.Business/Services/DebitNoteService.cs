using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Business.Services;

public class DebitNoteService : IDebitNoteService
{
    private readonly IDebitNoteRepository _repository;

    public DebitNoteService(IDebitNoteRepository repository)
    {
        _repository = repository;
    }

    public async Task<DebitNoteValidationData> ValidateAsync(DebitNoteModel note)
    {
        Normalize(note);
        var errors = new Dictionary<string, string>();
        var invoice = await _repository.GetIssuedInvoiceAsync(note.OriginalInvoiceId);

        if (invoice == null)
            errors[nameof(note.OriginalInvoiceId)] = "Select a valid issued invoice.";
        if (string.IsNullOrWhiteSpace(note.Reason))
            errors[nameof(note.Reason)] = "Reason is required.";
        if (note.AdditionalTaxableValue < 0 || note.CGST < 0 || note.SGST < 0 || note.IGST < 0)
            errors[nameof(note.TotalDebit)] = "Debit note values cannot be negative.";

        note.TotalDebit = Math.Round(note.AdditionalTaxableValue + note.CGST + note.SGST + note.IGST, 2);
        if (note.TotalDebit <= 0)
            errors[nameof(note.TotalDebit)] = "Debit note total must be greater than zero.";

        return new DebitNoteValidationData { Errors = errors };
    }

    public async Task CreateAsync(DebitNoteModel note, string? userId)
    {
        var invoice = await _repository.GetIssuedInvoiceAsync(note.OriginalInvoiceId)
            ?? throw new InvalidOperationException("Issued invoice not found.");

        int count = await _repository.GetDebitNoteCountAsync();
        note.DebitNoteNo = $"DBN-{DateTime.Now:yyMM}-{count + 1:0000}";
        note.CreatedByUserId = userId;
        note.ApprovedByUserId = userId;
        note.ApprovedOn = note.CreatedOn = DateTime.Now;

        invoice.GrandTotal = Math.Round(invoice.GrandTotal + note.TotalDebit, 2);
        invoice.BalanceAmount = Math.Max(0, Math.Round(invoice.GrandTotal - invoice.PaidAmount, 2));
        RefreshPaymentStatus(invoice);

        string adjustment = $"Debit note {note.DebitNoteNo} applied: {note.TotalDebit:N2}.";
        invoice.Remarks = string.IsNullOrWhiteSpace(invoice.Remarks)
            ? adjustment
            : $"{invoice.Remarks} | {adjustment}";

        await _repository.CreateWithInvoiceAdjustmentAsync(note);
    }

    private static void Normalize(DebitNoteModel note)
    {
        note.Reason = note.Reason?.Trim() ?? string.Empty;
        note.AdditionalTaxableValue = Math.Round(note.AdditionalTaxableValue, 2);
        note.CGST = Math.Round(note.CGST, 2);
        note.SGST = Math.Round(note.SGST, 2);
        note.IGST = Math.Round(note.IGST, 2);
    }

    private static void RefreshPaymentStatus(InvoiceModel invoice)
    {
        if (invoice.InvoiceStatus == "Cancelled") return;
        invoice.PaymentStatus = invoice.BalanceAmount <= 0 ? "Paid"
            : invoice.DueDate.HasValue && invoice.DueDate.Value.Date < DateTime.Today ? "Overdue"
            : invoice.PaidAmount > 0 ? "Partially Paid" : "Unpaid";
    }
}
