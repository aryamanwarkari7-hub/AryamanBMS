using AryamanBMS.Models;

namespace AryamanBMS.Business.Interfaces;

public interface IInvoiceDraftService
{
    Task CreateAsync(InvoiceModel invoice);

    Task UpdateAsync(InvoiceModel invoice);

    void NormalizeAndCalculate(InvoiceModel invoice);

    Task<Dictionary<string, string>> ApplyGstStateDecisionAsync(InvoiceModel invoice);

    Task<Dictionary<string, string>> ValidateGstPeriodAsync(DateTime invoiceDate);

    Task<Dictionary<string, string>> ValidateAndAssignProjectAsync(InvoiceModel invoice);
    Task<Dictionary<string, string>> ValidatePurchaseOrderAsync(InvoiceModel invoice);
    Task<Dictionary<string, string>> ValidateBillingMilestoneAsync(InvoiceModel invoice);
    Dictionary<string, string> ValidateBasicRules(InvoiceModel invoice);
}
