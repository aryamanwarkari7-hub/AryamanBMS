using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
namespace AryamanBMS.Business.Services;
public class AdvanceReceiptService(IAdvanceReceiptRepository repository) : IAdvanceReceiptService
{
    public async Task<Dictionary<string, string>> ValidateAsync(AdvanceReceiptModel receipt)
    {
        receipt.PaymentMode = receipt.PaymentMode?.Trim() ?? string.Empty; receipt.PaymentReference = string.IsNullOrWhiteSpace(receipt.PaymentReference) ? null : receipt.PaymentReference.Trim(); receipt.Remarks = string.IsNullOrWhiteSpace(receipt.Remarks) ? null : receipt.Remarks.Trim(); receipt.Amount = Math.Round(receipt.Amount, 2);
        var errors = new Dictionary<string, string>();
        if (receipt.ClientId <= 0) errors[nameof(receipt.ClientId)] = "Client is required.";
        if (receipt.Amount <= 0) errors[nameof(receipt.Amount)] = "Advance amount must be greater than zero.";
        if (receipt.ReceiptDate.Date > DateTime.Today) errors[nameof(receipt.ReceiptDate)] = "Receipt date cannot be in the future.";
        if (!string.IsNullOrWhiteSpace(receipt.PaymentReference) && await repository.PaymentReferenceExistsAsync(receipt.PaymentReference)) errors[nameof(receipt.PaymentReference)] = "This payment reference is already used.";
        return errors;
    }
    public Task CreateAsync(AdvanceReceiptModel receipt, string? userId) { receipt.AvailableBalance = receipt.Amount; receipt.AdjustedAmount = 0; receipt.CreatedOn = DateTime.Now; receipt.ReceivedByUserId = userId; return repository.CreateWithSequenceAsync(receipt); }
    public async Task<Dictionary<string, string>> ApplyAsync(int receiptId, int invoiceId, decimal amount, string? remarks)
    {
        var errors = new Dictionary<string, string>(); var receipt = await repository.GetAvailableByIdAsync(receiptId);
        if (receipt == null) { errors[nameof(receiptId)] = "Advance receipt was not found."; return errors; }
        var invoice = await repository.GetIssuedInvoiceForClientAsync(invoiceId, receipt.ClientId);
        amount = Math.Round(amount, 2);
        if (invoice == null) errors["InvoiceId"] = "Select a valid issued invoice for this client.";
        else if (invoice.BalanceAmount <= 0) errors["InvoiceId"] = "Selected invoice has no outstanding balance.";
        if (amount <= 0) errors["AmountToAdjust"] = "Adjustment amount must be greater than zero.";
        if (amount > receipt.AvailableBalance) errors["AmountToAdjust"] = $"Amount cannot exceed available advance balance {receipt.AvailableBalance:N2}.";
        if (invoice != null && amount > invoice.BalanceAmount) errors["AmountToAdjust"] = $"Amount cannot exceed invoice balance {invoice.BalanceAmount:N2}.";
        if (errors.Count > 0) return errors;
        receipt.AdjustedAmount = Math.Round(receipt.AdjustedAmount + amount, 2); receipt.AvailableBalance = Math.Max(0, Math.Round(receipt.Amount - receipt.AdjustedAmount, 2)); receipt.UpdatedOn = DateTime.Now;
        invoice!.PaidAmount = Math.Round(invoice.PaidAmount + amount, 2); invoice.BalanceAmount = Math.Max(0, Math.Round(invoice.GrandTotal - invoice.PaidAmount, 2)); invoice.PaymentStatus = invoice.BalanceAmount <= 0 ? "Paid" : invoice.DueDate.HasValue && invoice.DueDate.Value.Date < DateTime.Today ? "Overdue" : invoice.PaidAmount > 0 ? "Partially Paid" : "Unpaid";
        var note = $"Advance {receipt.AdvanceReceiptNo} adjusted against invoice {invoice.InvoiceNo}: {amount:N2}." + (string.IsNullOrWhiteSpace(remarks) ? string.Empty : $" {remarks.Trim()}"); receipt.Remarks = string.IsNullOrWhiteSpace(receipt.Remarks) ? note : $"{receipt.Remarks} | {note}";
        await repository.SaveAdjustmentAsync(); return errors;
    }
}
