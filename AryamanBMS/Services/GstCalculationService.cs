using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Services
{
    public class GstCalculationService : IGstCalculationService
    {
        private readonly ApplicationDbContext _context;

        public GstCalculationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsSnapshotLockedAsync(int month, int year)
        {
            return await _context.GstMonthlySnapshots
                .AnyAsync(x =>
                    x.Month == month &&
                    x.Year == year &&
                    (x.Status == FinancialConstants.GstSnapshotStatus.Filed ||
                     x.Status == FinancialConstants.GstSnapshotStatus.Locked));
        }

        public async Task<decimal> GetOutputGSTAsync(int month, int year)
        {
            return await _context.Invoices
                .Where(x =>
                    !x.IsDeleted &&
                    x.InvoiceStatus != FinancialConstants.InvoiceStatus.Draft &&
                    x.InvoiceStatus != FinancialConstants.InvoiceStatus.Cancelled &&
                    x.InvoiceDate.Month == month &&
                    x.InvoiceDate.Year == year)
                .SumAsync(x => (decimal?)x.GSTAmount) ?? 0;
        }

        public async Task<decimal> GetInputGSTAsync(int month, int year)
        {
            return await _context.ExpenseVouchers
                .Where(x =>
                    x.IsActive &&
                    !x.IsReversed &&
                    x.ITCEligible &&
                    x.Status == FinancialConstants.ExpenseVoucherStatus.Posted &&
                    x.VoucherDate.Month == month &&
                    x.VoucherDate.Year == year)
                .SumAsync(x => (decimal?)x.TotalGSTAmount) ?? 0;
        }

        public async Task<decimal> GetNetGSTAsync(int month, int year)
        {
            decimal output = await GetOutputGSTAsync(month, year);
            decimal input = await GetInputGSTAsync(month, year);

            decimal difference = output - input;

            return difference > 0 ? difference : 0;
        }

        public async Task<GstMonthlySnapshotModel> GenerateMonthlySnapshotAsync(
            int month,
            int year)
        {
            if (await IsSnapshotLockedAsync(month, year))
                throw new Exception("GST Snapshot is already filed or locked.");

            var invoices = await _context.Invoices
                .Where(x =>
                    !x.IsDeleted &&
                    x.InvoiceStatus != FinancialConstants.InvoiceStatus.Draft &&
                    x.InvoiceStatus != FinancialConstants.InvoiceStatus.Cancelled &&
                    x.InvoiceDate.Month == month &&
                    x.InvoiceDate.Year == year)
                .ToListAsync();

            var expenses = await _context.ExpenseVouchers
                .Where(x =>
                    x.IsActive &&
                    !x.IsReversed &&
                    x.ITCEligible &&
                    x.Status == FinancialConstants.ExpenseVoucherStatus.Posted &&
                    x.VoucherDate.Month == month &&
                    x.VoucherDate.Year == year)
                .ToListAsync();

            decimal salesTaxable = invoices.Sum(x => x.SubTotal);
            decimal outputGST = invoices.Sum(x => x.GSTAmount);

            decimal purchaseTaxable = expenses.Sum(x => x.Amount);

            decimal purchaseCGST = expenses.Sum(x => x.CGSTAmount);
            decimal purchaseSGST = expenses.Sum(x => x.SGSTAmount);
            decimal purchaseIGST = expenses.Sum(x => x.IGSTAmount);

            decimal inputGST = expenses.Sum(x => x.TotalGSTAmount);

            if (!invoices.Any() && !expenses.Any())
            {
                throw new InvalidOperationException(
                    "No GST records were found for the selected period. Add a " +
                    "valid invoice or posted ITC expense voucher before generating a snapshot.");
            }

            var snapshot = await _context.GstMonthlySnapshots
                .FirstOrDefaultAsync(x =>
                    x.Month == month &&
                    x.Year == year);

            if (snapshot == null)
            {
                snapshot = new GstMonthlySnapshotModel();

                _context.GstMonthlySnapshots.Add(snapshot);
            }

            snapshot.Month = month;
            snapshot.Year = year;

            snapshot.FinancialYear =
                month >= 4
                ? $"{year}-{(year + 1).ToString().Substring(2)}"
                : $"{year - 1}-{year.ToString().Substring(2)}";

            snapshot.SalesTaxableAmount = salesTaxable;

            decimal salesIGST = invoices
                .Where(x => x.IsInterState)
                .Sum(x => x.GSTAmount);

            decimal salesIntrastateGST = invoices
                .Where(x => !x.IsInterState)
                .Sum(x => x.GSTAmount);

            decimal salesCGST = Math.Round(
                salesIntrastateGST / 2,
                2,
                MidpointRounding.AwayFromZero);

            decimal salesSGST =
                salesIntrastateGST - salesCGST;

            snapshot.SalesCGST = salesCGST;
            snapshot.SalesSGST = salesSGST;
            snapshot.SalesIGST = salesIGST;
            snapshot.OutputCGSTBalance = salesCGST;
            snapshot.OutputSGSTBalance = salesSGST;
            snapshot.OutputIGSTBalance = salesIGST;

            snapshot.PurchaseTaxableAmount = purchaseTaxable;

            snapshot.PurchaseCGST = purchaseCGST;
            snapshot.PurchaseSGST = purchaseSGST;
            snapshot.PurchaseIGST = purchaseIGST;
            snapshot.InputCGSTBalance = purchaseCGST;
            snapshot.InputSGSTBalance = purchaseSGST;
            snapshot.InputIGSTBalance = purchaseIGST;

            snapshot.TotalOutputGST = outputGST;
            snapshot.TotalInputGST = inputGST;

            snapshot.PreviousITCCarryForward =
                await GetPreviousCarryForwardAsync(month, year);

            ApplyItcUtilization(snapshot);

            snapshot.NetGSTPayable =
                snapshot.CashPayableIGST +
                snapshot.CashPayableCGST +
                snapshot.CashPayableSGST;

            snapshot.InvoiceCount = invoices.Count;
            snapshot.ExpenseVoucherCount = expenses.Count;

            snapshot.GeneratedOn = DateTime.Now;
            snapshot.Status = FinancialConstants.GstSnapshotStatus.Calculated;

            await _context.SaveChangesAsync();

            return snapshot;
        }

        private async Task<decimal> GetPreviousCarryForwardAsync(
            int month,
            int year)
        {
            int previousMonth = month == 1 ? 12 : month - 1;
            int previousYear = month == 1 ? year - 1 : year;

            return await _context.GstMonthlySnapshots
                .Where(x =>
                    x.Month == previousMonth &&
                    x.Year == previousYear)
                .Select(x => (decimal?)x.InputCreditCarryForward)
                .FirstOrDefaultAsync() ?? 0m;
        }

        private static void ApplyItcUtilization(
            GstMonthlySnapshotModel snapshot)
        {
            decimal availableIgstCredit =
                snapshot.InputIGSTBalance +
                snapshot.PreviousITCCarryForward;
            decimal availableCgstCredit = snapshot.InputCGSTBalance;
            decimal availableSgstCredit = snapshot.InputSGSTBalance;

            decimal igstOutput = snapshot.OutputIGSTBalance;
            decimal cgstOutput = snapshot.OutputCGSTBalance;
            decimal sgstOutput = snapshot.OutputSGSTBalance;

            decimal igstToIgst = Math.Min(igstOutput, availableIgstCredit);
            igstOutput -= igstToIgst;
            availableIgstCredit -= igstToIgst;

            decimal igstToCgst = Math.Min(cgstOutput, availableIgstCredit);
            cgstOutput -= igstToCgst;
            availableIgstCredit -= igstToCgst;

            decimal igstToSgst = Math.Min(sgstOutput, availableIgstCredit);
            sgstOutput -= igstToSgst;
            availableIgstCredit -= igstToSgst;

            decimal cgstToCgst = Math.Min(cgstOutput, availableCgstCredit);
            cgstOutput -= cgstToCgst;
            availableCgstCredit -= cgstToCgst;

            decimal cgstToIgst = Math.Min(igstOutput, availableCgstCredit);
            igstOutput -= cgstToIgst;
            availableCgstCredit -= cgstToIgst;

            decimal sgstToSgst = Math.Min(sgstOutput, availableSgstCredit);
            sgstOutput -= sgstToSgst;
            availableSgstCredit -= sgstToSgst;

            decimal sgstToIgst = Math.Min(igstOutput, availableSgstCredit);
            igstOutput -= sgstToIgst;
            availableSgstCredit -= sgstToIgst;

            snapshot.ITCUtilizedForIGST = igstToIgst + cgstToIgst + sgstToIgst;
            snapshot.ITCUtilizedForCGST = igstToCgst + cgstToCgst;
            snapshot.ITCUtilizedForSGST = igstToSgst + sgstToSgst;

            snapshot.CashPayableIGST = igstOutput;
            snapshot.CashPayableCGST = cgstOutput;
            snapshot.CashPayableSGST = sgstOutput;
            snapshot.InputCreditCarryForward =
                availableIgstCredit +
                availableCgstCredit +
                availableSgstCredit;
        }
    }
}
