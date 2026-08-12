using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FinancialAuditDocumentController : Controller
    {
        #region Actions

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
            string? category,
            string? search,
            string? status,
            DateTime? fromDate,
            DateTime? toDate,
            string sortBy = "UploadedOn",
            string sortOrder = "desc",
            int page = 1)
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

            if (!string.IsNullOrWhiteSpace(status))
            {
                documents = status switch
                {
                    "Active" => documents.Where(x => x.IsActive && !x.IsFinalized).ToList(),
                    "Archived" => documents.Where(x => !x.IsActive).ToList(),
                    "Finalized" => documents.Where(x => x.IsFinalized).ToList(),
                    _ => documents
                };
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim().ToLower();

                documents = documents
                    .Where(x =>
                        (!string.IsNullOrWhiteSpace(x.FileName) &&
                            x.FileName.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.Remarks) &&
                            x.Remarks.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.FinancialYear) &&
                            x.FinancialYear.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.DocumentCategory) &&
                            x.DocumentCategory.ToLower().Contains(keyword)))
                    .ToList();
            }

            if (fromDate.HasValue)
            {
                documents = documents
                    .Where(x => x.UploadedOn.Date >= fromDate.Value.Date)
                    .ToList();
            }

            if (toDate.HasValue)
            {
                documents = documents
                    .Where(x => x.UploadedOn.Date <= toDate.Value.Date)
                    .ToList();
            }

            bool desc =
                string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            documents = sortBy switch
            {
                "Category" => desc
                    ? documents.OrderByDescending(x => x.DocumentCategory).ToList()
                    : documents.OrderBy(x => x.DocumentCategory).ToList(),

                "FinancialYear" => desc
                    ? documents.OrderByDescending(x => x.FinancialYear).ToList()
                    : documents.OrderBy(x => x.FinancialYear).ToList(),

                "FileName" => desc
                    ? documents.OrderByDescending(x => x.FileName).ToList()
                    : documents.OrderBy(x => x.FileName).ToList(),

                "Status" => desc
                    ? documents.OrderByDescending(x => x.IsFinalized)
                        .ThenByDescending(x => x.IsActive)
                        .ToList()
                    : documents.OrderBy(x => x.IsFinalized)
                        .ThenBy(x => x.IsActive)
                        .ToList(),

                _ => desc
                    ? documents.OrderByDescending(x => x.UploadedOn).ToList()
                    : documents.OrderBy(x => x.UploadedOn).ToList()
            };

            const int pageSize = 20;
            int totalRecords = documents.Count;

            documents = documents
                .Skip((Math.Max(page, 1) - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.FilterFinancialYear = financialYear;
            ViewBag.FilterCategory = category;
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages =
                (int)Math.Ceiling(totalRecords / (double)pageSize);

            return View(documents);
        }

        public async Task<IActionResult> ExportExcel(
            string? financialYear,
            string? category,
            string? search,
            string? status,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var documents = await _repository.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(financialYear))
                documents = documents.Where(x => x.FinancialYear == financialYear).ToList();

            if (!string.IsNullOrWhiteSpace(category))
                documents = documents.Where(x => x.DocumentCategory == category).ToList();

            if (!string.IsNullOrWhiteSpace(status))
            {
                documents = status switch
                {
                    "Active" => documents.Where(x => x.IsActive && !x.IsFinalized).ToList(),
                    "Archived" => documents.Where(x => !x.IsActive).ToList(),
                    "Finalized" => documents.Where(x => x.IsFinalized).ToList(),
                    _ => documents
                };
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim().ToLower();
                documents = documents.Where(x =>
                    (!string.IsNullOrWhiteSpace(x.FileName) && x.FileName.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(x.Remarks) && x.Remarks.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(x.FinancialYear) && x.FinancialYear.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(x.DocumentCategory) && x.DocumentCategory.ToLower().Contains(keyword)))
                    .ToList();
            }

            if (fromDate.HasValue)
                documents = documents.Where(x => x.UploadedOn.Date >= fromDate.Value.Date).ToList();

            if (toDate.HasValue)
                documents = documents.Where(x => x.UploadedOn.Date <= toDate.Value.Date).ToList();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Audit Documents");

            worksheet.Cell(1, 1).Value = "Category";
            worksheet.Cell(1, 2).Value = "Financial Year";
            worksheet.Cell(1, 3).Value = "File Name";
            worksheet.Cell(1, 4).Value = "Remarks";
            worksheet.Cell(1, 5).Value = "Uploaded On";
            worksheet.Cell(1, 6).Value = "Status";

            int row = 2;

            foreach (var item in documents.OrderByDescending(x => x.UploadedOn))
            {
                string documentStatus = item.IsFinalized
                    ? "Finalized"
                    : item.IsActive
                        ? "Active"
                        : "Archived";

                worksheet.Cell(row, 1).Value = item.DocumentCategory;
                worksheet.Cell(row, 2).Value = item.FinancialYear;
                worksheet.Cell(row, 3).Value = item.FileName;
                worksheet.Cell(row, 4).Value = item.Remarks;
                worksheet.Cell(row, 5).Value = item.UploadedOn;
                worksheet.Cell(row, 5).Style.DateFormat.Format = "dd-MMM-yyyy hh:mm AM/PM";
                worksheet.Cell(row, 6).Value = documentStatus;

                row++;
            }

            var headerRange = worksheet.Range("A1:F1");
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"financial-audit-documents-{DateTime.Now:yyyyMMddHHmmss}.xlsx");
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

        private static string Csv(string? value)
        {
            value ??= string.Empty;
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        #endregion
    }
}
