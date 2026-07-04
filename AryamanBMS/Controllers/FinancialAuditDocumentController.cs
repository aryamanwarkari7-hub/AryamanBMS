using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class FinancialAuditDocumentController : Controller
    {
        private const string DocumentFolder = "FinancialAuditDocuments";

        private static readonly HashSet<string> AllowedCategories =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "BankStatement",
                "AuditReport",
                "CADocument",
                "CSDocument",
                "Other"
            };

        private static readonly Regex FinancialYearRegex =
            new(@"^\d{4}-\d{2}$", RegexOptions.Compiled);

        private readonly IFinancialAuditDocumentRepository _repository;
        private readonly IFileStorageService _fileStorage;

        public FinancialAuditDocumentController(
            IFinancialAuditDocumentRepository repository,
            IFileStorageService fileStorage)
        {
            _repository = repository;
            _fileStorage = fileStorage;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? financialYear,
            string? category)
        {
            var documents = await _repository.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(financialYear))
            {
                documents = documents
                    .Where(x => x.FinancialYear == financialYear)
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                documents = documents
                    .Where(x => x.DocumentCategory == category)
                    .ToList();
            }

            ViewBag.FilterFinancialYear = financialYear;
            ViewBag.FilterCategory = category;

            return View(documents);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new FinancialAuditDocumentModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            FinancialAuditDocumentModel model,
            IFormFile? uploadFile)
        {
            RemoveServerManagedFieldsFromModelState(model);
            Normalize(model);
            ValidateDocument(model);

            if (uploadFile == null || uploadFile.Length == 0)
            {
                ModelState.AddModelError(
                    nameof(uploadFile),
                    "Please select a file to upload.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var upload =
                await _fileStorage.UploadAsync(uploadFile!, DocumentFolder);

            if (!upload.Success)
            {
                ModelState.AddModelError(string.Empty, upload.ErrorMessage);
                return View(model);
            }

            if (await _repository.ActiveDuplicateExistsAsync(
                    model.DocumentCategory,
                    model.FinancialYear,
                    upload.OriginalFileName))
            {
                await _fileStorage.DeleteAsync(upload.RelativePath);

                ModelState.AddModelError(
                    nameof(model.FileName),
                    "An active document with the same category, financial year and file name already exists.");

                return View(model);
            }

            model.FileName = upload.OriginalFileName;
            model.FilePath = upload.RelativePath;
            model.UploadedByUserId = GetCurrentUserId();
            model.UploadedOn = DateTime.Now;
            model.IsActive = true;
            model.IsFinalized = false;
            model.FinalizedByUserId = null;
            model.FinalizedOn = null;

            try
            {
                await _repository.AddAsync(model);
                await _repository.SaveAsync();
            }
            catch
            {
                await _fileStorage.DeleteAsync(upload.RelativePath);

                TempData["Error"] =
                    "Document could not be saved. Please try again.";

                return View(model);
            }

            TempData["Success"] = "Document uploaded successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var document = await _repository.GetByIdAsync(id);

            if (document == null)
                return NotFound();

            if (document.IsFinalized)
            {
                TempData["Error"] =
                    "Finalized audit documents cannot be edited.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            return View(document);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            FinancialAuditDocumentModel model,
            IFormFile? uploadFile)
        {
            RemoveServerManagedFieldsFromModelState(model);

            if (id != model.FinancialAuditDocumentId)
                return NotFound();

            Normalize(model);
            ValidateDocument(model);

            if (!ModelState.IsValid)
                return View(model);

            var existing = await _repository.GetByIdAsync(id);

            if (existing == null)
                return NotFound();

            if (existing.IsFinalized)
            {
                TempData["Error"] =
                    "Finalized audit documents cannot be edited.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            string oldFilePath = existing.FilePath;
            string? newFilePath = null;

            try
            {
                existing.DocumentCategory = model.DocumentCategory;
                existing.FinancialYear = model.FinancialYear;
                existing.Remarks = model.Remarks;

                if (uploadFile != null && uploadFile.Length > 0)
                {
                    var upload =
                        await _fileStorage.UploadAsync(
                            uploadFile,
                            DocumentFolder);

                    if (!upload.Success)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            upload.ErrorMessage);

                        return View(model);
                    }

                    newFilePath = upload.RelativePath;

                    if (await _repository.ActiveDuplicateExistsAsync(
                            existing.DocumentCategory,
                            existing.FinancialYear,
                            upload.OriginalFileName,
                            existing.FinancialAuditDocumentId))
                    {
                        await _fileStorage.DeleteAsync(newFilePath);

                        ModelState.AddModelError(
                            nameof(model.FileName),
                            "An active document with the same category, financial year and file name already exists.");

                        return View(model);
                    }

                    existing.FileName = upload.OriginalFileName;
                    existing.FilePath = upload.RelativePath;
                }
                else if (await _repository.ActiveDuplicateExistsAsync(
                            existing.DocumentCategory,
                            existing.FinancialYear,
                            existing.FileName,
                            existing.FinancialAuditDocumentId))
                {
                    ModelState.AddModelError(
                        nameof(model.FileName),
                        "An active document with the same category, financial year and file name already exists.");

                    return View(model);
                }

                await _repository.UpdateAsync(existing);
                await _repository.SaveAsync();
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(newFilePath))
                {
                    await _fileStorage.DeleteAsync(newFilePath);
                }

                TempData["Error"] =
                    "Document could not be updated. Please try again.";

                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(newFilePath) &&
                !string.Equals(
                    oldFilePath,
                    newFilePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                await _fileStorage.DeleteAsync(oldFilePath);
            }

            TempData["Success"] = "Document updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var document = await _repository.GetByIdAsync(id);

            if (document == null)
                return NotFound();

            return View(document);
        }

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var document = await _repository.GetByIdAsync(id);

            if (document == null)
                return NotFound();

            if (!document.IsActive)
            {
                TempData["Error"] =
                    "Archived audit documents cannot be downloaded.";

                return RedirectToAction(nameof(Index));
            }

            var bytes =
                await _fileStorage.DownloadAsync(document.FilePath);

            if (bytes == null)
            {
                TempData["Error"] =
                    "Document file was not found.";

                return RedirectToAction(nameof(Index));
            }

            return File(
                bytes,
                GetContentType(document.FileName),
                Path.GetFileName(document.FileName));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Finalize(int id)
        {
            var document = await _repository.GetByIdAsync(id);

            if (document == null)
                return NotFound();

            if (!document.IsActive)
            {
                TempData["Error"] =
                    "Archived audit documents cannot be finalized.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            if (!document.IsFinalized)
            {
                document.IsFinalized = true;
                document.FinalizedByUserId = GetCurrentUserId();
                document.FinalizedOn = DateTime.Now;

                await _repository.UpdateAsync(document);
                await _repository.SaveAsync();
            }

            TempData["Success"] = "Audit document finalized successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var document = await _repository.GetByIdAsync(id);

            if (document == null)
                return NotFound();

            if (document.IsFinalized)
            {
                TempData["Error"] =
                    "Finalized audit documents cannot be archived or activated.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            document.IsActive = !document.IsActive;

            await _repository.UpdateAsync(document);
            await _repository.SaveAsync();

            TempData["Success"] = document.IsActive
                ? "Audit document activated successfully."
                : "Audit document archived successfully.";

            return RedirectToAction(nameof(Index));
        }

        private void ValidateDocument(
            FinancialAuditDocumentModel model)
        {
            if (!AllowedCategories.Contains(model.DocumentCategory))
            {
                ModelState.AddModelError(
                    nameof(model.DocumentCategory),
                    "Please select a valid document category.");
            }

            if (!FinancialYearRegex.IsMatch(model.FinancialYear))
            {
                ModelState.AddModelError(
                    nameof(model.FinancialYear),
                    "Financial year must be in YYYY-YY format.");
            }

            if (!string.IsNullOrWhiteSpace(model.Remarks) &&
                model.Remarks.Length > 500)
            {
                ModelState.AddModelError(
                    nameof(model.Remarks),
                    "Remarks cannot exceed 500 characters.");
            }
        }

        private static void Normalize(
            FinancialAuditDocumentModel model)
        {
            model.DocumentCategory =
                model.DocumentCategory?.Trim() ?? string.Empty;

            model.FinancialYear =
                model.FinancialYear?.Trim() ?? string.Empty;

            model.Remarks =
                string.IsNullOrWhiteSpace(model.Remarks)
                    ? null
                    : model.Remarks.Trim();
        }

        private void RemoveServerManagedFieldsFromModelState(
            FinancialAuditDocumentModel model)
        {
            ModelState.Remove(nameof(model.FileName));
            ModelState.Remove(nameof(model.FilePath));
            ModelState.Remove(nameof(model.UploadedByUserId));
            ModelState.Remove(nameof(model.UploadedOn));
            ModelState.Remove(nameof(model.IsActive));
            ModelState.Remove(nameof(model.IsFinalized));
            ModelState.Remove(nameof(model.FinalizedByUserId));
            ModelState.Remove(nameof(model.FinalizedOn));
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? string.Empty;
        }

        private static string GetContentType(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }
    }
}
