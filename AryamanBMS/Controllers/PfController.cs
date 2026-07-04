using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Identity;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class PfController : Controller
    {
        private readonly IPfRepository _pfRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly UserManager<ApplicationUserModel> _userManager;

        private const string DocumentFolder = "PfDocuments";

        public PfController(
          IPfRepository pfRepository,
          IFileStorageService fileStorageService,
          UserManager<ApplicationUserModel> userManager)
        {
            _pfRepository = pfRepository;
            _fileStorageService = fileStorageService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var snapshots = await _pfRepository.GetAllSnapshotsAsync();
            return View(snapshots);
        }

        [HttpGet]
        public IActionResult Generate()
        {
            var model = new GenerateSnapshotInputModel
            {
                Month = DateTime.Now.Month,
                Year = DateTime.Now.Year
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(GenerateSnapshotInputModel input)
        {
            if (!ModelState.IsValid)
                return View(input);

            try
            {
                var snapshot = await _pfRepository.GenerateSnapshotAsync(input.Month, input.Year);

                TempData["Success"] = $"PF snapshot for {input.Month}/{input.Year} generated successfully.";
                return RedirectToAction(nameof(Details), new { id = snapshot.PfSnapshotId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(input);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var snapshot = await _pfRepository.GetSnapshotByIdAsync(id);
            if (snapshot == null)
                return NotFound();

            return View(snapshot);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFiled(int id)
        {
            var snapshot = await _pfRepository.GetSnapshotByIdAsync(id);

            if (snapshot == null)
            {
                return NotFound();
            }

            if (snapshot.TotalPayable <= 0)
            {
                TempData["Error"] = "PF snapshot has no payable amount and cannot be marked as filed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (snapshot.Status == FinancialConstants.StatutoryStatus.Paid)
            {
                TempData["Error"] = "Paid PF snapshots cannot be moved back to filed.";
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
                await _pfRepository.MarkFiledAsync(
                    id,
                    userId);

            if (!updated)
            {
                TempData["Error"] =
                    "PF snapshot could not be marked as filed.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            TempData["Success"] = "PF snapshot marked as Filed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int id)
        {
            var snapshot = await _pfRepository.GetSnapshotByIdAsync(id);

            if (snapshot == null)
            {
                return NotFound();
            }

            if (snapshot.Status != FinancialConstants.StatutoryStatus.Filed)
            {
                TempData["Error"] = "PF snapshot must be marked as filed before it can be marked as paid.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!snapshot.Challans.Any())
            {
                TempData["Error"] = "Please record PF challan details before marking this snapshot as paid.";
                return RedirectToAction(nameof(Details), new { id });
            }

            decimal paidAmount = snapshot.Challans.Sum(x => x.AmountPaid);

            if (paidAmount < snapshot.TotalPayable)
            {
                TempData["Error"] = "Paid challan amount is less than PF payable amount.";
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
                await _pfRepository.MarkPaidAsync(
                    id,
                    userId);

            if (!updated)
            {
                TempData["Error"] =
                    "PF snapshot could not be marked as paid.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            TempData["Success"] = "PF snapshot marked as Paid.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddChallan(PfChallanModel model)
        {
            ModelState.Remove(nameof(model.Snapshot));

            model.TRRN = model.TRRN?.Trim();
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
                return RedirectToAction(nameof(Details), new { id = model.PfSnapshotId });
            }

            var snapshot = await _pfRepository.GetSnapshotByIdAsync(model.PfSnapshotId);
            if (snapshot == null)
                return NotFound();

            model.Status = FinancialConstants.StatutoryStatus.Paid;

            await _pfRepository.AddChallanAsync(model);
            await _pfRepository.SaveAsync();

            TempData["Success"] = "PF challan recorded successfully.";
            return RedirectToAction(nameof(Details), new { id = model.PfSnapshotId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(int snapshotId, string documentType, IFormFile file, string? remarks)
        {
            var snapshot = await _pfRepository.GetSnapshotByIdAsync(snapshotId);
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

            var document = new PfDocumentModel
            {
                PfSnapshotId = snapshotId,
                DocumentType = documentType,
                FileName = uploadResult.OriginalFileName,
                FilePath = uploadResult.RelativePath,
                Remarks = remarks,
                UploadedByUserId = _userManager.GetUserId(User),
                IsActive = true
            };

            try
            {
                await _pfRepository.AddDocumentAsync(document);
                await _pfRepository.SaveAsync();
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
            var document = await _pfRepository.GetDocumentByIdAsync(id);

            if (document == null)
                return NotFound();

            if (!document.IsActive)
            {
                TempData["Error"] = "Archived documents cannot be downloaded.";
                return RedirectToAction(nameof(Details), new { id = document.PfSnapshotId });
            }

            var fileBytes =
                await _fileStorageService.DownloadAsync(document.FilePath);

            if (fileBytes == null)
            {
                TempData["Error"] = "Document file was not found.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = document.PfSnapshotId });
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
            var document = await _pfRepository.GetDocumentByIdAsync(id);
            if (document == null)
                return NotFound();

            if (document.Snapshot == null)
                return NotFound();

            if (document.Snapshot.Status != FinancialConstants.StatutoryStatus.Pending)
            {
                TempData["Error"] = "Documents linked to filed or paid PF snapshots cannot be deleted.";
                return RedirectToAction(nameof(Details), new { id = document.PfSnapshotId });
            }

            await _pfRepository.DeleteDocumentAsync(document);
            await _pfRepository.SaveAsync();

            TempData["Success"] = "Document archived successfully.";
            return RedirectToAction(nameof(Details), new { id = document.PfSnapshotId });
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
