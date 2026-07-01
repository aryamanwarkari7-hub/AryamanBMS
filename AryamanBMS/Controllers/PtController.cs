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
                return NotFound();

            await _ptRepository.UpdateSnapshotStatusAsync(id, FinancialConstants.StatutoryStatus.Filed);

            TempData["Success"] = "PT snapshot marked as Filed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int id)
        {
            var snapshot = await _ptRepository.GetSnapshotByIdAsync(id);
            if (snapshot == null)
                return NotFound();

            await _ptRepository.UpdateSnapshotStatusAsync(id, FinancialConstants.StatutoryStatus.Paid);

            TempData["Success"] = "PT snapshot marked as Paid.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddChallan(PtChallanModel model)
        {
            ModelState.Remove(nameof(model.Snapshot));

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please correct the challan details and try again.";
                return RedirectToAction(nameof(Details), new { id = model.PtSnapshotId });
            }

            var snapshot = await _ptRepository.GetSnapshotByIdAsync(model.PtSnapshotId);
            if (snapshot == null)
                return NotFound();

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
    }
}