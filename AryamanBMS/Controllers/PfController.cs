using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class PfController : Controller
    {
        private readonly IPfRepository _pfRepository;
        private readonly IFileStorageService _fileStorageService;

        private const string DocumentFolder = "PfDocuments";

        public PfController(
            IPfRepository pfRepository,
            IFileStorageService fileStorageService)
        {
            _pfRepository = pfRepository;
            _fileStorageService = fileStorageService;
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

            await _pfRepository.UpdateSnapshotStatusAsync(
                id,
                FinancialConstants.StatutoryStatus.Filed);

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

            await _pfRepository.UpdateSnapshotStatusAsync(
                id,
                FinancialConstants.StatutoryStatus.Paid);

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
                Remarks = remarks
            };

            await _pfRepository.AddDocumentAsync(document);
            await _pfRepository.SaveAsync();

            TempData["Success"] = "Document uploaded successfully.";
            return RedirectToAction(nameof(Details), new { id = snapshotId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int id, int snapshotId)
        {
            var document = await _pfRepository.GetDocumentByIdAsync(id);
            if (document == null)
                return NotFound();

            await _fileStorageService.DeleteAsync(document.FilePath);
            await _pfRepository.DeleteDocumentAsync(id);

            TempData["Success"] = "Document deleted successfully.";
            return RedirectToAction(nameof(Details), new { id = snapshotId });
        }
    }
}