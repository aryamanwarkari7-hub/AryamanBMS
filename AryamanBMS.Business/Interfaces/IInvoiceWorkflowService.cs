using AryamanBMS.Models;
namespace AryamanBMS.Business.Interfaces;
public interface IInvoiceWorkflowService { Task<string?> IssueAsync(InvoiceModel invoice, string userId); Task<string?> CancelAsync(InvoiceModel invoice, string reason, string userId); Task<string?> DeleteDraftAsync(InvoiceModel invoice); }
