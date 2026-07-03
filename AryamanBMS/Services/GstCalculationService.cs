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
                    x.Status == "Filed");
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
                throw new Exception("GST Snapshot is already filed and locked.");

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

            snapshot.PurchaseTaxableAmount = purchaseTaxable;

            snapshot.PurchaseCGST = purchaseCGST;
            snapshot.PurchaseSGST = purchaseSGST;
            snapshot.PurchaseIGST = purchaseIGST;

            snapshot.TotalOutputGST = outputGST;
            snapshot.TotalInputGST = inputGST;

            decimal difference = outputGST - inputGST;

            snapshot.NetGSTPayable =
                difference > 0 ? difference : 0;

            snapshot.InputCreditCarryForward =
                difference < 0 ? Math.Abs(difference) : 0;

            snapshot.InvoiceCount = invoices.Count;
            snapshot.ExpenseVoucherCount = expenses.Count;

            snapshot.GeneratedOn = DateTime.Now;
            snapshot.Status = "Calculated";

            await _context.SaveChangesAsync();

            return snapshot;
        }
    }
}
