using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class NoticeController : Controller
    {
        private readonly INoticeRepository _noticeRepository;
        private readonly IFileStorageService _fileStorageService;

        private const string DocumentFolder = "NoticeDocuments";

        public NoticeController(
            INoticeRepository noticeRepository,
            IFileStorageService fileStorageService)
        {
            _noticeRepository = noticeRepository;
            _fileStorageService = fileStorageService;
        }

        #region Index

        public async Task<IActionResult> Index(
            string? department,
            string? status,
            string? search,
            DateTime? fromDate,
            DateTime? toDate,
            string sortBy = "NoticeDate",
            string sortOrder = "desc",
            int page = 1)
        {
            var notices = await _noticeRepository.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(department))
            {
                notices = notices
                    .Where(x => x.Department == department)
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                notices = notices
                    .Where(x => x.Status == status)
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim().ToLower();

                notices = notices
                    .Where(x =>
                        (!string.IsNullOrWhiteSpace(x.NoticeNumber) &&
                            x.NoticeNumber.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.Subject) &&
                            x.Subject.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.Description) &&
                            x.Description.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.Department) &&
                            x.Department.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.Remarks) &&
                            x.Remarks.ToLower().Contains(keyword)))
                    .ToList();
            }

            if (fromDate.HasValue)
            {
                notices = notices
                    .Where(x => x.NoticeDate.Date >= fromDate.Value.Date)
                    .ToList();
            }

            if (toDate.HasValue)
            {
                notices = notices
                    .Where(x => x.NoticeDate.Date <= toDate.Value.Date)
                    .ToList();
            }

            bool desc =
                string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            notices = sortBy switch
            {
                "NoticeNo" => desc
                    ? notices.OrderByDescending(x => x.NoticeNumber).ToList()
                    : notices.OrderBy(x => x.NoticeNumber).ToList(),

                "Department" => desc
                    ? notices.OrderByDescending(x => x.Department).ToList()
                    : notices.OrderBy(x => x.Department).ToList(),

                "Subject" => desc
                    ? notices.OrderByDescending(x => x.Subject).ToList()
                    : notices.OrderBy(x => x.Subject).ToList(),

                "DueDate" => desc
                    ? notices.OrderByDescending(x => x.DueDate).ToList()
                    : notices.OrderBy(x => x.DueDate).ToList(),

                "Status" => desc
                    ? notices.OrderByDescending(x => x.Status).ToList()
                    : notices.OrderBy(x => x.Status).ToList(),

                "Active" => desc
                    ? notices.OrderByDescending(x => x.IsActive).ToList()
                    : notices.OrderBy(x => x.IsActive).ToList(),

                _ => desc
                    ? notices.OrderByDescending(x => x.NoticeDate).ToList()
                    : notices.OrderBy(x => x.NoticeDate).ToList()
            };

            const int pageSize = 20;
            int totalRecords = notices.Count;

            notices = notices
                .Skip((Math.Max(page, 1) - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Department = department;
            ViewBag.Status = status;
            ViewBag.Search = search;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages =
                (int)Math.Ceiling(totalRecords / (double)pageSize);

            return View(notices);
        }

        public async Task<IActionResult> ExportCsv(
            string? department,
            string? status,
            string? search,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var notices = await _noticeRepository.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(department))
                notices = notices.Where(x => x.Department == department).ToList();

            if (!string.IsNullOrWhiteSpace(status))
                notices = notices.Where(x => x.Status == status).ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim().ToLower();
                notices = notices.Where(x =>
                    (!string.IsNullOrWhiteSpace(x.NoticeNumber) && x.NoticeNumber.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(x.Subject) && x.Subject.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(x.Description) && x.Description.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(x.Department) && x.Department.ToLower().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(x.Remarks) && x.Remarks.ToLower().Contains(keyword)))
                    .ToList();
            }

            if (fromDate.HasValue)
                notices = notices.Where(x => x.NoticeDate.Date >= fromDate.Value.Date).ToList();

            if (toDate.HasValue)
                notices = notices.Where(x => x.NoticeDate.Date <= toDate.Value.Date).ToList();

            var csv = new StringBuilder();
            csv.AppendLine("Notice No,Department,Subject,Notice Date,Received Date,Due Date,Status,Active");

            foreach (var item in notices.OrderByDescending(x => x.NoticeDate))
            {
                csv.AppendLine(string.Join(",",
                    Csv(item.NoticeNumber),
                    Csv(item.Department),
                    Csv(item.Subject),
                    Csv(item.NoticeDate.ToString("dd-MMM-yyyy")),
                    Csv(item.ReceivedDate.ToString("dd-MMM-yyyy")),
                    Csv(item.DueDate?.ToString("dd-MMM-yyyy")),
                    Csv(item.Status),
                    Csv(item.IsActive ? "Active" : "Archived")));
            }

            return File(
                Encoding.UTF8.GetBytes(csv.ToString()),
                "text/csv",
                $"statutory-notices-{DateTime.Now:yyyyMMddHHmmss}.csv");
        }

        #endregion

        #region Create

        [HttpGet]
        public IActionResult Create()
        {
            var model = new NoticeModel
            {
                NoticeDate = DateTime.Today,
                ReceivedDate = DateTime.Today,
                Status = FinancialConstants.NoticeStatus.Open,
                IsActive = true
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NoticeModel model)
        {
            ModelState.Remove(nameof(model.Documents));

            ValidateNotice(model);

            if (!ModelState.IsValid)
                return View(model);

            await _noticeRepository.AddAsync(model);
            await _noticeRepository.SaveAsync();

            TempData["Success"] = "Notice created successfully.";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Edit

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var notice = await _noticeRepository.GetByIdAsync(id);

            if (notice == null)
                return NotFound();

            return View(notice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(NoticeModel model)
        {
            ModelState.Remove(nameof(model.Documents));
            ValidateNotice(model);

            if (!ModelState.IsValid)
                return View(model);

            var existing = await _noticeRepository.GetByIdAsync(model.NoticeId);

            if (existing == null)
                return NotFound();

            existing.NoticeNumber = model.NoticeNumber;
            existing.Department = model.Department;
            existing.NoticeDate = model.NoticeDate;
            existing.ReceivedDate = model.ReceivedDate;
            existing.DueDate = model.DueDate;
            existing.Subject = model.Subject;
            existing.Description = model.Description;
            existing.Status = model.Status;
            existing.ReplyDate = model.ReplyDate;
            existing.ReplyDetails = model.ReplyDetails;
            existing.Remarks = model.Remarks;

            await _noticeRepository.UpdateAsync(existing);
            await _noticeRepository.SaveAsync();

            TempData["Success"] = "Notice updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Details

        public async Task<IActionResult> Details(int id)
        {
            var notice = await _noticeRepository.GetByIdAsync(id);

            if (notice == null)
                return NotFound();

            return View(notice);
        }

        #endregion

        #region Delete

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var notice = await _noticeRepository.GetByIdAsync(id);

            if (notice == null)
                return NotFound();

            return View(notice);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var notice = await _noticeRepository.GetByIdAsync(id);

            if (notice == null)
                return NotFound();

            notice.IsActive = !notice.IsActive;

            await _noticeRepository.UpdateAsync(notice);
            await _noticeRepository.SaveAsync();

            TempData["Success"] = notice.IsActive
                ? "Notice activated successfully."
                : "Notice archived successfully.";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Documents

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(
            int noticeId,
            string documentType,
            IFormFile file,
            string? remarks)
        {
            var notice = await _noticeRepository.GetByIdAsync(noticeId);

            if (notice == null)
                return NotFound();

            documentType = documentType?.Trim() ?? string.Empty;
             remarks = remarks?.Trim();
             
             if (string.IsNullOrWhiteSpace(documentType))
             {
                 TempData["Error"] = "Please select a document type.";
                 return RedirectToAction(nameof(Details), new { id = noticeId });
             }

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file.";
                return RedirectToAction(nameof(Details), new { id = noticeId });
            }

            var upload = await _fileStorageService.UploadAsync(file, DocumentFolder);

            if (!upload.Success)
            {
                TempData["Error"] = upload.ErrorMessage;
                return RedirectToAction(nameof(Details), new { id = noticeId });
            }

            var document = new NoticeDocumentModel
            {
                NoticeId = noticeId,
                DocumentType = documentType,
                FileName = upload.OriginalFileName,
                FilePath = upload.RelativePath,
                Remarks = remarks,
                UploadedByUserId = GetCurrentUserId(),
                IsActive = true
            };

            try
            {
                await _noticeRepository.AddDocumentAsync(document);
                await _noticeRepository.SaveAsync();
            }
            catch
            {
                await _fileStorageService.DeleteAsync(upload.RelativePath);
                TempData["Error"] = "Document could not be saved. Please try again.";
                return RedirectToAction(nameof(Details), new { id = noticeId });
            }

            TempData["Success"] = "Document uploaded successfully.";

            return RedirectToAction(nameof(Details), new { id = noticeId });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var document = await _noticeRepository.GetDocumentByIdAsync(id);

            if (document == null)
                return NotFound();

            if (!document.IsActive)
            {
                TempData["Error"] = "Archived documents cannot be downloaded.";
                return RedirectToAction(nameof(Details), new { id = document.NoticeId });
            }

            var fileBytes =
                await _fileStorageService.DownloadAsync(document.FilePath);

            if (fileBytes == null)
            {
                TempData["Error"] = "Document file was not found.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = document.NoticeId });
            }

            return File(
                fileBytes,
                GetContentType(document.FileName),
                Path.GetFileName(document.FileName));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int id, int noticeId)
        {
            var document = await _noticeRepository.GetDocumentByIdAsync(id);

            if (document == null)
                return NotFound();

            document.IsActive = !document.IsActive;
            await _noticeRepository.DeleteDocumentAsync(document);
            await _noticeRepository.SaveAsync();

            TempData["Success"] = document.IsActive
                ? "Document activated successfully."
                : "Document archived successfully.";

            return RedirectToAction(nameof(Details), new { id = document.NoticeId });
        }

        #endregion

        #region Helper
        private void ValidateNotice(NoticeModel model)
        {
            model.NoticeNumber = model.NoticeNumber?.Trim();
            model.Department = model.Department?.Trim() ?? string.Empty;
            model.Subject = model.Subject?.Trim() ?? string.Empty;
            model.Description = model.Description?.Trim();
            model.Status = model.Status?.Trim() ?? FinancialConstants.NoticeStatus.Open;
            model.ReplyDetails = model.ReplyDetails?.Trim();
            model.Remarks = model.Remarks?.Trim();

            if (model.ReceivedDate.Date < model.NoticeDate.Date)
            {
                ModelState.AddModelError(
                    nameof(model.ReceivedDate),
                    "Received date cannot be before notice date.");
            }

            if (model.DueDate.HasValue &&
                model.DueDate.Value.Date < model.ReceivedDate.Date)
            {
                ModelState.AddModelError(
                    nameof(model.DueDate),
                    "Due date cannot be before received date.");
            }

            if (model.DueDate.HasValue &&
                model.DueDate.Value.Date < model.NoticeDate.Date)
            {
                ModelState.AddModelError(
                    nameof(model.DueDate),
                    "Due date cannot be before notice date.");
            }

            if (model.Status == FinancialConstants.NoticeStatus.Replied &&
                !model.ReplyDate.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.ReplyDate),
                    "Reply date is required when status is Replied.");
            }

            if (model.Status == FinancialConstants.NoticeStatus.Closed &&
                !model.ReplyDate.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.ReplyDate),
                    "Reply date is required when closing a notice.");
            }
        }

        #endregion
        private static string GetContentType(string fileName)
        {
            return Path.GetExtension(fileName)
                .ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",

                ".doc" => "application/msword",

                ".docx" =>
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",

                ".xls" => "application/vnd.ms-excel",

                ".xlsx" =>
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                ".jpg" or ".jpeg" => "image/jpeg",

                ".png" => "image/png",

                _ => "application/octet-stream"
            };
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? string.Empty;
        }

        private static string Csv(string? value)
        {
            value ??= string.Empty;
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

    }

 }
