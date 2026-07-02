using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class EsicController : Controller
    {
        private readonly IEsicRepository _esicRepository;
        private readonly IFileStorageService _fileStorageService;

        private const string DocumentFolder = "EsicDocuments";

        public EsicController(
            IEsicRepository esicRepository,
            IFileStorageService fileStorageService)
        {
            _esicRepository = esicRepository;
            _fileStorageService = fileStorageService;
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

            await _esicRepository.UpdateSnapshotStatusAsync(
                id,
                FinancialConstants.StatutoryStatus.Filed);

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

            await _esicRepository.UpdateSnapshotStatusAsync(
                id,
                FinancialConstants.StatutoryStatus.Paid);

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
                Remarks = remarks
            };

            await _esicRepository.AddDocumentAsync(document);
            await _esicRepository.SaveAsync();

            TempData["Success"] = "Document uploaded successfully.";
            return RedirectToAction(nameof(Details), new { id = snapshotId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int id, int snapshotId)
        {
            var document = await _esicRepository.GetDocumentByIdAsync(id);
            if (document == null)
                return NotFound();

            await _fileStorageService.DeleteAsync(document.FilePath);
            await _esicRepository.DeleteDocumentAsync(id);

            TempData["Success"] = "Document deleted successfully.";
            return RedirectToAction(nameof(Details), new { id = snapshotId });
        }
    }
}