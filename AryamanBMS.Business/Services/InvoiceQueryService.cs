using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Business.Services;

public class InvoiceQueryService : IInvoiceQueryService
{
    private readonly IInvoiceRepository _repository;

    public InvoiceQueryService(IInvoiceRepository repository)
    {
        _repository = repository;
    }

    public async Task<InvoiceTrackerData> GetTrackerAsync(
        string? search,
        int? clientId,
        string? invoiceStatus,
        string? paymentStatus,
        int? month,
        int? year)
    {
        var allInvoices = await _repository.GetAllAsync();
        var activeInvoices = allInvoices.Where(x => !x.IsDeleted).ToList();
        IEnumerable<InvoiceModel> filteredInvoices = activeInvoices;

        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim();
            filteredInvoices = filteredInvoices.Where(x =>
                x.InvoiceNo.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (x.Client?.ClientName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.Project?.ProjectName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.ProjectName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (clientId.HasValue)
            filteredInvoices = filteredInvoices.Where(x => x.ClientId == clientId.Value);
        if (!string.IsNullOrWhiteSpace(invoiceStatus))
            filteredInvoices = filteredInvoices.Where(x => string.Equals(x.InvoiceStatus, invoiceStatus, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(paymentStatus))
            filteredInvoices = filteredInvoices.Where(x => string.Equals(x.PaymentStatus, paymentStatus, StringComparison.OrdinalIgnoreCase));
        if (month is >= 1 and <= 12)
            filteredInvoices = filteredInvoices.Where(x => x.InvoiceDate.Month == month.Value);
        if (year is > 0)
            filteredInvoices = filteredInvoices.Where(x => x.InvoiceDate.Year == year.Value);

        var availableYears = activeInvoices.Select(x => x.InvoiceDate.Year).Distinct().OrderByDescending(x => x).ToList();
        if (!availableYears.Contains(DateTime.Today.Year))
            availableYears.Insert(0, DateTime.Today.Year);

        return new InvoiceTrackerData
        {
            Invoices = filteredInvoices.OrderByDescending(x => x.InvoiceDate).ThenByDescending(x => x.InvoiceId).ToList(),
            Clients = await _repository.GetClientsAsync(),
            AvailableYears = availableYears,
            TotalInvoices = activeInvoices.Count,
            DraftCount = activeInvoices.Count(x => x.InvoiceStatus == "Draft"),
            IssuedCount = activeInvoices.Count(x => x.InvoiceStatus == "Issued"),
            CancelledCount = allInvoices.Count(x => x.InvoiceStatus == "Cancelled" || x.IsDeleted),
            UnpaidCount = activeInvoices.Count(x => x.PaymentStatus == "Unpaid"),
            PartiallyPaidCount = activeInvoices.Count(x => x.PaymentStatus == "Partially Paid"),
            PaidCount = activeInvoices.Count(x => x.PaymentStatus == "Paid"),
            TotalInvoiceAmount = activeInvoices.Sum(x => x.GrandTotal),
            TotalReceivedAmount = activeInvoices.Sum(x => x.PaidAmount),
            TotalOutstandingAmount = activeInvoices.Sum(x => x.BalanceAmount)
        };
    }
}
