using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Master")]
    public class ExpenseCategoryController : Controller
    {
        #region Actions

        private readonly IExpenseCategoryRepository _categoryRepository;

        public ExpenseCategoryController(IExpenseCategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<IActionResult> Index(
          string? search,
          string sortBy = "CategoryName",
          string sortOrder = "asc")
        {
            var categories = await _categoryRepository.GetAllActiveAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim().ToLower();

                categories = categories
                    .Where(x =>
                        (!string.IsNullOrWhiteSpace(x.CategoryCode) &&
                            x.CategoryCode.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.CategoryName) &&
                            x.CategoryName.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.Description) &&
                            x.Description.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.GLAccountCode) &&
                            x.GLAccountCode.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.ExpenseType) &&
                            x.ExpenseType.ToLower().Contains(keyword)))
                    .ToList();
            }

            bool desc =
                string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            categories = sortBy switch
            {
                "CategoryCode" => desc
                    ? categories.OrderByDescending(x => x.CategoryCode).ToList()
                    : categories.OrderBy(x => x.CategoryCode).ToList(),

                "GSTRate" => desc
                    ? categories.OrderByDescending(x => x.DefaultGSTRate).ToList()
                    : categories.OrderBy(x => x.DefaultGSTRate).ToList(),

                "ITC" => desc
                    ? categories.OrderByDescending(x => x.ITCEligible).ToList()
                    : categories.OrderBy(x => x.ITCEligible).ToList(),

                "ExpenseType" => desc
                    ? categories.OrderByDescending(x => x.ExpenseType).ToList()
                    : categories.OrderBy(x => x.ExpenseType).ToList(),

                _ => desc
                    ? categories.OrderByDescending(x => x.CategoryName).ToList()
                    : categories.OrderBy(x => x.CategoryName).ToList()
            };

            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

            return View(categories);
        }

        public IActionResult Create()
        {
            return View(new ExpenseCategoryModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExpenseCategoryModel model)
        {

            model.CategoryCode =
                (model.CategoryCode ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

            model.CategoryName =
                (model.CategoryName ?? string.Empty).Trim();
            if (!ModelState.IsValid)
                return View(model);

            // Check if category code already exists
            if (await _categoryRepository.CategoryCodeExistsAsync(model.CategoryCode))
            {
                ModelState.AddModelError("CategoryCode", "This category code already exists.");
                return View(model);
            }

            if (model.DefaultGSTRate < 0 || model.DefaultGSTRate > 100)
            {
                ModelState.AddModelError(nameof(model.DefaultGSTRate), "GST rate must be between 0 and 100.");
                return View(model);
            }

            await _categoryRepository.AddAsync(model);
            await _categoryRepository.SaveAsync();

            TempData["Success"] = $"Expense Category '{model.CategoryName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                return NotFound();

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ExpenseCategoryModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check if category code already exists (excluding current record)
            if (await _categoryRepository.CategoryCodeExistsAsync(model.CategoryCode, model.ExpenseCategoryId))
            {
                ModelState.AddModelError("CategoryCode", "This category code already exists.");
                return View(model);
            }

            var existing = await _categoryRepository.GetByIdAsync(model.ExpenseCategoryId);
            if (existing == null)
                return NotFound();

            existing.CategoryCode = model.CategoryCode;
            existing.CategoryName = model.CategoryName;
            existing.Description = model.Description;
            existing.DefaultGSTRate = model.DefaultGSTRate;
            existing.ITCEligible = model.ITCEligible;
            existing.GLAccountCode = model.GLAccountCode;
            existing.ExpenseType = model.ExpenseType;
            existing.PayableGLAccountCode = model.PayableGLAccountCode;
            existing.InputGSTGLAccountCode = model.InputGSTGLAccountCode;
            existing.IsCapitalExpense = model.IsCapitalExpense;

            await _categoryRepository.UpdateAsync(existing);
            await _categoryRepository.SaveAsync();

            TempData["Success"] = $"Expense Category '{model.CategoryName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            await _categoryRepository.SoftDeleteAsync(id);
            await _categoryRepository.SaveAsync();

            TempData["Success"] = $"Expense Category '{category.CategoryName}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        #endregion
    }
}
