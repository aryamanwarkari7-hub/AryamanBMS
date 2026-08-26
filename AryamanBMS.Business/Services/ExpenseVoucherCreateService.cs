using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using System.Text.RegularExpressions;

namespace AryamanBMS.Business.Services;

public class ExpenseVoucherCreateService : IExpenseVoucherCreateService
{
    private static readonly decimal[] AllowedGstRates = [0m, 5m, 12m, 18m, 28m];
    private static readonly Regex GstinRegex = new(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly IExpenseVoucherRepository _voucherRepository;
    private readonly IExpenseCategoryRepository _categoryRepository;
    private readonly IVendorRepository _vendorRepository;
    private readonly IGstSnapshotRepository _gstSnapshotRepository;

    public ExpenseVoucherCreateService(IExpenseVoucherRepository voucherRepository, IExpenseCategoryRepository categoryRepository, IVendorRepository vendorRepository, IGstSnapshotRepository gstSnapshotRepository)
    {
        _voucherRepository = voucherRepository;
        _categoryRepository = categoryRepository;
        _vendorRepository = vendorRepository;
        _gstSnapshotRepository = gstSnapshotRepository;
    }

    public async Task<ExpenseVoucherCreateValidationData> ValidateAsync(ExpenseVoucherModel voucher)
    {
        Normalize(voucher);
        var category = await _categoryRepository.GetByIdAsync(voucher.ExpenseCategoryId);
        var errors = new Dictionary<string, string>();
        if (category == null) errors[nameof(voucher.ExpenseCategoryId)] = "Selected category does not exist.";
        else if (voucher.GSTRate == 0 && category.DefaultGSTRate > 0) voucher.GSTRate = category.DefaultGSTRate;

        foreach (var error in Validate(voucher)) errors[error.Key] = error.Value;

        var snapshot = await _gstSnapshotRepository.GetByMonthYearAsync(voucher.VoucherDate.Month, voucher.VoucherDate.Year);
        if (snapshot?.Status is FinancialConstants.GstSnapshotStatus.Filed or FinancialConstants.GstSnapshotStatus.Locked || snapshot?.IsFiledPeriodLocked == true)
            errors[nameof(voucher.VoucherDate)] = "This GST period is filed or locked. Reopen the GST period before changing expenses.";

        if (!string.IsNullOrWhiteSpace(voucher.InvoiceNumber) && await _voucherRepository.VendorInvoiceExistsAsync(voucher.VendorId, voucher.VendorGSTIN, voucher.InvoiceNumber, voucher.FinancialYear))
            errors[nameof(voucher.InvoiceNumber)] = "This vendor invoice number already exists for the vendor.";

        return new ExpenseVoucherCreateValidationData { Category = category, Errors = errors };
    }

    public async Task CreateAsync(ExpenseVoucherModel voucher, ExpenseCategoryModel? category, string userId, string financialYear)
    {
        CalculateGst(voucher);
        voucher.CreatedByUserId = userId;
        voucher.Status = FinancialConstants.ExpenseVoucherStatus.Draft;
        voucher.FinancialYear = financialYear;
        if (voucher.VendorId.HasValue)
        {
            var vendor = await _vendorRepository.GetActiveByIdAsync(voucher.VendorId.Value);
            if (vendor != null) { voucher.VendorName = vendor.VendorName; voucher.VendorGSTIN = vendor.GSTIN; voucher.VendorStateCode = vendor.StateCode; if (string.IsNullOrWhiteSpace(voucher.PlaceOfSupplyStateCode)) voucher.PlaceOfSupplyStateCode = vendor.StateCode; if (!voucher.IsGstStateOverride && !string.IsNullOrWhiteSpace(voucher.CompanyStateCode) && !string.IsNullOrWhiteSpace(voucher.PlaceOfSupplyStateCode)) voucher.IsInterState = voucher.CompanyStateCode != voucher.PlaceOfSupplyStateCode; }
        }
        if (category != null) { voucher.GLAccountCode = category.GLAccountCode; voucher.PayableGLAccountCode = category.PayableGLAccountCode; voucher.InputGSTGLAccountCode = category.InputGSTGLAccountCode; if (string.IsNullOrWhiteSpace(voucher.ExpenseClassification)) voucher.ExpenseClassification = category.ExpenseType; voucher.ITCStatus = voucher.ITCEligible ? voucher.ITCStatus : "Not Applicable"; }
        voucher.PaidAmount = Math.Round(voucher.PaidAmount, 2, MidpointRounding.AwayFromZero);
        voucher.BalanceAmount = Math.Max(voucher.TotalAmount - voucher.PaidAmount, 0);
        voucher.PaymentStatus = voucher.PaidAmount <= 0 ? FinancialConstants.PaymentStatus.Unpaid : voucher.BalanceAmount <= 0 ? FinancialConstants.PaymentStatus.Paid : FinancialConstants.PaymentStatus.PartiallyPaid;
        await _voucherRepository.CreateWithSequenceAsync(voucher);
    }

    private static Dictionary<string, string> Validate(ExpenseVoucherModel v)
    {
        var e = new Dictionary<string, string>();
        if (v.Amount <= 0) e[nameof(v.Amount)] = "Amount must be greater than zero.";
        if (v.VoucherDate.Date > DateTime.Today) e[nameof(v.VoucherDate)] = "Voucher date cannot be in the future.";
        if (!AllowedGstRates.Contains(v.GSTRate)) e[nameof(v.GSTRate)] = "GST rate must be 0%, 5%, 12%, 18% or 28%.";
        if (!string.IsNullOrWhiteSpace(v.VendorGSTIN) && !GstinRegex.IsMatch(v.VendorGSTIN)) e[nameof(v.VendorGSTIN)] = "Enter a valid 15-character GSTIN.";
        bool registered = v.ExpensePartyType == "Registered Vendor"; bool small = v.ExpensePartyType is "Unregistered Vendor" or "One-Time Vendor" or "Employee Reimbursement" or "Petty Cash";
        if (registered && !v.VendorId.HasValue) e[nameof(v.VendorId)] = "Vendor is required for registered vendor expenses.";
        if (small) { v.VendorId = null; v.VendorGSTIN = null; v.GSTRate = 0; v.ITCEligible = false; v.ITCStatus = "Not Applicable"; v.Gstr2BMatchStatus = "Not Applicable"; v.Gstr2BMatchedOn = null; v.Gstr2BMatchedByUserId = null; v.Gstr2BMismatchReason = null; v.ITCClaimMonth = null; v.ITCClaimYear = null; v.InputGSTGLAccountCode = null; if (string.IsNullOrWhiteSpace(v.VendorName) && string.IsNullOrWhiteSpace(v.BeneficiaryName)) e[nameof(v.VendorName)] = "Vendor or payee name is required."; }
        if (v.ITCEligible) { if (v.GSTRate <= 0) e[nameof(v.ITCEligible)] = "ITC cannot be claimed when GST rate is zero."; if (string.IsNullOrWhiteSpace(v.VendorGSTIN)) e[nameof(v.VendorGSTIN)] = "Vendor GSTIN is required when ITC is eligible."; if (string.IsNullOrWhiteSpace(v.InvoiceNumber)) e[nameof(v.InvoiceNumber)] = "Vendor invoice number is required when ITC is eligible."; }
        return e;
    }

    private static void Normalize(ExpenseVoucherModel v) { v.Description = v.Description?.Trim() ?? string.Empty; v.ExpensePartyType = string.IsNullOrWhiteSpace(v.ExpensePartyType) ? "Registered Vendor" : v.ExpensePartyType.Trim(); v.VendorName = NormalizeOptional(v.VendorName); v.VendorGSTIN = NormalizeOptional(v.VendorGSTIN, true); v.InvoiceNumber = NormalizeOptional(v.InvoiceNumber, true); v.Remarks = NormalizeOptional(v.Remarks); v.BusinessPurpose = NormalizeOptional(v.BusinessPurpose); v.BeneficiaryName = NormalizeOptional(v.BeneficiaryName); v.SupportingReference = NormalizeOptional(v.SupportingReference); }
    private static string? NormalizeOptional(string? value, bool uppercase = false) { if (string.IsNullOrWhiteSpace(value)) return null; value = value.Trim(); return uppercase ? value.ToUpperInvariant() : value; }
    private static void CalculateGst(ExpenseVoucherModel v) { v.Amount = Math.Round(v.Amount, 2, MidpointRounding.AwayFromZero); v.GSTRate = Math.Round(v.GSTRate, 2, MidpointRounding.AwayFromZero); v.TaxableAmount = v.Amount; if (v.GSTRate <= 0) { v.CGSTAmount = v.SGSTAmount = v.IGSTAmount = v.TotalGSTAmount = 0; v.TotalAmount = v.Amount; return; } var total = Math.Round(v.Amount * v.GSTRate / 100, 2, MidpointRounding.AwayFromZero); if (v.IsInterState) { v.CGSTAmount = v.SGSTAmount = 0; v.IGSTAmount = total; } else { var cgst = Math.Round(total / 2, 2, MidpointRounding.AwayFromZero); v.CGSTAmount = cgst; v.SGSTAmount = total - cgst; v.IGSTAmount = 0; } v.TotalGSTAmount = total; v.TotalAmount = Math.Round(v.Amount + total, 2, MidpointRounding.AwayFromZero); }
}
