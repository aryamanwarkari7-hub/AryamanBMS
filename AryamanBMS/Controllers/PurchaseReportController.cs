using AryamanBMS.Data;
using AryamanBMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class PurchaseReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PurchaseReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string report = "VendorPayable")
        {
            var vouchers = await _context.ExpenseVouchers
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Include(x => x.Vendor)
                .Include(x => x.Category)
                .Include(x => x.Project)
                .Include(x => x.Department)
                .ToListAsync();

            ViewBag.Report = report;
            ViewBag.VendorPayable = vouchers
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
                .OrderByDescending(x => x.BalanceAmount)
                .ToList();

            ViewBag.CategoryWise = BuildRows(vouchers, x => x.Category?.CategoryName ?? "Unmapped");
            ViewBag.VendorWise = BuildRows(vouchers, x => x.Vendor?.VendorName ?? x.VendorName ?? "Unmapped");
            ViewBag.ProjectWise = BuildRows(vouchers, x => x.Project?.ProjectName ?? "Not Linked");
            ViewBag.DepartmentWise = BuildRows(vouchers, x => x.Department?.DepartmentName ?? "Not Linked");
            ViewBag.Reimbursements = BuildRows(
                vouchers.Where(x => x.IsEmployeeReimbursement),
                x => x.ReimbursementStatus);
            ViewBag.Itc = BuildRows(vouchers, x => x.ITCStatus);
            ViewBag.PaidUnpaid = BuildRows(vouchers, x => x.PaymentStatus);
            ViewBag.Monthly = BuildRows(
                vouchers,
                x => $"{x.VoucherDate:yyyy-MM}");
            ViewBag.Capital = BuildRows(
                vouchers.Where(x => x.Category != null && x.Category.IsCapitalExpense),
                x => x.Category?.CategoryName ?? "Capital");

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
    }
}
