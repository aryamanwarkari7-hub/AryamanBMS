using AryamanBMS.Data;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class ReceivablesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReceivablesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var invoices =
                await _context.Invoices
                    .AsNoTracking()
                    .Include(x => x.Client)
                    .Include(x => x.Project)
                    .Where(x =>
                        !x.IsDeleted &&
                        x.InvoiceStatus != "Cancelled" &&
                        x.InvoiceStatus != "Draft")
                    .ToListAsync();

            DateTime today = DateTime.Today;

            var invoiceRows =
                invoices
                    .Select(x => new InvoiceReceivableRowViewModel
                    {
                        InvoiceId = x.InvoiceId,
                        InvoiceNo = x.InvoiceNo,
                        ClientName = x.Client?.ClientName ?? "",
                        ProjectName =
                            x.Project?.ProjectName ??
                            x.ProjectName ??
                            "",
                        InvoiceDate = x.InvoiceDate,
                        DueDate = x.DueDate,
                        InvoiceTotal = x.GrandTotal,
                        AmountReceived = x.PaidAmount,
                        OutstandingBalance = x.BalanceAmount,
                        AgeingDays =
                            x.BalanceAmount <= 0
                                ? 0
                                : Math.Max(
                                    0,
                                    (today - (x.DueDate ?? x.InvoiceDate).Date)
                                        .Days),
                        PaymentStatus = x.PaymentStatus
                    })
                    .Where(x => x.OutstandingBalance > 0)
                    .OrderByDescending(x => x.AgeingDays)
                    .ThenBy(x => x.ClientName)
                    .ToList();

            var model = new ReceivablesReportViewModel
            {
                TotalInvoiced =
                    invoices.Sum(x => x.GrandTotal),

                TotalCollected =
                    invoices.Sum(x => x.PaidAmount),

                TotalOutstanding =
                    invoices.Sum(x => x.BalanceAmount),

                OverdueAmount =
                    invoices
                        .Where(x =>
                            x.BalanceAmount > 0 &&
                            x.DueDate.HasValue &&
                            x.DueDate.Value.Date < today)
                        .Sum(x => x.BalanceAmount),

                InvoiceRows = invoiceRows,

                ClientRows =
                    invoices
                        .GroupBy(x => new
                        {
                            x.ClientId,
                            ClientName = x.Client!.ClientName
                        })
                        .Select(g => new ClientReceivableRowViewModel
                        {
                            ClientId = g.Key.ClientId,
                            ClientName = g.Key.ClientName,
                            TotalInvoiced = g.Sum(x => x.GrandTotal),
                            TotalCollected = g.Sum(x => x.PaidAmount),
                            Outstanding = g.Sum(x => x.BalanceAmount),
                            Overdue =
                                g.Where(x =>
                                        x.BalanceAmount > 0 &&
                                        x.DueDate.HasValue &&
                                        x.DueDate.Value.Date < today)
                                    .Sum(x => x.BalanceAmount)
                        })
                        .OrderByDescending(x => x.Outstanding)
                        .ToList(),

                ProjectRows =
                    invoices
                        .GroupBy(x => new
                        {
                            x.ProjectId,
                            ProjectName =
                                x.Project != null
                                    ? x.Project.ProjectName
                                    : x.ProjectName ?? "No Project"
                        })
                        .Select(g => new ProjectReceivableRowViewModel
                        {
                            ProjectId = g.Key.ProjectId,
                            ProjectName = g.Key.ProjectName,
                            TotalInvoiced = g.Sum(x => x.GrandTotal),
                            TotalCollected = g.Sum(x => x.PaidAmount),
                            Outstanding = g.Sum(x => x.BalanceAmount),
                            Overdue =
                                g.Where(x =>
                                        x.BalanceAmount > 0 &&
                                        x.DueDate.HasValue &&
                                        x.DueDate.Value.Date < today)
                                    .Sum(x => x.BalanceAmount)
                        })
                        .OrderByDescending(x => x.Outstanding)
                        .ToList()
            };

            return View(model);
        }

        public async Task<IActionResult> Ageing()
        {
            DateTime today = DateTime.Today;

            var invoices =
                await _context.Invoices
                    .AsNoTracking()
                    .Include(x => x.Client)
                    .Where(x =>
                        !x.IsDeleted &&
                        x.InvoiceStatus != "Cancelled" &&
                        x.InvoiceStatus != "Draft" &&
                        x.BalanceAmount > 0)
                    .ToListAsync();

            var rows =
                invoices
                    .Select(x =>
                    {
                        int ageingDays =
                            Math.Max(
                                0,
                                (today - (x.DueDate ?? x.InvoiceDate).Date)
                                    .Days);

                        return new InvoiceAgeingRowViewModel
                        {
                            InvoiceId = x.InvoiceId,
                            InvoiceNo = x.InvoiceNo,
                            ClientName = x.Client?.ClientName ?? "",
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

            var model = new InvoiceAgeingReportViewModel
            {
                Rows = rows,
                TotalOutstanding =
                    rows.Sum(x => x.OutstandingBalance),
                Bucket0To30 =
                    rows.Where(x => x.AgeingBucket == "0-30 Days")
                        .Sum(x => x.OutstandingBalance),
                Bucket31To60 =
                    rows.Where(x => x.AgeingBucket == "31-60 Days")
                        .Sum(x => x.OutstandingBalance),
                Bucket61To90 =
                    rows.Where(x => x.AgeingBucket == "61-90 Days")
                        .Sum(x => x.OutstandingBalance),
                BucketAbove90 =
                    rows.Where(x => x.AgeingBucket == "Above 90 Days")
                        .Sum(x => x.OutstandingBalance)
            };

            return View(model);
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
}