using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
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

        public async Task<IActionResult> Index(string? department, string? status)
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

            ViewBag.Department = department;
            ViewBag.Status = status;

            return View(notices);
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
                Status = FinancialConstants.NoticeStatus.Open
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

            await _noticeRepository.UpdateAsync(model);
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
            await _noticeRepository.DeleteAsync(id);

            TempData["Success"] = "Notice deleted successfully.";

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
                Remarks = remarks
            };

            await _noticeRepository.AddDocumentAsync(document);
            await _noticeRepository.SaveAsync();

            TempData["Success"] = "Document uploaded successfully.";

            return RedirectToAction(nameof(Details), new { id = noticeId });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var document = await _noticeRepository.GetDocumentByIdAsync(id);

            if (document == null)
                return NotFound();

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

            await _fileStorageService.DeleteAsync(document.FilePath);

            await _noticeRepository.DeleteDocumentAsync(id);

            TempData["Success"] = "Document deleted successfully.";

            return RedirectToAction(nameof(Details), new { id = noticeId });
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

    }

 }