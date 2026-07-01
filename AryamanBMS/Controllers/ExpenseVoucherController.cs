using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance,Employee")]
    public class ExpenseVoucherController : Controller
    {
        private readonly IExpenseVoucherRepository _voucherRepository;
        private readonly IExpenseCategoryRepository _categoryRepository;
        private readonly UserManager<ApplicationUserModel> _userManager;

        public ExpenseVoucherController(
            IExpenseVoucherRepository voucherRepository,
            IExpenseCategoryRepository categoryRepository,
            UserManager<ApplicationUserModel> userManager)
        {
            _voucherRepository = voucherRepository;
            _categoryRepository = categoryRepository;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? status, int? categoryId, string? search)
        {
            var vouchers = await _voucherRepository.GetAllAsync();

            if (!IsFinanceUser())
            {
                int userId = GetCurrentUserId();

                vouchers = vouchers
                    .Where(x => x.CreatedByUserId == userId)
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                vouchers = vouchers
                    .Where(x => x.Status == status)
                    .ToList();
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                vouchers = vouchers
                    .Where(x => x.ExpenseCategoryId == categoryId.Value)
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim().ToLower();

                vouchers = vouchers
                    .Where(x =>
                        (!string.IsNullOrWhiteSpace(x.VoucherNumber) && x.VoucherNumber.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.VendorName) && x.VendorName.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.InvoiceNumber) && x.InvoiceNumber.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.Description) && x.Description.ToLower().Contains(keyword)))
                    .ToList();
            }

            ViewBag.Status = status;
            ViewBag.CategoryId = categoryId;
            ViewBag.Search = search;

            await LoadCategories();

            return View(vouchers);
        }

        public IActionResult Pending()
        {
            return RedirectToAction(nameof(Index), new
            {
                status = FinancialConstants.ExpenseVoucherStatus.Draft
            });
        }

        public async Task<IActionResult> Create()
        {
            var model = new ExpenseVoucherModel
            {
                VoucherDate = DateTime.Now,
                FinancialYear = GetCurrentFinancialYear()
            };

            await LoadCategories();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExpenseVoucherModel model)
        {
            await LoadCategories();

            // VoucherNumber is generated server-side below, not posted from the form
            ModelState.Remove(nameof(model.VoucherNumber));

            if (!ModelState.IsValid)
                return View(model);

            // Validate category exists
            var category = await _categoryRepository.GetByIdAsync(model.ExpenseCategoryId);
            if (category == null)
            {
                ModelState.AddModelError("ExpenseCategoryId", "Selected category does not exist.");
                return View(model);
            }

            if (model.Amount <= 0)
            {
                ModelState.AddModelError(nameof(model.Amount), "Amount should be greater than zero.");
                return View(model);
            }

            if (model.VoucherDate.Date > DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.VoucherDate), "Voucher date cannot be in the future.");
                return View(model);
            }

            // Generate voucher number
            model.VoucherNumber = await GenerateUniqueVoucherNumber(model.FinancialYear);

            // Set GST rates and calculate amounts
            if (model.GSTRate == 0)
                model.GSTRate = category.DefaultGSTRate;

            CalculateGSTAmounts(model);

            model.CreatedByUserId = GetCurrentUserId();
            model.Status = FinancialConstants.ExpenseVoucherStatus.Draft;

            await _voucherRepository.AddAsync(model);
            await _voucherRepository.SaveAsync();

            TempData["Success"] = $"Expense Voucher '{model.VoucherNumber}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        #region Edit
        public async Task<IActionResult> Edit(int id)
        {
            var voucher = await _voucherRepository.GetByIdAsync(id);
            if (voucher == null)
                return NotFound();

            // Only allow editing of draft vouchers
            if (!CanModifyDraftVoucher(voucher))
            {
                TempData["Error"] = "Only draft expense vouchers can be edited.";
                return RedirectToAction(nameof(Index));
            }

            await LoadCategories();
            return View(voucher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ExpenseVoucherModel model)
        {
            await LoadCategories();

            if (!ModelState.IsValid)
                return View(model);

            var existing = await _voucherRepository.GetByIdAsync(model.ExpenseVoucherId);
            if (existing == null)
                return NotFound();

            if (!CanModifyDraftVoucher(existing))
            {
                TempData["Error"] = "Only accessible draft expense vouchers can be edited.";
                return RedirectToAction(nameof(Index));
            }

            // Only allow editing of draft vouchers
            if (existing.Status != FinancialConstants.ExpenseVoucherStatus.Draft)
            {
                TempData["Error"] = "Only draft expense vouchers can be edited.";
                return RedirectToAction(nameof(Index));
            }

            // Validate category exists
            var category = await _categoryRepository.GetByIdAsync(model.ExpenseCategoryId);
            if (category == null)
            {
                ModelState.AddModelError("ExpenseCategoryId", "Selected category does not exist.");
                return View(model);
            }

            // Update fields
            existing.ExpenseCategoryId = model.ExpenseCategoryId;
            existing.VoucherDate = model.VoucherDate;
            existing.Description = model.Description;
            existing.Amount = model.Amount;
            existing.GSTRate = model.GSTRate > 0 ? model.GSTRate : category.DefaultGSTRate;
            existing.VendorName = model.VendorName;
            existing.VendorGSTIN = model.VendorGSTIN;
            existing.InvoiceNumber = model.InvoiceNumber;
            existing.ITCEligible = model.ITCEligible;
            existing.Remarks = model.Remarks;

            CalculateGSTAmounts(existing);

            await _voucherRepository.UpdateAsync(existing);
            await _voucherRepository.SaveAsync();

            TempData["Success"] = $"Expense Voucher '{existing.VoucherNumber}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        public async Task<IActionResult> Details(int id)
        {
            var voucher = await _voucherRepository.GetByIdAsync(id);
            if (voucher == null)
                return NotFound();

            if (!CanAccessVoucher(voucher))
            {
                return Forbid();
            }

            return View(voucher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {

            if (!IsFinanceUser())
            {
                return Forbid();
            } 
            var voucher = await _voucherRepository.GetByIdAsync(id);
            if (voucher == null)
                return NotFound();

            if (voucher.Status != FinancialConstants.ExpenseVoucherStatus.Draft)
            {
                TempData["Error"] = "Only draft expense vouchers can be approved.";
                return RedirectToAction(nameof(Index));
            }

            var userId = GetCurrentUserId();
            await _voucherRepository.ApproveAsync(id, userId);
            await _voucherRepository.SaveAsync();

            TempData["Success"] = $"Expense Voucher '{voucher.VoucherNumber}' approved successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {

            if (!IsFinanceUser())
            {
                return Forbid();
            }

            var voucher = await _voucherRepository.GetByIdAsync(id);
            if (voucher == null)
                return NotFound();

            if (voucher.Status != FinancialConstants.ExpenseVoucherStatus.Draft)
            {
                TempData["Error"] = "Only draft expense vouchers can be rejected.";
                return RedirectToAction(nameof(Index));
            }

            await _voucherRepository.RejectAsync(id);
            await _voucherRepository.SaveAsync();

            TempData["Success"] = $"Expense Voucher '{voucher.VoucherNumber}' rejected.";
            return RedirectToAction(nameof(Index));
        }

        #region Delete
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var voucher = await _voucherRepository.GetByIdAsync(id);

            if (voucher == null)
            {
                return NotFound();
            }

            if (!CanModifyDraftVoucher(voucher))
            {
                TempData["Error"] = "Only accessible draft expense vouchers can be deleted.";
                return RedirectToAction(nameof(Index));
            }

            return View(voucher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var voucher = await _voucherRepository.GetByIdAsync(id);

            if (voucher == null)
            {
                return NotFound();
            }

            if (!CanModifyDraftVoucher(voucher))
            {
                TempData["Error"] = "Only accessible draft expense vouchers can be deleted.";
                return RedirectToAction(nameof(Index));
            }

            await _voucherRepository.SoftDeleteAsync(id);
            await _voucherRepository.SaveAsync();

            TempData["Success"] = $"Expense Voucher '{voucher.VoucherNumber}' deleted.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        // Helper Methods
        private bool IsFinanceUser()
        {
            return User.IsInRole("Admin") || User.IsInRole("Finance");
        }

        private bool CanAccessVoucher(ExpenseVoucherModel voucher)
        {
            if (IsFinanceUser())
            {
                return true;
            }

            return voucher.CreatedByUserId == GetCurrentUserId();
        }

        private bool CanModifyDraftVoucher(ExpenseVoucherModel voucher)
        {
            return voucher.Status == FinancialConstants.ExpenseVoucherStatus.Draft &&
                   CanAccessVoucher(voucher);
        }
        private async Task LoadCategories()
        {
            ViewBag.Categories = await _categoryRepository.GetAllActiveAsync();
        }

        private string GetCurrentFinancialYear()
        {
            var today = DateTime.Now;
            int fyStart = today.Month >= 4 ? today.Year : today.Year - 1;
            int fyEnd = fyStart + 1;
            return $"{fyStart}-{fyEnd.ToString().Substring(2)}";
        }

        private async Task<string> GenerateVoucherNumber(string financialYear)
        {
            var sequence = await _voucherRepository.GetNextVoucherSequenceAsync(financialYear);
            return $"{FinancialConstants.ExpenseVoucherPrefix}-{financialYear}-{sequence.ToString("D4")}";
        }

        private async Task<string> GenerateUniqueVoucherNumber(string financialYear)
        {
            string voucherNumber;
            int attempts = 0;

            do
            {
                voucherNumber = await GenerateVoucherNumber(financialYear);
                attempts++;

                if (attempts > 10)
                {
                    throw new InvalidOperationException(
                        "Failed to generate unique voucher number. Please check financial sequence.");
                }
            }
            while (await _voucherRepository.VoucherNumberExistsAsync(voucherNumber));

            return voucherNumber;
        }

        private int GetCurrentUserId()
        {
            // This assumes ApplicationUserModel has an Id field. Adjust if needed.
            return int.TryParse(_userManager.GetUserId(User), out int userId) ? userId : 0;
        }

        private void CalculateGSTAmounts(ExpenseVoucherModel model)
        {
            if (model.GSTRate == 0)
            {
                model.CGSTAmount = 0;
                model.SGSTAmount = 0;
                model.IGSTAmount = 0;
                model.TotalGSTAmount = 0;
                model.TotalAmount = model.Amount;
            }
            else
            {
                decimal gstAmount = (model.Amount * model.GSTRate) / 100;

                model.CGSTAmount = gstAmount / 2;
                model.SGSTAmount = gstAmount / 2;
                model.IGSTAmount = 0;

                model.TotalGSTAmount = gstAmount;
                model.TotalAmount = model.Amount + gstAmount;
            }
        }
    }
}