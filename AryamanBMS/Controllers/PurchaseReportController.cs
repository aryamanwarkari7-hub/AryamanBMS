using AryamanBMS.Data;
using AryamanBMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Master")]
    public class PurchaseReportController : Controller
    {
        #region Actions

        private readonly ApplicationDbContext _context;

        public PurchaseReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
             string report = "VendorPayable",
             string? search = null,
             string sortBy = "Total",
             string sortOrder = "desc")
        {
            var vouchers = await _context.ExpenseVouchers
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Include(x => x.Vendor)
                .Include(x => x.Category)
                .Include(x => x.Project)
                .Include(x => x.Department)
                .ToListAsync();

            var vendorPayable = vouchers
            .GroupBy(x => x.Vendor?.VendorName ?? x.VendorName ?? "Unmapped")
            .Select(x => new PurchaseReportRow
            {
                Label = x.Key,
                Count = x.Count(),
                TaxableAmount = x.Sum(v => v.TaxableAmount > 0 ? v.TaxableAmount : v.Amount),
                GstAmount = x.Sum(v => v.TotalGSTAmount),
                TotalAmount = x.Sum(v => v.TotalAmount),
                PaidAmount = x.Sum(v => v.PaidAmount),
                BalanceAmount = x.Sum(v => v.BalanceAmount)
            })
            .ToList();

            ViewBag.Report = report;
            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

            ViewBag.VendorPayable = ApplyRowFilters(vendorPayable, search, sortBy, sortOrder);
            ViewBag.CategoryWise = ApplyRowFilters(BuildRows(vouchers, x => x.Category?.CategoryName ?? "Unmapped"), search, sortBy, sortOrder);
            ViewBag.VendorWise = ApplyRowFilters(BuildRows(vouchers, x => x.Vendor?.VendorName ?? x.VendorName ?? "Unmapped"), search, sortBy, sortOrder);
            ViewBag.ProjectWise = ApplyRowFilters(BuildRows(vouchers, x => x.Project?.ProjectName ?? "Not Linked"), search, sortBy, sortOrder);
            ViewBag.DepartmentWise = ApplyRowFilters(BuildRows(vouchers, x => x.Department?.DepartmentName ?? "Not Linked"), search, sortBy, sortOrder);
            ViewBag.Reimbursements = ApplyRowFilters(
                BuildRows(vouchers.Where(x => x.IsEmployeeReimbursement), x => x.ReimbursementStatus),
                search,
                sortBy,
                sortOrder);

            ViewBag.Itc = ApplyRowFilters(BuildRows(vouchers, x => x.ITCStatus), search, sortBy, sortOrder);
            ViewBag.PaidUnpaid = ApplyRowFilters(BuildRows(vouchers, x => x.PaymentStatus), search, sortBy, sortOrder);
            ViewBag.Monthly = ApplyRowFilters(BuildRows(vouchers, x => $"{x.VoucherDate:yyyy-MM}"), search, sortBy, sortOrder);
            ViewBag.Capital = ApplyRowFilters(
                BuildRows(
                    vouchers.Where(x => x.Category != null && x.Category.IsCapitalExpense),
                    x => x.Category?.CategoryName ?? "Capital"),
                search,
                sortBy,
                sortOrder);

            return View();
        }

        private static List<PurchaseReportRow> BuildRows(
            IEnumerable<ExpenseVoucherModel> vouchers,
            Func<ExpenseVoucherModel, string> groupSelector)
        {
            return vouchers
                .GroupBy(groupSelector)
                .Select(x => new PurchaseReportRow
                {
                    Label = x.Key,
                    Count = x.Count(),
                    TaxableAmount = x.Sum(v => v.TaxableAmount > 0 ? v.TaxableAmount : v.Amount),
                    GstAmount = x.Sum(v => v.TotalGSTAmount),
                    TotalAmount = x.Sum(v => v.TotalAmount),
                    PaidAmount = x.Sum(v => v.PaidAmount),
                    BalanceAmount = x.Sum(v => v.BalanceAmount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();
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
                    : rows.OrderBy(x => x.TotalAmount).ToList(),
            };
        }
    }

    public class PurchaseReportRow
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal GstAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        #endregion
    }
}
