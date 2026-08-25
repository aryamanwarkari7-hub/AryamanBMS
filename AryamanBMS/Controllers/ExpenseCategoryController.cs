using AryamanBMS.Business.Interfaces;
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

        private readonly IExpenseCategoryService _categoryService;

        public ExpenseCategoryController(IExpenseCategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        public async Task<IActionResult> Index(
          string? search,
          string sortBy = "CategoryName",
          string sortOrder = "asc")
        {
            var categories = await _categoryService.GetActiveAsync(
                search,
                sortBy,
                sortOrder);

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

            var errors = await _categoryService.ValidateForCreateAsync(model);
            AddValidationErrors(errors);

            if (!ModelState.IsValid)
                return View(model);

            await _categoryService.CreateAsync(model);

            TempData["Success"] = $"Expense Category '{model.CategoryName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var category = await _categoryService.GetActiveByIdAsync(id);
            if (category == null)
                return NotFound();

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ExpenseCategoryModel model)
        {
            var errors = await _categoryService.ValidateForUpdateAsync(model);
            AddValidationErrors(errors);

            if (!ModelState.IsValid)
                return View(model);

            var existing = await _categoryService.UpdateAsync(model);
            if (existing == null)
                return NotFound();

            TempData["Success"] = $"Expense Category '{model.CategoryName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _categoryService.GetActiveByIdAsync(id);

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
            var category = await _categoryService.DeleteAsync(id);

            if (category == null)
                return NotFound();

            TempData["Success"] = $"Expense Category '{category.CategoryName}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private void AddValidationErrors(
            IReadOnlyDictionary<string, string> errors)
        {
            foreach (var error in errors)
            {
                ModelState.AddModelError(error.Key, error.Value);
            }
        }
        #endregion
    }
}
