using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Business.Services;

public class PaymentReceiptTrackerService : IPaymentReceiptTrackerService
{
    private const int PageSize = 20;

    private readonly IPaymentReceiptRepository _paymentRepository;

    public PaymentReceiptTrackerService(
        IPaymentReceiptRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<PaymentReceiptTrackerData> GetTrackerAsync(
        string? search,
        int? clientId,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        string sortBy,
        string sortOrder,
        int page)
    {
        var payments = await GetFilteredAsync(
            search,
            clientId,
            status,
            fromDate,
            toDate);

        payments = Sort(payments, sortBy, sortOrder);

        int totalRecords = payments.Count;

        payments = payments
            .Skip((Math.Max(page, 1) - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        var allPayments = await _paymentRepository.GetAllAsync();
        var activePayments = allPayments.Where(x => !x.IsCancelled).ToList();

        return new PaymentReceiptTrackerData
        {
            PaymentReceipts = payments,
            Clients = await _paymentRepository.GetClientsAsync(),
            Summary = new PaymentReceiptSummaryData
            {
                TotalReceipts = allPayments.Count,
                TotalReceived = activePayments.Sum(x => x.AmountReceived),
                ActiveReceiptCount = activePayments.Count,
                CancelledReceiptCount = allPayments.Count(x => x.IsCancelled)
            },
            TotalPages = (int)Math.Ceiling(totalRecords / (double)PageSize)
        };
    }

    public async Task<List<PaymentReceiptModel>> GetForExportAsync(
        string? search,
        int? clientId,
        string? status,
        DateTime? fromDate,
        DateTime? toDate)
    {
        return await GetFilteredAsync(
            search,
            clientId,
            status,
            fromDate,
            toDate);
    }

    private async Task<List<PaymentReceiptModel>> GetFilteredAsync(
        string? search,
        int? clientId,
        string? status,
        DateTime? fromDate,
        DateTime? toDate)
    {
        var payments = await _paymentRepository.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim().ToLower();

            payments = payments
                .Where(x =>
                    (!string.IsNullOrWhiteSpace(x.ReceiptNo) &&
                        x.ReceiptNo.ToLower().Contains(keyword)) ||
                    (x.Client != null &&
                        !string.IsNullOrWhiteSpace(x.Client.ClientName) &&
                        x.Client.ClientName.ToLower().Contains(keyword)) ||
                    (x.Invoice != null &&
                        !string.IsNullOrWhiteSpace(x.Invoice.InvoiceNo) &&
                        x.Invoice.InvoiceNo.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(x.PaymentMode) &&
                        x.PaymentMode.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(x.TransactionNo) &&
                        x.TransactionNo.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(x.ReferenceNo) &&
                        x.ReferenceNo.ToLower().Contains(keyword)))
                .ToList();
        }

        if (clientId.HasValue && clientId.Value > 0)
        {
            payments = payments
                .Where(x => x.ClientId == clientId.Value)
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            payments = status switch
            {
                "Active" => payments.Where(x => !x.IsCancelled).ToList(),
                "Cancelled" => payments.Where(x => x.IsCancelled).ToList(),
                _ => payments
            };
        }

        if (fromDate.HasValue)
        {
            payments = payments
                .Where(x => x.ReceiptDate.Date >= fromDate.Value.Date)
                .ToList();
        }

        if (toDate.HasValue)
        {
            payments = payments
                .Where(x => x.ReceiptDate.Date <= toDate.Value.Date)
                .ToList();
        }

        return payments;
    }

    private static List<PaymentReceiptModel> Sort(
        List<PaymentReceiptModel> payments,
        string sortBy,
        string sortOrder)
    {
        bool descending = string.Equals(
            sortOrder,
            "desc",
            StringComparison.OrdinalIgnoreCase);

        return sortBy switch
        {
            "ReceiptNo" => descending
                ? payments.OrderByDescending(x => x.ReceiptNo).ToList()
                : payments.OrderBy(x => x.ReceiptNo).ToList(),
            "Client" => descending
                ? payments.OrderByDescending(x => x.Client?.ClientName).ToList()
                : payments.OrderBy(x => x.Client?.ClientName).ToList(),
            "Invoice" => descending
                ? payments.OrderByDescending(x => x.Invoice?.InvoiceNo).ToList()
                : payments.OrderBy(x => x.Invoice?.InvoiceNo).ToList(),
            "Mode" => descending
                ? payments.OrderByDescending(x => x.PaymentMode).ToList()
                : payments.OrderBy(x => x.PaymentMode).ToList(),
            "Amount" => descending
                ? payments.OrderByDescending(x => x.AmountReceived).ToList()
                : payments.OrderBy(x => x.AmountReceived).ToList(),
            "Status" => descending
                ? payments.OrderByDescending(x => x.IsCancelled).ToList()
                : payments.OrderBy(x => x.IsCancelled).ToList(),
            _ => descending
                ? payments.OrderByDescending(x => x.ReceiptDate).ToList()
                : payments.OrderBy(x => x.ReceiptDate).ToList()
        };
    }
}
