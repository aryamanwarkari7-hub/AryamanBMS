using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Business.Services;

public class InvoiceDraftService : IInvoiceDraftService
{
    private readonly IInvoiceRepository _repository;
    private readonly IGstConfigurationRepository _gstConfigurationRepository;

    public InvoiceDraftService(IInvoiceRepository repository, IGstConfigurationRepository gstConfigurationRepository)
    {
        _repository = repository;
        _gstConfigurationRepository = gstConfigurationRepository;
    }

    public Task CreateAsync(InvoiceModel invoice)
    {
        return _repository.CreateWithSequenceAsync(invoice);
    }

    public async Task UpdateAsync(InvoiceModel invoice)
    {
        await _repository.UpdateAsync(invoice);
        await _repository.SaveAsync();
    }

    public void NormalizeAndCalculate(InvoiceModel model)
    {
        model.InvoiceType = string.Equals(model.InvoiceType, "Proforma Invoice", StringComparison.OrdinalIgnoreCase) ? "Proforma Invoice" : "Tax Invoice";
        bool zeroRated = model.InvoiceType == "Proforma Invoice" || string.Equals(model.TaxTreatment, "ExportUnderLUT", StringComparison.OrdinalIgnoreCase);
        model.InvoiceDetails ??= new List<InvoiceDetailsModel>();
        model.InvoiceDetails = model.InvoiceDetails.Where(x => !string.IsNullOrWhiteSpace(x.ItemName) || x.Qty > 0 || x.Rate > 0).ToList();
        decimal subTotal = 0, gstTotal = 0, allocated = 0;
        int sortOrder = 1;
        var lines = new List<(InvoiceDetailsModel Item, decimal Amount)>();
        foreach (var item in model.InvoiceDetails)
        {
            item.ItemName = item.ItemName?.Trim() ?? string.Empty;
            item.Description = item.Description?.Trim() ?? string.Empty;
            item.Unit = item.Unit?.Trim() ?? string.Empty;
            item.SortOrder = sortOrder++;
            item.Qty = Math.Round(item.Qty, 2);
            item.Rate = Math.Round(item.Rate, 2);
            decimal amount = Math.Round(item.Qty * item.Rate, 2);
            if (zeroRated)
            {
                item.GSTPercent = 0;
                item.GSTAmount = 0;
                model.SACCode = null;
                model.IsInterState = false;
                model.SupplierStateCode = model.CustomerStateCode = model.PlaceOfSupplyStateCode = null;
                model.IsGstStateOverride = false;
                model.GstStateOverrideReason = null;
            }
            else item.GSTPercent = Math.Clamp(item.GSTPercent, 0, 100);
            item.Amount = amount;
            subTotal += amount;
            lines.Add((item, amount));
        }
        model.Discount = Math.Clamp(Math.Round(model.Discount, 2), 0, subTotal);
        model.PaidAmount = Math.Max(0, Math.Round(model.PaidAmount, 2));
        model.SubTotal = Math.Round(subTotal, 2);
        for (int i = 0; i < lines.Count; i++)
        {
            var (item, amount) = lines[i];
            decimal discount = model.Discount > 0 && subTotal > 0
                ? i == lines.Count - 1 ? model.Discount - allocated : Math.Round(model.Discount * amount / subTotal, 2)
                : 0;
            discount = Math.Clamp(discount, 0, amount);
            allocated += discount;
            item.GSTAmount = zeroRated ? 0 : Math.Round(Math.Max(0, amount - discount) * item.GSTPercent / 100m, 2);
            gstTotal += item.GSTAmount;
        }
        model.GSTAmount = zeroRated ? 0 : Math.Round(gstTotal, 2);
        model.GrandTotal = Math.Round(model.SubTotal - model.Discount + model.GSTAmount, 2);
        if (model.PaidAmount > model.GrandTotal) model.PaidAmount = model.GrandTotal;
        model.BalanceAmount = Math.Max(0, Math.Round(model.GrandTotal - model.PaidAmount, 2));
    }

    public async Task<Dictionary<string, string>> ApplyGstStateDecisionAsync(InvoiceModel model)
    {
        var errors = new Dictionary<string, string>();
        var client = await _repository.GetClientWithCountryAsync(model.ClientId);
        if (client == null) { errors[nameof(model.ClientId)] = "Select a valid client."; return errors; }
        if (client.Country == null) { errors[nameof(model.ClientId)] = "The selected client does not have a valid country."; return errors; }
        bool export = !string.Equals(client.Country.Iso2Code, "IN", StringComparison.OrdinalIgnoreCase);
        model.CustomerCountryName = client.Country.CountryName;
        model.CustomerCountryIso2Code = client.Country.Iso2Code;
        model.TaxTreatment = export ? "ExportUnderLUT" : "Domestic";
        model.LutReference = null;
        bool proforma = string.Equals(model.InvoiceType, "Proforma Invoice", StringComparison.OrdinalIgnoreCase);
        if (proforma)
        {
            ClearGstStateDecision(model);
            if (export) model.GSTNo = null;
            return errors;
        }
        var configuration = await _gstConfigurationRepository.GetActiveAsync();
        if (export)
        {
            bool validLut = !string.IsNullOrWhiteSpace(configuration?.LutReference) && configuration.LutValidFrom.HasValue && configuration.LutValidTo.HasValue && model.InvoiceDate.Date >= configuration.LutValidFrom.Value.Date && model.InvoiceDate.Date <= configuration.LutValidTo.Value.Date;
            if (!validLut) errors[nameof(model.InvoiceDate)] = "A valid LUT must exist for the invoice date before issuing an export invoice.";
            model.LutReference = configuration?.LutReference;
            model.GSTNo = null;
            ClearGstStateDecision(model);
            return errors;
        }
        model.SupplierStateCode = StateCode(configuration?.CompanyGstin);
        model.CustomerStateCode = StateCode(model.GSTNo) ?? StateCode(client.GSTNumber);
        model.PlaceOfSupplyStateCode = NormalizeStateCode(model.PlaceOfSupplyStateCode) ?? model.CustomerStateCode;
        if (model.IsGstStateOverride)
        {
            model.GstStateOverrideReason = model.GstStateOverrideReason?.Trim();
            if (string.IsNullOrWhiteSpace(model.GstStateOverrideReason)) errors[nameof(model.GstStateOverrideReason)] = "GST state override reason is required.";
        }
        else
        {
            model.GstStateOverrideReason = null;
            if (!string.IsNullOrWhiteSpace(model.SupplierStateCode) && !string.IsNullOrWhiteSpace(model.PlaceOfSupplyStateCode)) model.IsInterState = model.SupplierStateCode != model.PlaceOfSupplyStateCode;
        }
        return errors;
    }

    public async Task<Dictionary<string, string>> ValidateGstPeriodAsync(DateTime invoiceDate)
    {
        return await _repository.IsGstPeriodClosedAsync(invoiceDate)
            ? new Dictionary<string, string> { [nameof(InvoiceModel.InvoiceDate)] = "This GST period is filed or locked. Reopen the GST period before changing invoices." }
            : [];
    }

    public async Task<Dictionary<string, string>> ValidateAndAssignProjectAsync(InvoiceModel invoice)
    {
        if (!invoice.ProjectId.HasValue) { invoice.ProjectName = null; return []; }
        var project = (await _repository.GetProjectsAsync()).FirstOrDefault(x => x.Id == invoice.ProjectId.Value);
        if (project == null) return new Dictionary<string, string> { [nameof(invoice.ProjectId)] = "Selected project does not exist or is inactive." };
        invoice.ProjectName = project.ProjectName;
        return [];
    }
    public async Task<Dictionary<string, string>> ValidatePurchaseOrderAsync(InvoiceModel invoice)
    {
        if (!invoice.PurchaseWorkOrderId.HasValue) return [];
        var errors = new Dictionary<string, string>(); var order = await _repository.GetActivePurchaseOrderAsync(invoice.PurchaseWorkOrderId.Value);
        if (order == null) { errors[nameof(invoice.PurchaseWorkOrderId)] = "Selected Purchase Order is invalid."; return errors; }
        if (order.ClientId != invoice.ClientId) errors[nameof(invoice.PurchaseWorkOrderId)] = "Purchase Order does not belong to the selected client.";
        invoice.ProposalId = order.ProposalId;
        if (!order.OrderAmount.HasValue || order.OrderAmount.Value <= 0) { errors[nameof(invoice.PurchaseWorkOrderId)] = "Purchase Order approved value is required before billing."; return errors; }
        decimal available = order.OrderAmount.Value - await _repository.GetBilledTaxableAmountForPurchaseOrderAsync(order.PurchaseOrderId, invoice.InvoiceId);
        if (invoice.SubTotal - invoice.Discount > available) errors[nameof(invoice.PurchaseWorkOrderId)] = $"Billing exceeds the Purchase Order value. Available taxable billable amount is {available:N2}.";
        return errors;
    }
    public async Task<Dictionary<string, string>> ValidateBillingMilestoneAsync(InvoiceModel invoice)
    {
        if (!invoice.BillingMilestoneId.HasValue) return [];
        var errors = new Dictionary<string, string>(); var milestone = await _repository.GetActiveBillingMilestoneAsync(invoice.BillingMilestoneId.Value);
        if (milestone == null) { errors[nameof(invoice.BillingMilestoneId)] = "Selected billing milestone is invalid."; return errors; }
        if (invoice.PurchaseWorkOrderId != milestone.PurchaseWorkOrderId) errors[nameof(invoice.BillingMilestoneId)] = "Selected milestone does not belong to the selected Purchase / Work Order.";
        if (invoice.ProjectId.HasValue && milestone.ProjectId.HasValue && invoice.ProjectId.Value != milestone.ProjectId.Value) errors[nameof(invoice.BillingMilestoneId)] = "Selected milestone does not belong to the selected project.";
        if (await _repository.IsBillingMilestoneInvoicedAsync(milestone.BillingMilestoneId, invoice.InvoiceId)) errors[nameof(invoice.BillingMilestoneId)] = "This milestone has already been invoiced.";
        if (invoice.SubTotal - invoice.Discount > milestone.MilestoneValue) errors[nameof(invoice.BillingMilestoneId)] = $"Invoice taxable amount cannot exceed milestone value {milestone.MilestoneValue:N2}.";
        return errors;
    }
    public Dictionary<string, string> ValidateBasicRules(InvoiceModel invoice)
    {
        var errors = new Dictionary<string, string>();
        if (invoice.InvoiceType != "Tax Invoice" && invoice.InvoiceType != "Proforma Invoice") errors[nameof(invoice.InvoiceType)] = "Invoice type must be Tax Invoice or Proforma Invoice.";
        if (invoice.DueDate.HasValue && invoice.DueDate.Value.Date < invoice.InvoiceDate.Date) errors[nameof(invoice.DueDate)] = "Due date cannot be before invoice date.";
        if (invoice.InvoiceDetails == null || invoice.InvoiceDetails.Count == 0) { errors[nameof(invoice.InvoiceDetails)] = "At least one valid invoice item is required."; return errors; }
        for (int i = 0; i < invoice.InvoiceDetails.Count; i++) { var item = invoice.InvoiceDetails.ElementAt(i); if (string.IsNullOrWhiteSpace(item.ItemName)) errors[$"InvoiceDetails[{i}].ItemName"] = "Item name is required."; if (item.Qty <= 0) errors[$"InvoiceDetails[{i}].Qty"] = "Quantity must be greater than zero."; if (item.Rate < 0) errors[$"InvoiceDetails[{i}].Rate"] = "Rate cannot be negative."; if (item.GSTPercent < 0 || item.GSTPercent > 100) errors[$"InvoiceDetails[{i}].GSTPercent"] = "GST percentage must be between 0 and 100."; }
        if (invoice.Discount < 0) errors[nameof(invoice.Discount)] = "Discount cannot be negative.";
        if (invoice.Discount > invoice.SubTotal) errors[nameof(invoice.Discount)] = "Discount cannot exceed the subtotal.";
        if (invoice.InvoiceStatus != "Draft" && invoice.InvoiceStatus != "Issued") errors[nameof(invoice.InvoiceStatus)] = "Invoice status must be Draft or Issued.";
        return errors;
    }

    private static void ClearGstStateDecision(InvoiceModel model) { model.SupplierStateCode = model.CustomerStateCode = model.PlaceOfSupplyStateCode = null; model.IsInterState = false; model.IsGstStateOverride = false; model.GstStateOverrideReason = null; }
    private static string? StateCode(string? gstin) => string.IsNullOrWhiteSpace(gstin) || gstin.Trim().Length < 2 ? null : NormalizeStateCode(gstin.Trim()[..2]);
    private static string? NormalizeStateCode(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().PadLeft(2, '0')[..2];
}
