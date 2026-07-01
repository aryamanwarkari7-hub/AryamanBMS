using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class ExpenseCategoryController : Controller
    {
        private readonly IExpenseCategoryRepository _categoryRepository;

        public ExpenseCategoryController(IExpenseCategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var categories = await _categoryRepository.GetAllActiveAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim().ToLower();

                categories = categories
                    .Where(x =>
                        (!string.IsNullOrWhiteSpace(x.CategoryCode) && x.CategoryCode.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.CategoryName) && x.CategoryName.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.Description) && x.Description.ToLower().Contains(keyword)))
                    .ToList();
            }

            ViewBag.Search = search;

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

            model.CategoryCode = model.CategoryCode?.Trim().ToUpper();
            model.CategoryName = model.CategoryName?.Trim();
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
    }
}