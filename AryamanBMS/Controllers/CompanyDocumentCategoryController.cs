using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR,Master")]
    public class CompanyDocumentCategoryController : Controller
    {
        private readonly ICompanyDocumentCategoryRepository _repository;

        public CompanyDocumentCategoryController(
            ICompanyDocumentCategoryRepository repository)
        {
            _repository = repository;
        }

        #region Index

        public async Task<IActionResult> Index()
        {
            var categories = await _repository.GetAllAsync();

            return View(categories);
        }

        #endregion

        #region Get
        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var category = await _repository.GetByIdAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            return Json(new
            {
                documentCategoryId = category.DocumentCategoryId,
                categoryCode = category.CategoryCode,
                categoryName = category.CategoryName,
                description = category.Description,
                displayOrder = category.DisplayOrder,
                expiryReminderDays = category.ExpiryReminderDays,
                allowedExtensions = category.AllowedExtensions,
                maxFileSizeMB = category.MaxFileSizeMB,
                isMandatory = category.IsMandatory,
                hasExpiry = category.HasExpiry,
                requireDocumentNumber = category.RequireDocumentNumber,
                allowMultipleDocuments = category.AllowMultipleDocuments,
                isActive = category.IsActive
            });
        }

        #endregion

        #region Save

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(
            CompanyDocumentCategoryModel model)
        {
            model.CategoryCode =
                (model.CategoryCode ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

            model.CategoryName =
                (model.CategoryName ?? string.Empty).Trim();

            model.Description = model.Description?.Trim();
            model.AllowedExtensions = model.AllowedExtensions?.Trim().ToLower();

            if (!ModelState.IsValid)
            {
                TempData["Error"] =
                    "Please correct validation errors.";

                return RedirectToAction(nameof(Index));
            }

            if (model.DocumentCategoryId == 0)
            {
                model.IsActive = true;
                model.CreatedOn = DateTime.Now;

                await _repository.AddAsync(model);

                TempData["Success"] =
                    "Category created successfully.";
            }
            else
            {
                var existing =
                    await _repository.GetByIdAsync(
                        model.DocumentCategoryId);

                if (existing == null)
                {
                    TempData["Error"] =
                        "Category not found.";

                    return RedirectToAction(nameof(Index));
                }

                existing.CategoryCode =
                    model.CategoryCode;

                existing.CategoryName =
                    model.CategoryName;

                existing.Description =
                    model.Description;

                existing.DisplayOrder =
                    model.DisplayOrder;

                existing.IsMandatory =
                    model.IsMandatory;

                existing.HasExpiry =
                    model.HasExpiry;

                existing.RequireDocumentNumber =
                    model.RequireDocumentNumber;

                existing.AllowMultipleDocuments =
                    model.AllowMultipleDocuments;

                existing.ExpiryReminderDays =
                    model.ExpiryReminderDays;

                existing.AllowedExtensions =
                    model.AllowedExtensions;

                existing.MaxFileSizeMB =
                    model.MaxFileSizeMB;

                existing.IsActive =
                    model.IsActive;

                existing.UpdatedOn =
                    DateTime.Now;

                await _repository.UpdateAsync(existing);

                TempData["Success"] =
                    "Category updated successfully.";
            }

            await _repository.SaveAsync();

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Delete

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category =
                await _repository.GetByIdAsync(id);

            if (category == null)
            {
                TempData["Error"] =
                    "Category not found.";

                return RedirectToAction(nameof(Index));
            }

            category.IsActive = !category.IsActive;
            category.UpdatedOn = DateTime.Now;

            await _repository.UpdateAsync(category);
            await _repository.SaveAsync();

            TempData["Success"] = category.IsActive
                ? "Category activated successfully."
                : "Category archived successfully.";

            return RedirectToAction(nameof(Index));
        }

        #endregion

     }
}
