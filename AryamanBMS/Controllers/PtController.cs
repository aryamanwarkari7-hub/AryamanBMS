using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class PtController : Controller
    {
        private readonly IPtRepository _ptRepository;
        private readonly IFileStorageService _fileStorageService;

        private const string DocumentFolder = "PtDocuments";

        public PtController(
            IPtRepository ptRepository,
            IFileStorageService fileStorageService)
        {
            _ptRepository = ptRepository;
            _fileStorageService = fileStorageService;
        }

        public async Task<IActionResult> Index()
        {
            var snapshots = await _ptRepository.GetAllSnapshotsAsync();
            return View(snapshots);
        }

        [HttpGet]
        public IActionResult Generate()
        {
            var model = new GeneratePtSnapshotInput
            {
                Month = DateTime.Now.Month,
                Year = DateTime.Now.Year
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(GeneratePtSnapshotInput input)
        {
            if (!ModelState.IsValid)
                return View(input);

            try
            {
                var snapshot = await _ptRepository.GenerateSnapshotAsync(input.Month, input.Year);

                TempData["Success"] = $"PT snapshot for {input.Month}/{input.Year} generated successfully.";
                return RedirectToAction(nameof(Details), new { id = snapshot.PtSnapshotId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(input);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var snapshot = await _ptRepository.GetSnapshotByIdAsync(id);
            if (snapshot == null)
                return NotFound();

            return View(snapshot);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkFiled(int id)
        {
            var snapshot = await _ptRepository.GetSnapshotByIdAsync(id);

            if (snapshot == null)
            {
                return NotFound();
            }

            if (snapshot.TotalPayable <= 0)
            {
                TempData["Error"] = "PT snapshot has no payable amount and cannot be marked as filed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (snapshot.Status == FinancialConstants.StatutoryStatus.Paid)
            {
                TempData["Error"] = "Paid PT snapshots cannot be moved back to filed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await _ptRepository.UpdateSnapshotStatusAsync(
                id,
                FinancialConstants.StatutoryStatus.Filed);

            TempData["Success"] = "PT snapshot marked as Filed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int id)
        {
            var snapshot = await _ptRepository.GetSnapshotByIdAsync(id);

            if (snapshot == null)
            {
                return NotFound();
            }

            if (snapshot.Status != FinancialConstants.StatutoryStatus.Filed)
            {
                TempData["Error"] = "PT snapshot must be marked as filed before it can be marked as paid.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!snapshot.Challans.Any())
            {
                TempData["Error"] = "Please record PT challan details before marking this snapshot as paid.";
                return RedirectToAction(nameof(Details), new { id });
            }

            decimal paidAmount = snapshot.Challans.Sum(x => x.AmountPaid);

            if (paidAmount < snapshot.TotalPayable)
            {
                TempData["Error"] = "Paid challan amount is less than PT payable amount.";
                return RedirectToAction(nameof(Details), new { id });
            }

            await _ptRepository.UpdateSnapshotStatusAsync(
                id,
                FinancialConstants.StatutoryStatus.Paid);

            TempData["Success"] = "PT snapshot marked as Paid.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddChallan(PtChallanModel model)
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
                return RedirectToAction(nameof(Details), new { id = model.PtSnapshotId });
            }

            var snapshot = await _ptRepository.GetSnapshotByIdAsync(model.PtSnapshotId);
            if (snapshot == null)
                return NotFound();


            model.Status = FinancialConstants.StatutoryStatus.Paid;

            await _ptRepository.AddChallanAsync(model);
            await _ptRepository.SaveAsync();

            TempData["Success"] = "PT challan recorded successfully.";
            return RedirectToAction(nameof(Details), new { id = model.PtSnapshotId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(int snapshotId, string documentType, IFormFile file, string? remarks)
        {
            var snapshot = await _ptRepository.GetSnapshotByIdAsync(snapshotId);
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

            var document = new PtDocumentModel
            {
                PtSnapshotId = snapshotId,
                DocumentType = documentType,
                FileName = uploadResult.OriginalFileName,
                FilePath = uploadResult.RelativePath,
                Remarks = remarks
            };

            await _ptRepository.AddDocumentAsync(document);
            await _ptRepository.SaveAsync();

            TempData["Success"] = "Document uploaded successfully.";
            return RedirectToAction(nameof(Details), new { id = snapshotId });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var document = await _ptRepository.GetDocumentByIdAsync(id);

            if (document == null)
                return NotFound();

            var fileBytes =
                await _fileStorageService.DownloadAsync(document.FilePath);

            if (fileBytes == null)
            {
                TempData["Error"] = "Document file was not found.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = document.PtSnapshotId });
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
            var document = await _ptRepository.GetDocumentByIdAsync(id);
            if (document == null)
                return NotFound();

            await _fileStorageService.DeleteAsync(document.FilePath);
            await _ptRepository.DeleteDocumentAsync(id);

            TempData["Success"] = "Document deleted successfully.";
            return RedirectToAction(nameof(Details), new { id = snapshotId });
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