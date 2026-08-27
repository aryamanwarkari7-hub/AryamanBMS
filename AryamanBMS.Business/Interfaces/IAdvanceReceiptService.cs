using AryamanBMS.Models;
namespace AryamanBMS.Business.Interfaces;
public interface IAdvanceReceiptService { Task<Dictionary<string, string>> ValidateAsync(AdvanceReceiptModel receipt); Task CreateAsync(AdvanceReceiptModel receipt, string? userId); Task<Dictionary<string, string>> ApplyAsync(int receiptId, int invoiceId, decimal amount, string? remarks); }
