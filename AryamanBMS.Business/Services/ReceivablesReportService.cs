using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Business.Services;

public class ReceivablesReportService : IReceivablesReportService
{
    private readonly IInvoiceRepository _invoiceRepository;

    public ReceivablesReportService(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<ReceivablesReportData> GetReceivablesAsync()
    {
        var invoices = await _invoiceRepository.GetForReceivablesAsync();
        DateTime today = DateTime.Today;

        var invoiceRows = invoices
            .Select(x => new InvoiceReceivableData
            {
                InvoiceId = x.InvoiceId,
                InvoiceNo = x.InvoiceNo,
                ClientName = x.Client?.ClientName ?? string.Empty,
                ProjectName = x.Project?.ProjectName ?? x.ProjectName ?? string.Empty,
                InvoiceDate = x.InvoiceDate,
                DueDate = x.DueDate,
                InvoiceTotal = x.GrandTotal,
                AmountReceived = x.PaidAmount,
                OutstandingBalance = x.BalanceAmount,
                AgeingDays = x.BalanceAmount <= 0
                    ? 0
                    : Math.Max(
                        0,
                        (today - (x.DueDate ?? x.InvoiceDate).Date).Days),
                PaymentStatus = x.PaymentStatus
            })
            .Where(x => x.OutstandingBalance > 0)
            .OrderByDescending(x => x.AgeingDays)
            .ThenBy(x => x.ClientName)
            .ToList();

        return new ReceivablesReportData
        {
            TotalInvoiced = invoices.Sum(x => x.GrandTotal),
            TotalCollected = invoices.Sum(x => x.PaidAmount),
            TotalOutstanding = invoices.Sum(x => x.BalanceAmount),
            OverdueAmount = invoices
                .Where(x =>
                    x.BalanceAmount > 0 &&
                    x.DueDate.HasValue &&
                    x.DueDate.Value.Date < today)
                .Sum(x => x.BalanceAmount),
            InvoiceRows = invoiceRows,
            ClientRows = invoices
                .GroupBy(x => new
                {
                    x.ClientId,
                    ClientName = x.Client!.ClientName
                })
                .Select(group => new ClientReceivableData
                {
                    ClientId = group.Key.ClientId,
                    ClientName = group.Key.ClientName,
                    TotalInvoiced = group.Sum(x => x.GrandTotal),
                    TotalCollected = group.Sum(x => x.PaidAmount),
                    Outstanding = group.Sum(x => x.BalanceAmount),
                    Overdue = group
                        .Where(x =>
                            x.BalanceAmount > 0 &&
                            x.DueDate.HasValue &&
                            x.DueDate.Value.Date < today)
                        .Sum(x => x.BalanceAmount)
                })
                .OrderByDescending(x => x.Outstanding)
                .ToList(),
            ProjectRows = invoices
                .GroupBy(x => new
                {
                    x.ProjectId,
                    ProjectName = x.Project != null
                        ? x.Project.ProjectName
                        : x.ProjectName ?? "No Project"
                })
                .Select(group => new ProjectReceivableData
                {
                    ProjectId = group.Key.ProjectId,
                    ProjectName = group.Key.ProjectName,
                    TotalInvoiced = group.Sum(x => x.GrandTotal),
                    TotalCollected = group.Sum(x => x.PaidAmount),
                    Outstanding = group.Sum(x => x.BalanceAmount),
                    Overdue = group
                        .Where(x =>
                            x.BalanceAmount > 0 &&
                            x.DueDate.HasValue &&
                            x.DueDate.Value.Date < today)
                        .Sum(x => x.BalanceAmount)
                })
                .OrderByDescending(x => x.Outstanding)
                .ToList()
        };
    }

    public async Task<InvoiceAgeingReportData> GetAgeingAsync()
    {
        var invoices = await _invoiceRepository.GetOutstandingForAgeingAsync();
        DateTime today = DateTime.Today;

        var rows = invoices
            .Select(x =>
            {
                int ageingDays = Math.Max(
                    0,
                    (today - (x.DueDate ?? x.InvoiceDate).Date).Days);

                return new InvoiceAgeingData
                {
                    InvoiceId = x.InvoiceId,
                    InvoiceNo = x.InvoiceNo,
                    ClientName = x.Client?.ClientName ?? string.Empty,
                    InvoiceDate = x.InvoiceDate,
                    DueDate = x.DueDate,
                    InvoiceTotal = x.GrandTotal,
                    AmountReceived = x.PaidAmount,
                    OutstandingBalance = x.BalanceAmount,
                    AgeingDays = ageingDays,
                    AgeingBucket = GetAgeingBucket(ageingDays)
                };
            })
            .OrderByDescending(x => x.AgeingDays)
            .ThenBy(x => x.ClientName)
            .ToList();

        return new InvoiceAgeingReportData
        {
            Rows = rows,
            TotalOutstanding = rows.Sum(x => x.OutstandingBalance),
            Bucket0To30 = rows
                .Where(x => x.AgeingBucket == "0-30 Days")
                .Sum(x => x.OutstandingBalance),
            Bucket31To60 = rows
                .Where(x => x.AgeingBucket == "31-60 Days")
                .Sum(x => x.OutstandingBalance),
            Bucket61To90 = rows
                .Where(x => x.AgeingBucket == "61-90 Days")
                .Sum(x => x.OutstandingBalance),
            BucketAbove90 = rows
                .Where(x => x.AgeingBucket == "Above 90 Days")
                .Sum(x => x.OutstandingBalance)
        };
    }

    private static string GetAgeingBucket(int ageingDays)
    {
        if (ageingDays <= 30)
        {
            return "0-30 Days";
        }

        if (ageingDays <= 60)
        {
            return "31-60 Days";
        }

        if (ageingDays <= 90)
        {
            return "61-90 Days";
        }

        return "Above 90 Days";
    }
}
