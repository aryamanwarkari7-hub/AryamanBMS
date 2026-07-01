using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using AryamanBMS.ViewModels;

namespace AryamanBMS.Services
{
    public class GstDashboardService : IGstDashboardService
    {
        private readonly IGstSnapshotRepository _snapshotRepository;

        public GstDashboardService(
            IGstSnapshotRepository snapshotRepository)
        {
            _snapshotRepository = snapshotRepository;
        }

        public async Task<GstDashboardViewModel> GetDashboardAsync(
            int month,
            int year)
        {
            var snapshot = await _snapshotRepository
                .GetByMonthYearAsync(month, year);

            if (snapshot == null)
            {
                return new GstDashboardViewModel
                {
                    Month = month,
                    Year = year
                };
            }

            return new GstDashboardViewModel
            {
                Month = snapshot.Month,
                Year = snapshot.Year,
                FinancialYear = snapshot.FinancialYear,

                SalesTaxable = snapshot.SalesTaxableAmount,
                OutputGST = snapshot.TotalOutputGST,
                InputGST = snapshot.TotalInputGST,
                NetGSTPayable = snapshot.NetGSTPayable,

                InvoiceCount = snapshot.InvoiceCount,
                ExpenseVoucherCount = snapshot.ExpenseVoucherCount,

                Gstr1Status = snapshot.Returns
                    .FirstOrDefault(x => x.ReturnType == "GSTR1")?.Status ?? "Pending",

                Gstr3BStatus = snapshot.Returns
                    .FirstOrDefault(x => x.ReturnType == "GSTR3B")?.Status ?? "Pending",

                ChallanStatus = snapshot.Challans
                    .FirstOrDefault()?.Status ?? "Pending"
            };
        }
    }
}