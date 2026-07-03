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
                    Year = year,
                    FinancialYear = month >= 4
                       ? $"{year}-{(year + 1).ToString().Substring(2)}"
                       : $"{year - 1}-{year.ToString().Substring(2)}",
                    SnapshotStatus = "Pending",
                    SnapshotId = 0
                };
            }

            var gstr1 =
                snapshot.Returns
                    .FirstOrDefault(x => x.ReturnType == "GSTR1");

            var gstr3b =
                snapshot.Returns
                    .FirstOrDefault(x => x.ReturnType == "GSTR3B");

            var latestChallan =
                snapshot.Challans
                    .OrderByDescending(x => x.PaymentDate)
                    .ThenByDescending(x => x.CreatedOn)
                    .FirstOrDefault();

            return new GstDashboardViewModel
            {
                SnapshotId = snapshot.SnapshotId,

                Month = snapshot.Month,
                Year = snapshot.Year,
                FinancialYear = snapshot.FinancialYear,
                SnapshotStatus = snapshot.Status,
                SalesTaxable = snapshot.SalesTaxableAmount,
                OutputGST = snapshot.TotalOutputGST,
                InputGST = snapshot.TotalInputGST,
                NetGSTPayable = snapshot.NetGSTPayable,
                InputCreditCarryForward = snapshot.InputCreditCarryForward,

                InvoiceCount = snapshot.InvoiceCount,
                ExpenseVoucherCount = snapshot.ExpenseVoucherCount,

                Gstr1Status = gstr1?.Status ?? "Pending",
                Gstr1ArnNumber = gstr1?.ArnNumber,
                Gstr1FiledDate = gstr1?.FiledDate,
                Gstr1Remarks = gstr1?.Remarks,

                Gstr3BStatus = gstr3b?.Status ?? "Pending",
                Gstr3BArnNumber = gstr3b?.ArnNumber,
                Gstr3BFiledDate = gstr3b?.FiledDate,
                Gstr3BRemarks = gstr3b?.Remarks,

                ChallanStatus = latestChallan?.Status ?? "Pending",
                ChallanNumber = latestChallan?.ChallanNumber,
                AmountPaid = latestChallan?.AmountPaid ?? snapshot.NetGSTPayable,
                PaymentDate = latestChallan?.PaymentDate,
                PaymentMode = latestChallan?.PaymentMode,
                BankName = latestChallan?.BankName,
                CPIN = latestChallan?.CPIN,
                CIN = latestChallan?.CIN,
                ChallanRemarks = latestChallan?.Remarks,
                Challans = snapshot.Challans
                    .OrderByDescending(x => x.PaymentDate)
                    .ThenByDescending(x => x.CreatedOn)
                    .ToList(),
                Documents = snapshot.Documents
                    .OrderByDescending(x => x.UploadedOn)
                    .ToList()
            };
        }
    }
}
