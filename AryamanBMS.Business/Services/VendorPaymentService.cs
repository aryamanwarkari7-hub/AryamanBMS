using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Business.Services;

public class VendorPaymentService : IVendorPaymentService
{
    private readonly IVendorPaymentRepository _repository;

    public VendorPaymentService(IVendorPaymentRepository repository) => _repository = repository;

    public async Task<List<VendorPaymentModel>> GetAllAsync(
        int? vendorId,
        string? search,
        DateTime? fromDate,
        DateTime? toDate,
        string sortBy,
        string sortOrder)
    {
        var payments = await _repository.GetActiveWithDetailsAsync();

        if (vendorId.HasValue)
        {
            payments = payments.Where(x => x.VendorId == vendorId.Value).ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            payments = payments.Where(x =>
                Contains(x.PaymentNo, keyword) ||
                Contains(x.Vendor?.VendorName, keyword) ||
                Contains(x.ExpenseVoucher?.VoucherNumber, keyword) ||
                Contains(x.PaymentMode, keyword) ||
                Contains(x.TransactionReference, keyword)).ToList();
        }

        if (fromDate.HasValue)
        {
            payments = payments.Where(x => x.PaymentDate.Date >= fromDate.Value.Date).ToList();
        }

        if (toDate.HasValue)
        {
            payments = payments.Where(x => x.PaymentDate.Date <= toDate.Value.Date).ToList();
        }

        var descending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy switch
        {
            "PaymentNo" => descending ? payments.OrderByDescending(x => x.PaymentNo).ToList() : payments.OrderBy(x => x.PaymentNo).ToList(),
            "Vendor" => descending ? payments.OrderByDescending(x => x.Vendor?.VendorName).ToList() : payments.OrderBy(x => x.Vendor?.VendorName).ToList(),
            "Voucher" => descending ? payments.OrderByDescending(x => x.ExpenseVoucher?.VoucherNumber).ToList() : payments.OrderBy(x => x.ExpenseVoucher?.VoucherNumber).ToList(),
            "Mode" => descending ? payments.OrderByDescending(x => x.PaymentMode).ToList() : payments.OrderBy(x => x.PaymentMode).ToList(),
            "Reference" => descending ? payments.OrderByDescending(x => x.TransactionReference).ToList() : payments.OrderBy(x => x.TransactionReference).ToList(),
            "Amount" => descending ? payments.OrderByDescending(x => x.AmountPaid).ToList() : payments.OrderBy(x => x.AmountPaid).ToList(),
            _ => descending ? payments.OrderByDescending(x => x.PaymentDate).ToList() : payments.OrderBy(x => x.PaymentDate).ToList()
        };
    }

    public Task<List<VendorModel>> GetActiveVendorsAsync() => _repository.GetActiveVendorsAsync();

    public Task<List<ExpenseVoucherModel>> GetPayablePostedVouchersAsync() =>
        _repository.GetPayablePostedVouchersAsync();

    public async Task<VendorPaymentModel> CreateDraftAsync(int? expenseVoucherId)
    {
        var payment = new VendorPaymentModel
        {
            PaymentDate = DateTime.Today,
            ExpenseVoucherId = expenseVoucherId ?? 0
        };

        if (!expenseVoucherId.HasValue)
        {
            return payment;
        }

        var voucher = await _repository.GetActiveVoucherWithPaymentsAsync(expenseVoucherId.Value);
        if (voucher == null)
        {
            return payment;
        }

        payment.VendorId = voucher.VendorId ?? 0;
        payment.AmountPaid = voucher.BalanceAmount > 0
            ? voucher.BalanceAmount
            : voucher.TotalAmount - voucher.PaidAmount;

        return payment;
    }

    public async Task<IReadOnlyDictionary<string, string>> ValidateAsync(VendorPaymentModel payment)
    {
        Normalize(payment);
        var errors = new Dictionary<string, string>();
        var voucher = await _repository.GetActiveVoucherWithPaymentsAsync(payment.ExpenseVoucherId);

        if (voucher == null)
        {
            errors[nameof(payment.ExpenseVoucherId)] = "Selected voucher does not exist.";
        }
        else
        {
            var paid = voucher.VendorPayments.Sum(x => x.AmountPaid);
            var balance = Math.Max(voucher.TotalAmount - paid, 0);

            if (voucher.Status != FinancialConstants.ExpenseVoucherStatus.Posted)
            {
                errors[nameof(payment.ExpenseVoucherId)] = "Only posted vouchers can be paid.";
            }

            if (payment.VendorId != voucher.VendorId)
            {
                errors[nameof(payment.VendorId)] = "Vendor must match the selected voucher.";
            }

            if (payment.AmountPaid <= 0 || payment.AmountPaid > balance)
            {
                errors[nameof(payment.AmountPaid)] = $"Payment cannot exceed unpaid balance {balance:N2}.";
            }
        }

        if (!string.IsNullOrWhiteSpace(payment.TransactionReference) &&
            await _repository.TransactionReferenceExistsAsync(payment.TransactionReference))
        {
            errors[nameof(payment.TransactionReference)] = "This transaction reference is already used.";
        }

        return errors;
    }

    public async Task<VendorPaymentModel> CreateAsync(
        VendorPaymentModel payment,
        string paidByUserId)
    {
        var voucher = await _repository.GetActiveVoucherWithPaymentsAsync(payment.ExpenseVoucherId)
            ?? throw new InvalidOperationException("Selected voucher does not exist.");

        var financialYear = GetCurrentFinancialYear();
        var paymentCount = await _repository.GetPaymentCountForFinancialYearAsync(financialYear);
        var now = DateTime.Now;

        payment.PaymentNo = $"VPAY-{financialYear}-{paymentCount + 1:0000}";
        payment.PaidByUserId = paidByUserId;
        payment.CreatedOn = now;
        payment.IsActive = true;

        voucher.PaidAmount = voucher.VendorPayments.Sum(x => x.AmountPaid) + payment.AmountPaid;
        voucher.BalanceAmount = Math.Max(voucher.TotalAmount - voucher.PaidAmount, 0);
        voucher.PaymentStatus = voucher.PaidAmount <= 0
            ? FinancialConstants.PaymentStatus.Unpaid
            : voucher.BalanceAmount <= 0
                ? FinancialConstants.PaymentStatus.Paid
                : FinancialConstants.PaymentStatus.PartiallyPaid;
        voucher.UpdatedOn = now;

        await _repository.SavePaymentAndVoucherAsync(payment, voucher);
        return payment;
    }

    private static bool Contains(string? value, string keyword) =>
        !string.IsNullOrWhiteSpace(value) && value.ToLower().Contains(keyword);

    private static void Normalize(VendorPaymentModel payment)
    {
        payment.TransactionReference = string.IsNullOrWhiteSpace(payment.TransactionReference)
            ? null
            : payment.TransactionReference.Trim();
        payment.Remarks = string.IsNullOrWhiteSpace(payment.Remarks)
            ? null
            : payment.Remarks.Trim();
    }

    private static string GetCurrentFinancialYear()
    {
        var today = DateTime.Now;
        var financialYearStart = today.Month >= 4 ? today.Year : today.Year - 1;
        return $"{financialYearStart}-{(financialYearStart + 1).ToString()[^2..]}";
    }
}
