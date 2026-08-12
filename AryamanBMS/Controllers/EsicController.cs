using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Identity;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EsicController : Controller
    {
        #region Actions

        private readonly IEsicRepository _esicRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly UserManager<ApplicationUserModel> _userManager;

        private const string DocumentFolder = "EsicDocuments";

        public EsicController(
           IEsicRepository esicRepository,
           IFileStorageService fileStorageService,
           UserManager<ApplicationUserModel> userManager)
        {
            _esicRepository = esicRepository;
            _fileStorageService = fileStorageService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var snapshots = await _esicRepository.GetAllSnapshotsAsync();
            return View(snapshots);
        }

        [HttpGet]
        public IActionResult Generate()
        {
            var model = new GenerateEsicSnapshotInput
            {
                Month = DateTime.Now.Month,
                Year = DateTime.Now.Year
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(GenerateEsicSnapshotInput input)
        {
            if (!ModelState.IsValid)
                return View(input);

            try
            {
                var snapshot = await _esicRepository.GenerateSnapshotAsync(input.Month, input.Year);

                TempData["Success"] = $"ESIC snapshot for {input.Month}/{input.Year} generated successfully.";
                return RedirectToAction(nameof(Details), new { id = snapshot.EsicSnapshotId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(input);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var snapshot = await _esicRepository.GetSnapshotByIdAsync(id);
            if (snapshot == null)
                return NotFound();

            return View(snapshot);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFiled(int id)
        {
            var snapshot = await _esicRepository.GetSnapshotByIdAsync(id);

            if (snapshot == null)
            {
                return NotFound();
            }

            if (snapshot.TotalPayable <= 0)
            {
                TempData["Error"] = "ESIC snapshot has no payable amount and cannot be marked as filed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (snapshot.Status == FinancialConstants.StatutoryStatus.Paid)
            {
                TempData["Error"] = "Paid ESIC snapshots cannot be moved back to filed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            string? userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] =
                    "Current user could not be identified.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            bool updated =
                await _esicRepository.MarkFiledAsync(
                    id,
                    userId);

            if (!updated)
            {
                TempData["Error"] =
                    "ESIC snapshot could not be marked as filed.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            TempData["Success"] = "ESIC snapshot marked as Filed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int id)
        {
            var snapshot = await _esicRepository.GetSnapshotByIdAsync(id);

            if (snapshot == null)
            {
                return NotFound();
            }

            if (snapshot.Status != FinancialConstants.StatutoryStatus.Filed)
            {
                TempData["Error"] = "ESIC snapshot must be marked as filed before it can be marked as paid.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!snapshot.Challans.Any())
            {
                TempData["Error"] = "Please record ESIC challan details before marking this snapshot as paid.";
                return RedirectToAction(nameof(Details), new { id });
            }

            decimal paidAmount = snapshot.Challans.Sum(x => x.AmountPaid);

            if (paidAmount < snapshot.TotalPayable)
            {
                TempData["Error"] = "Paid challan amount is less than ESIC payable amount.";
                return RedirectToAction(nameof(Details), new { id });
            }

            string? userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["Error"] =
                    "Current user could not be identified.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            bool updated =
                await _esicRepository.MarkPaidAsync(
                    id,
                    userId);

            if (!updated)
            {
                TempData["Error"] =
                    "ESIC snapshot could not be marked as paid.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            TempData["Success"] = "ESIC snapshot marked as Paid.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddChallan(EsicChallanModel model)
        {
            ModelState.Remove(nameof(model.Snapshot));

            model.ChallanNumber = model.ChallanNumber?.Trim();
            model.BankName = model.BankName?.Trim();
            model.PaymentMode = model.PaymentMode?.Trim();
            model.Remarks = model.Remarks?.Trim();

            if (model.AmountPaid <= 0)
            {
                ModelState.AddModelError(nameof(model.AmountPaid), "Amount paid must be greater than zero.");
            }

            if (!model.PaymentDate.HasValue)
            {
                ModelState.AddModelError(nameof(model.PaymentDate), "Payment date is required.");
            }
            else if (model.PaymentDate.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.PaymentDate), "Payment date cannot be in the future.");
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please correct the challan details and try again.";
                return RedirectToAction(nameof(Details), new { id = model.EsicSnapshotId });
            }

            var snapshot = await _esicRepository.GetSnapshotByIdAsync(model.EsicSnapshotId);
            if (snapshot == null)
                return NotFound();

            model.Status = FinancialConstants.StatutoryStatus.Paid;

            await _esicRepository.AddChallanAsync(model);
            await _esicRepository.SaveAsync();

            TempData["Success"] = "ESIC challan recorded successfully.";
            return RedirectToAction(nameof(Details), new { id = model.EsicSnapshotId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(int snapshotId, string documentType, IFormFile file, string? remarks)
        {
            var snapshot = await _esicRepository.GetSnapshotByIdAsync(snapshotId);
            if (snapshot == null)
                return NotFound();

            documentType = documentType?.Trim() ?? string.Empty;
            remarks = remarks?.Trim();

            if (string.IsNullOrWhiteSpace(documentType))
            {
                TempData["Error"] = "Please select a document type.";
                return RedirectToAction(nameof(Details), new { id = snapshotId });
            }

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction(nameof(Details), new { id = snapshotId });
            }

            var uploadResult = await _fileStorageService.UploadAsync(file, DocumentFolder);

            if (!uploadResult.Success)
            {
                TempData["Error"] = uploadResult.ErrorMessage;
                return RedirectToAction(nameof(Details), new { id = snapshotId });
            }

            var document = new EsicDocumentModel
            {
                EsicSnapshotId = snapshotId,
                DocumentType = documentType,
                FileName = uploadResult.OriginalFileName,
                FilePath = uploadResult.RelativePath,
                Remarks = remarks,
                UploadedByUserId = _userManager.GetUserId(User),
                IsActive = true
            };

            try
            {
                await _esicRepository.AddDocumentAsync(document);
                await _esicRepository.SaveAsync();
            }
            catch
            {
                await _fileStorageService.DeleteAsync(uploadResult.RelativePath);
                TempData["Error"] = "Document could not be saved. Please try again.";
                return RedirectToAction(nameof(Details), new { id = snapshotId });
            }

            TempData["Success"] = "Document uploaded successfully.";
            return RedirectToAction(nameof(Details), new { id = snapshotId });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var document = await _esicRepository.GetDocumentByIdAsync(id);

            if (document == null)
                return NotFound();

            if (!document.IsActive)
            {
                TempData["Error"] = "Archived documents cannot be downloaded.";
                return RedirectToAction(nameof(Details), new { id = document.EsicSnapshotId });
            }

            var fileBytes =
                await _fileStorageService.DownloadAsync(document.FilePath);

            if (fileBytes == null)
            {
                TempData["Error"] = "Document file was not found.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = document.EsicSnapshotId });
            }

            return File(
                fileBytes,
                GetContentType(document.FileName),
                Path.GetFileName(document.FileName));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int id, int snapshotId)
        {
            var document = await _esicRepository.GetDocumentByIdAsync(id);
            if (document == null)
                return NotFound();

            if (document.Snapshot == null)
                return NotFound();

            if (document.Snapshot.Status != FinancialConstants.StatutoryStatus.Pending)
            {
                TempData["Error"] = "Documents linked to filed or paid ESIC snapshots cannot be deleted.";
                return RedirectToAction(nameof(Details), new { id = document.EsicSnapshotId });
            }

            await _esicRepository.DeleteDocumentAsync(document);
            await _esicRepository.SaveAsync();

            TempData["Success"] = "Document archived successfully.";
            return RedirectToAction(nameof(Details), new { id = document.EsicSnapshotId });
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
        #endregion
    }
}
