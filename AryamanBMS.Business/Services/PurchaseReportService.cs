using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Business.Services;

public class PurchaseReportService : IPurchaseReportService
{
    private readonly IExpenseVoucherRepository _expenseVoucherRepository;

    public PurchaseReportService(
        IExpenseVoucherRepository expenseVoucherRepository)
    {
        _expenseVoucherRepository = expenseVoucherRepository;
    }

    public async Task<PurchaseReportData> GetAsync(
        string? search,
        string sortBy,
        string sortOrder)
    {
        var vouchers = await _expenseVoucherRepository
            .GetActiveForPurchaseReportAsync();

        var vendorPayable = vouchers
            .GroupBy(x => x.Vendor?.VendorName ?? x.VendorName ?? "Unmapped")
            .Select(CreateRow)
            .ToList();

        return new PurchaseReportData
        {
            VendorPayable = ApplyRowFilters(
                vendorPayable,
                search,
                sortBy,
                sortOrder),
            CategoryWise = ApplyRowFilters(
                BuildRows(vouchers, x => x.Category?.CategoryName ?? "Unmapped"),
                search,
                sortBy,
                sortOrder),
            VendorWise = ApplyRowFilters(
                BuildRows(
                    vouchers,
                    x => x.Vendor?.VendorName ?? x.VendorName ?? "Unmapped"),
                search,
                sortBy,
                sortOrder),
            ProjectWise = ApplyRowFilters(
                BuildRows(vouchers, x => x.Project?.ProjectName ?? "Not Linked"),
                search,
                sortBy,
                sortOrder),
            DepartmentWise = ApplyRowFilters(
                BuildRows(
                    vouchers,
                    x => x.Department?.DepartmentName ?? "Not Linked"),
                search,
                sortBy,
                sortOrder),
            Reimbursements = ApplyRowFilters(
                BuildRows(
                    vouchers.Where(x => x.IsEmployeeReimbursement),
                    x => x.ReimbursementStatus),
                search,
                sortBy,
                sortOrder),
            Itc = ApplyRowFilters(
                BuildRows(vouchers, x => x.ITCStatus),
                search,
                sortBy,
                sortOrder),
            PaidUnpaid = ApplyRowFilters(
                BuildRows(vouchers, x => x.PaymentStatus),
                search,
                sortBy,
                sortOrder),
            Monthly = ApplyRowFilters(
                BuildRows(vouchers, x => $"{x.VoucherDate:yyyy-MM}"),
                search,
                sortBy,
                sortOrder),
            Capital = ApplyRowFilters(
                BuildRows(
                    vouchers.Where(x =>
                        x.Category != null && x.Category.IsCapitalExpense),
                    x => x.Category?.CategoryName ?? "Capital"),
                search,
                sortBy,
                sortOrder)
        };
    }

    private static List<PurchaseReportRow> BuildRows(
        IEnumerable<ExpenseVoucherModel> vouchers,
        Func<ExpenseVoucherModel, string> groupSelector)
    {
        return vouchers
            .GroupBy(groupSelector)
            .Select(CreateRow)
            .OrderByDescending(x => x.TotalAmount)
            .ToList();
    }

    private static PurchaseReportRow CreateRow(
        IGrouping<string, ExpenseVoucherModel> group)
    {
        return new PurchaseReportRow
        {
            Label = group.Key,
            Count = group.Count(),
            TaxableAmount = group.Sum(x =>
                x.TaxableAmount > 0 ? x.TaxableAmount : x.Amount),
            GstAmount = group.Sum(x => x.TotalGSTAmount),
            TotalAmount = group.Sum(x => x.TotalAmount),
            PaidAmount = group.Sum(x => x.PaidAmount),
            BalanceAmount = group.Sum(x => x.BalanceAmount)
        };
    }

    private static List<PurchaseReportRow> ApplyRowFilters(
        List<PurchaseReportRow> rows,
        string? search,
        string sortBy,
        string sortOrder)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim().ToLower();

            rows = rows
                .Where(x => x.Label.ToLower().Contains(keyword))
                .ToList();
        }

        bool descending = sortOrder == "desc";

        return sortBy switch
        {
            "Group" => descending
                ? rows.OrderByDescending(x => x.Label).ToList()
                : rows.OrderBy(x => x.Label).ToList(),
            "Count" => descending
                ? rows.OrderByDescending(x => x.Count).ToList()
                : rows.OrderBy(x => x.Count).ToList(),
            "Taxable" => descending
                ? rows.OrderByDescending(x => x.TaxableAmount).ToList()
                : rows.OrderBy(x => x.TaxableAmount).ToList(),
            "GST" => descending
                ? rows.OrderByDescending(x => x.GstAmount).ToList()
                : rows.OrderBy(x => x.GstAmount).ToList(),
            "Paid" => descending
                ? rows.OrderByDescending(x => x.PaidAmount).ToList()
                : rows.OrderBy(x => x.PaidAmount).ToList(),
            "Balance" => descending
                ? rows.OrderByDescending(x => x.BalanceAmount).ToList()
                : rows.OrderBy(x => x.BalanceAmount).ToList(),
            _ => descending
                ? rows.OrderByDescending(x => x.TotalAmount).ToList()
                : rows.OrderBy(x => x.TotalAmount).ToList()
        };
    }
}
