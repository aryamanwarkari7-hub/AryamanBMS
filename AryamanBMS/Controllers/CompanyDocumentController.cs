using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public partial class CompanyDocumentController : Controller
    {
        private readonly ICompanyDocumentRepository _documentRepository;
        private readonly ICompanyDocumentCategoryRepository _categoryRepository;
        private readonly IFileStorageService _fileStorage;
        public CompanyDocumentController(
            ICompanyDocumentRepository documentRepository,
            ICompanyDocumentCategoryRepository categoryRepository,
            IFileStorageService fileStorage)
        {
            _documentRepository = documentRepository;
            _categoryRepository = categoryRepository;
            _fileStorage = fileStorage;
        }

        #region Index

        public async Task<IActionResult> Index()
        {
            var documents =
                await _documentRepository.GetAllAsync();

            return View(documents);
        }

        #endregion

        #region Create

        public async Task<IActionResult> Create()
        {
            CompanyDocumentViewModel vm =
                new CompanyDocumentViewModel();

            await LoadCategories(vm);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CompanyDocumentViewModel vm)
        {
            await LoadCategories(vm);

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            if (vm.UploadFile == null)
            {
                ModelState.AddModelError(
                    nameof(vm.UploadFile),
                    "Please select a document.");

                return View(vm);
            }

            var category =
                await _categoryRepository.GetByIdAsync(
                    vm.Document.DocumentCategoryId);

            if (category == null)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid document category.");

                return View(vm);
            }

            var upload =
                await _fileStorage.UploadAsync(
                    vm.UploadFile,
                    $"CompanyDocuments/{category.CategoryName}");

            if (!upload.Success)
            {
                ModelState.AddModelError(
                    "",
                    upload.ErrorMessage);

                return View(vm);
            }

            vm.Document.FileName =
                upload.OriginalFileName;

            vm.Document.StoredFileName =
                upload.StoredFileName;

            vm.Document.FileExtension =
                upload.FileExtension;

            vm.Document.ContentType =
                upload.ContentType;

            vm.Document.FileSize =
                upload.FileSize;

            vm.Document.FilePath =
                upload.RelativePath;

            vm.Document.VersionNo = 1;

            vm.Document.CreatedOn =
                DateTime.Now;

            vm.Document.IsActive = true;
            vm.Document.UploadedByUserId = GetCurrentUserId();

            try
            {
                await _documentRepository.AddAsync(
                    vm.Document);

                await _documentRepository.SaveAsync();
            }
            catch
            {
                await _fileStorage.DeleteAsync(
                    upload.RelativePath);

                ModelState.AddModelError(
                    "",
                    "Document could not be saved. Please try again.");

                return View(vm);
            }

            TempData["Success"] =
                "Company document uploaded successfully.";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Edit

        public async Task<IActionResult> Edit(int id)
        {
            var document =
                await _documentRepository.GetByIdAsync(id);

            if (document == null)
                return NotFound();

            CompanyDocumentViewModel vm =
                new CompanyDocumentViewModel
                {
                    Document = document
                };

            await LoadCategories(vm);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            CompanyDocumentViewModel vm)
        {
            if (id != vm.Document.CompanyDocumentId)
                return NotFound();

            await LoadCategories(vm);

            if (!ModelState.IsValid)
                return View(vm);

            var document =
                await _documentRepository.GetByIdAsync(id);

            if (document == null)
                return NotFound();

            document.DocumentCategoryId =
                vm.Document.DocumentCategoryId;

            document.DocumentName =
                vm.Document.DocumentName;

            document.DocumentNumber =
                vm.Document.DocumentNumber;

            document.IssueDate =
                vm.Document.IssueDate;

            document.ExpiryDate =
                vm.Document.ExpiryDate;

            document.IssuedBy =
                vm.Document.IssuedBy;

            document.Remarks =
                vm.Document.Remarks;

            document.UpdatedOn =
                DateTime.Now;

            string oldFilePath = document.FilePath;
            string? newFilePath = null;

            if (vm.UploadFile != null)
            {
                var category =
                    await _categoryRepository.GetByIdAsync(
                        document.DocumentCategoryId);

                if (category == null)
                {
                    ModelState.AddModelError(
                        "",
                        "Invalid document category.");

                    return View(vm);
                }

                var upload =
                    await _fileStorage.UploadAsync(
                        vm.UploadFile,
                        $"CompanyDocuments/{category.CategoryName}");

                if (!upload.Success)
                {
                    ModelState.AddModelError(
                        "",
                        upload.ErrorMessage);

                    return View(vm);
                }

                newFilePath = upload.RelativePath;

                document.FileName =
                    upload.OriginalFileName;

                document.StoredFileName =
                    upload.StoredFileName;

                document.FileExtension =
                    upload.FileExtension;

                document.ContentType =
                    upload.ContentType;

                document.FileSize =
                    upload.FileSize;

                document.FilePath =
                    upload.RelativePath;

                document.VersionNo++;
            }

            try
            {
                await _documentRepository.UpdateAsync(
                    document);

                await _documentRepository.SaveAsync();
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(newFilePath))
                {
                    await _fileStorage.DeleteAsync(newFilePath);
                }

                ModelState.AddModelError(
                    "",
                    "Document could not be updated. Please try again.");

                return View(vm);
            }

            if (!string.IsNullOrWhiteSpace(newFilePath) &&
                !string.IsNullOrWhiteSpace(oldFilePath) &&
                !string.Equals(
                    oldFilePath,
                    newFilePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                await _fileStorage.DeleteAsync(oldFilePath);
            }

            TempData["Success"] =
                "Document updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Details

        public async Task<IActionResult> Details(int id)
        {
            var document =
                await _documentRepository.GetByIdAsync(id);

            if (document == null)
                return NotFound();

            return View(document);
        }

        #endregion

        #region Download

        public async Task<IActionResult> Download(int id)
        {
            var document =
                await _documentRepository.GetByIdAsync(id);

            if (document == null)
                return NotFound();

            if (!document.IsActive)
            {
                TempData["Error"] = "Archived documents cannot be downloaded.";
                return RedirectToAction(nameof(Index));
            }

            var fileBytes =
                await _fileStorage.DownloadAsync(
                    document.FilePath);

            if (fileBytes == null)
            {
                TempData["Error"] =
                    "File not found.";

                return RedirectToAction(nameof(Index));
            }

            return File(
                fileBytes,
                document.ContentType ??
                "application/octet-stream",
                document.FileName);
        }

        #endregion

        #region Delete

        public async Task<IActionResult> Delete(int id)
        {
            var document =
                await _documentRepository.GetByIdAsync(id);

            if (document == null)
                return NotFound();

            return View(document);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var document =
                await _documentRepository.GetByIdAsync(id);

            if (document == null)
                return NotFound();

            document.IsActive = !document.IsActive;
            document.UpdatedOn = DateTime.Now;

            await _documentRepository.UpdateAsync(document);
            await _documentRepository.SaveAsync();

            TempData["Success"] = document.IsActive
                ? "Document activated successfully."
                : "Document archived successfully.";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Helpers

        private async Task LoadCategories(
            CompanyDocumentViewModel vm)
        {
            var categories =
                await _categoryRepository.GetAllAsync();

            vm.Categories =
                categories
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new SelectListItem
                {
                    Value =
                        x.DocumentCategoryId.ToString(),

                    Text =
                        x.CategoryName
                });
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? string.Empty;
        }

        #endregion
    }
}
