using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Business.Services;

public class ExpenseVoucherTrackerService : IExpenseVoucherTrackerService
{
    private const int PageSize = 20;

    private readonly IExpenseVoucherRepository _voucherRepository;

    public ExpenseVoucherTrackerService(IExpenseVoucherRepository voucherRepository)
    {
        _voucherRepository = voucherRepository;
    }

    public async Task<ExpenseVoucherTrackerData> GetTrackerAsync(
        string? status,
        int? categoryId,
        string? search,
        DateTime? fromDate,
        DateTime? toDate,
        string sortBy,
        string sortOrder,
        int page,
        string currentUserId,
        bool restrictToCurrentUser
    )
    {
        var vouchers = await FilterAsync(
            status,
            categoryId,
            search,
            fromDate,
            toDate,
            currentUserId,
            restrictToCurrentUser
        );

        bool descending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

        vouchers = sortBy switch
        {
            "VoucherNo" => descending
                ? vouchers.OrderByDescending(x => x.VoucherNumber).ToList()
                : vouchers.OrderBy(x => x.VoucherNumber).ToList(),
            "Category" => descending
                ? vouchers.OrderByDescending(x => x.Category?.CategoryName).ToList()
                : vouchers.OrderBy(x => x.Category?.CategoryName).ToList(),
            "Vendor" => descending
                ? vouchers.OrderByDescending(x => x.Vendor?.VendorName ?? x.VendorName).ToList()
                : vouchers.OrderBy(x => x.Vendor?.VendorName ?? x.VendorName).ToList(),
            "Amount" => descending
                ? vouchers.OrderByDescending(x => x.Amount).ToList()
                : vouchers.OrderBy(x => x.Amount).ToList(),
            "TotalAmount" => descending
                ? vouchers.OrderByDescending(x => x.TotalAmount).ToList()
                : vouchers.OrderBy(x => x.TotalAmount).ToList(),
            "Status" => descending
                ? vouchers.OrderByDescending(x => x.Status).ToList()
                : vouchers.OrderBy(x => x.Status).ToList(),
            "PaymentStatus" => descending
                ? vouchers.OrderByDescending(x => x.PaymentStatus).ToList()
                : vouchers.OrderBy(x => x.PaymentStatus).ToList(),
            _ => descending
                ? vouchers.OrderByDescending(x => x.VoucherDate).ToList()
                : vouchers.OrderBy(x => x.VoucherDate).ToList(),
        };

        int totalRecords = vouchers.Count;

        return new ExpenseVoucherTrackerData
        {
            Vouchers = vouchers.Skip((Math.Max(page, 1) - 1) * PageSize).Take(PageSize).ToList(),
            TotalPages = (int)Math.Ceiling(totalRecords / (double)PageSize),
        };
    }

    public async Task<List<ExpenseVoucherModel>> GetForExportAsync(
        string? status,
        int? categoryId,
        string? search,
        DateTime? fromDate,
        DateTime? toDate,
        string currentUserId,
        bool restrictToCurrentUser
    )
    {
        return await FilterAsync(
            status,
            categoryId,
            search,
            fromDate,
            toDate,
            currentUserId,
            restrictToCurrentUser
        );
    }

    private async Task<List<ExpenseVoucherModel>> FilterAsync(
        string? status,
        int? categoryId,
        string? search,
        DateTime? fromDate,
        DateTime? toDate,
        string currentUserId,
        bool restrictToCurrentUser
    )
    {
        var vouchers = (await _voucherRepository.GetAllAsync()).ToList();

        if (restrictToCurrentUser)
        {
            vouchers = vouchers.Where(x => x.CreatedByUserId == currentUserId).ToList();
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            vouchers = vouchers.Where(x => x.Status == status).ToList();
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            vouchers = vouchers.Where(x => x.ExpenseCategoryId == categoryId.Value).ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string keyword = search.Trim().ToLower();

            vouchers = vouchers
                .Where(x =>
                    (
                        !string.IsNullOrWhiteSpace(x.VoucherNumber)
                        && x.VoucherNumber.ToLower().Contains(keyword)
                    )
                    || (
                        !string.IsNullOrWhiteSpace(x.VendorName)
                        && x.VendorName.ToLower().Contains(keyword)
                    )
                    || (
                        x.Vendor != null
                        && !string.IsNullOrWhiteSpace(x.Vendor.VendorName)
                        && x.Vendor.VendorName.ToLower().Contains(keyword)
                    )
                    || (
                        x.Category != null
                        && !string.IsNullOrWhiteSpace(x.Category.CategoryName)
                        && x.Category.CategoryName.ToLower().Contains(keyword)
                    )
                    || (
                        !string.IsNullOrWhiteSpace(x.InvoiceNumber)
                        && x.InvoiceNumber.ToLower().Contains(keyword)
                    )
                    || (
                        !string.IsNullOrWhiteSpace(x.Description)
                        && x.Description.ToLower().Contains(keyword)
                    )
                )
                .ToList();
        }

        if (fromDate.HasValue)
        {
            vouchers = vouchers.Where(x => x.VoucherDate.Date >= fromDate.Value.Date).ToList();
        }

        if (toDate.HasValue)
        {
            vouchers = vouchers.Where(x => x.VoucherDate.Date <= toDate.Value.Date).ToList();
        }

        return vouchers;
    }
}
