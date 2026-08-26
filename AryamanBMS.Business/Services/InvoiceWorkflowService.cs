using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
namespace AryamanBMS.Business.Services;
public class InvoiceWorkflowService(IInvoiceRepository repository) : IInvoiceWorkflowService
{
    public async Task<string?> IssueAsync(InvoiceModel invoice, string userId)
    {
        if (invoice.InvoiceStatus != "Draft") return "Only draft invoices can be issued.";
        if (invoice.InvoiceDetails == null || !invoice.InvoiceDetails.Any()) return "Invoice must contain at least one item before it can be issued.";
        if (invoice.GrandTotal <= 0) return "Invoice total must be greater than zero before it can be issued.";
        invoice.InvoiceStatus = "Issued"; invoice.IssuedByUserId = userId; invoice.IssuedOn = invoice.ModifiedOn = DateTime.Now;
        await repository.UpdateAsync(invoice); await repository.SaveAsync(); return null;
    }
    public async Task<string?> CancelAsync(InvoiceModel invoice, string reason, string userId)
    {
        if (invoice.InvoiceStatus == "Cancelled") return "This invoice is already cancelled.";
        if (invoice.PaymentStatus == "Paid") return "A paid invoice cannot be cancelled directly.";
        if (string.IsNullOrWhiteSpace(reason)) return "Cancellation reason is required.";
        invoice.InvoiceStatus = "Cancelled"; invoice.CancelledByUserId = userId; invoice.CancelledOn = invoice.ModifiedOn = DateTime.Now; invoice.CancellationReason = reason.Trim();
        await repository.UpdateAsync(invoice); await repository.SaveAsync(); return null;
    }
    public async Task<string?> DeleteDraftAsync(InvoiceModel invoice)
    {
        if (invoice.PaidAmount > 0) return "Invoices with payment receipts cannot be cancelled.";
        if (invoice.InvoiceStatus == "Cancelled") return "Invoice is already cancelled.";
        await repository.DeleteAsync(invoice); await repository.SaveAsync(); return null;
    }
}
