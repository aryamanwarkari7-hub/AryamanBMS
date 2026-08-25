using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Master")]
    public class ReceivablesController : Controller
    {
        #region Actions

        private readonly IReceivablesReportService _receivablesReportService;

        public ReceivablesController(
            IReceivablesReportService receivablesReportService)
        {
            _receivablesReportService = receivablesReportService;
        }

        public async Task<IActionResult> Index()
        {
            var report = await _receivablesReportService
                .GetReceivablesAsync();

            return View(ToViewModel(report));
        }

        public async Task<IActionResult> Ageing()
        {
            var report = await _receivablesReportService.GetAgeingAsync();

            return View(ToViewModel(report));
        }
        private static ReceivablesReportViewModel ToViewModel(
            ReceivablesReportData report)
        {
            return new ReceivablesReportViewModel
            {
                TotalInvoiced = report.TotalInvoiced,
                TotalCollected = report.TotalCollected,
                TotalOutstanding = report.TotalOutstanding,
                OverdueAmount = report.OverdueAmount,
                ClientRows = report.ClientRows
                    .Select(x => new ClientReceivableRowViewModel
                    {
                        ClientId = x.ClientId,
                        ClientName = x.ClientName,
                        TotalInvoiced = x.TotalInvoiced,
                        TotalCollected = x.TotalCollected,
                        Outstanding = x.Outstanding,
                        Overdue = x.Overdue
                    })
                    .ToList(),
                ProjectRows = report.ProjectRows
                    .Select(x => new ProjectReceivableRowViewModel
                    {
                        ProjectId = x.ProjectId,
                        ProjectName = x.ProjectName,
                        TotalInvoiced = x.TotalInvoiced,
                        TotalCollected = x.TotalCollected,
                        Outstanding = x.Outstanding,
                        Overdue = x.Overdue
                    })
                    .ToList(),
                InvoiceRows = report.InvoiceRows
                    .Select(x => new InvoiceReceivableRowViewModel
                    {
                        InvoiceId = x.InvoiceId,
                        InvoiceNo = x.InvoiceNo,
                        ClientName = x.ClientName,
                        ProjectName = x.ProjectName,
                        InvoiceDate = x.InvoiceDate,
                        DueDate = x.DueDate,
                        InvoiceTotal = x.InvoiceTotal,
                        AmountReceived = x.AmountReceived,
                        OutstandingBalance = x.OutstandingBalance,
                        AgeingDays = x.AgeingDays,
                        PaymentStatus = x.PaymentStatus
                    })
                    .ToList()
            };
        }

        private static InvoiceAgeingReportViewModel ToViewModel(
            InvoiceAgeingReportData report)
        {
            return new InvoiceAgeingReportViewModel
            {
                TotalOutstanding = report.TotalOutstanding,
                Bucket0To30 = report.Bucket0To30,
                Bucket31To60 = report.Bucket31To60,
                Bucket61To90 = report.Bucket61To90,
                BucketAbove90 = report.BucketAbove90,
                Rows = report.Rows
                    .Select(x => new InvoiceAgeingRowViewModel
                    {
                        InvoiceId = x.InvoiceId,
                        InvoiceNo = x.InvoiceNo,
                        ClientName = x.ClientName,
                        InvoiceDate = x.InvoiceDate,
                        DueDate = x.DueDate,
                        InvoiceTotal = x.InvoiceTotal,
                        AmountReceived = x.AmountReceived,
                        OutstandingBalance = x.OutstandingBalance,
                        AgeingDays = x.AgeingDays,
                        AgeingBucket = x.AgeingBucket
                    })
                    .ToList()
            };
        }

        #endregion
    }
}
