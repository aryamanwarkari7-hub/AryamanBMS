using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class FinancialAuditDocumentController : Controller
    {
        private readonly IFinancialAuditDocumentRepository _repository;
        private readonly IFileStorageService _fileStorage;

        public FinancialAuditDocumentController(
            IFinancialAuditDocumentRepository repository,
            IFileStorageService fileStorage)
        {
            _repository = repository;
            _fileStorage = fileStorage;
        }

        #region Index

        public async Task<IActionResult> Index(string? financialYear, string? category)
        {
            List<FinancialAuditDocumentModel> documents;

            if (!string.IsNullOrEmpty(financialYear))
                documents = await _repository.GetByFinancialYearAsync(financialYear);
            else if (!string.IsNullOrEmpty(category))
                documents = await _repository.GetByCategoryAsync(category);
            else
                documents = await _repository.GetAllAsync();

            ViewBag.FilterFinancialYear = financialYear;
            ViewBag.FilterCategory = category;

            return View(documents);
        }

        #endregion

        #region Create

        public IActionResult Create()
        {
            return View(new FinancialAuditDocumentModel());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FinancialAuditDocumentModel model, IFormFile uploadFile)
        {
            // FileName/FilePath aren't supplied by the form — they're set after upload.
            // Remove them from validation so [Required] doesn't fail before we set them.
            ModelState.Remove(nameof(model.FileName));
            ModelState.Remove(nameof(model.FilePath));

            if (uploadFile == null)
            {
                ModelState.AddModelError(nameof(uploadFile), "Please select a file to upload.");
                return View(model);
            }

            if (!ModelState.IsValid)
                return View(model);

            var upload = await _fileStorage.UploadAsync(uploadFile, "FinancialAuditDocuments");
            if (!upload.Success)
            {
                ModelState.AddModelError("", upload.ErrorMessage);
                return View(model);
            }

            model.FileName = upload.OriginalFileName;
            model.FilePath = upload.RelativePath;

            await _repository.AddAsync(model);
            await _repository.SaveAsync();

            TempData["Success"] = "Document uploaded successfully.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Edit

        public async Task<IActionResult> Edit(int id)
        {
            var document = await _repository.GetByIdAsync(id);
            if (document == null) return NotFound();

            return View(document);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FinancialAuditDocumentModel model, IFormFile? uploadFile)
        {
            if (id != model.FinancialAuditDocumentId) return NotFound();

            if (!ModelState.IsValid)
                return View(model);

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.DocumentCategory = model.DocumentCategory;
            existing.FinancialYear = model.FinancialYear;
            existing.Remarks = model.Remarks;

            if (uploadFile != null)
            {
                var upload = await _fileStorage.UploadAsync(uploadFile, "FinancialAuditDocuments");
                if (!upload.Success)
                {
                    ModelState.AddModelError("", upload.ErrorMessage);
                    return View(model);
                }

                // Upload succeeded first — safe to delete old file now
                await _fileStorage.DeleteAsync(existing.FilePath);

                existing.FileName = upload.OriginalFileName;
                existing.FilePath = upload.RelativePath;
            }

            await _repository.UpdateAsync(existing);
            await _repository.SaveAsync();

            TempData["Success"] = "Document updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Details

        public async Task<IActionResult> Details(int id)
        {
            var document = await _repository.GetByIdAsync(id);
            if (document == null) return NotFound();

            return View(document);
        }

        #endregion

        #region Download

        public async Task<IActionResult> Download(int id)
        {
            var document = await _repository.GetByIdAsync(id);
            if (document == null) return NotFound();

            var bytes = await _fileStorage.DownloadAsync(document.FilePath);
            if (bytes == null)
            {
                TempData["Error"] = "File not found on disk.";
                return RedirectToAction(nameof(Index));
            }

            return File(bytes, "application/octet-stream", document.FileName);
        }

        #endregion

        #region Delete

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var document = await _repository.GetByIdAsync(id);
            if (document == null) return NotFound();

            await _fileStorage.DeleteAsync(document.FilePath);
            await _repository.DeleteAsync(document);
            await _repository.SaveAsync();

            TempData["Success"] = "Document deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        #endregion
    }
}
