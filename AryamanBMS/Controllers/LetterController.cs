using AryamanBMS.Extensions;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class LetterController : Controller
    {
        private readonly ILetterRepository _letterRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly IWebHostEnvironment _environment;

        // File size check
        private const long MaximumLetterFileSize = 5 * 1024 * 1024;
        private static readonly string[] AllowedLetterExtensions =
        {
            ".pdf"
        };

        public LetterController(
            ILetterRepository letterRepository,
            IEmployeeRepository employeeRepository,
            UserManager<ApplicationUserModel> userManager,
            IWebHostEnvironment environment)
        {
            _letterRepository = letterRepository;
            _employeeRepository = employeeRepository;
            _userManager = userManager;
            _environment = environment;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 10;

            var query = _letterRepository.Letters
                .AsNoTracking()
                .OrderByDescending(x => x.IssuedOn)
                .ThenByDescending(x => x.Id);

            var model = await query.ToPagedListAsync(
                page,
                pageSize);

            model.Pagination.ControllerName = "Letter";
            model.Pagination.ActionName = nameof(Index);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            LetterModel letter,
            IFormFile? documentFile)
        {
            ModelState.Remove("Employee");
            ModelState.Remove("LetterNumber");
            ModelState.Remove("IssuedBy");
            ModelState.Remove("DocumentPath");

            if (ModelState.IsValid)
            {
                letter.LetterNumber =
                    GenerateLetterNumber();

                letter.IssuedOn =
                    DateTime.Now;

                letter.IssuedBy =
                    User.Identity?.Name;

                if (documentFile != null && documentFile.Length > 0)
                {
                    if (documentFile.Length > MaximumLetterFileSize)
                    {
                        ModelState.AddModelError(
                            "documentFile",
                            "Letter document cannot exceed 5 MB.");

                        await LoadDropdowns();
                        return View(letter);
                    }

                    string extension =
                        Path.GetExtension(documentFile.FileName)
                            .ToLowerInvariant();

                    if (!AllowedLetterExtensions.Contains(extension))
                    {
                        ModelState.AddModelError(
                            "documentFile",
                            "Only PDF letter documents are allowed.");

                        await LoadDropdowns();
                        return View(letter);
                    }

                    string uploadFolder =
                     Path.Combine(
                         _environment.ContentRootPath,
                         "App_Data",
                         "LetterDocuments");

                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    string fileName =
                        $"{Guid.NewGuid():N}{extension}";

                    string filePath =
                        Path.Combine(uploadFolder, fileName);

                    using var stream =
                        new FileStream(filePath, FileMode.CreateNew);

                    await documentFile.CopyToAsync(stream);

                    letter.DocumentPath =
                        Path.Combine("LetterDocuments", fileName);
                }

                await _letterRepository.AddAsync(letter);
                await _letterRepository.SaveAsync();

                TempData["Success"] =
                    "Letter created successfully.";

                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();

            return View(letter);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var letter =
                await _letterRepository.GetByIdAsync(id);

            if (letter == null)
            {
                return NotFound();
            }

            return View(letter);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            var letter =
                await _letterRepository.GetByIdAsync(id);

            if (letter == null ||
                string.IsNullOrWhiteSpace(letter.DocumentPath))
            {
                return NotFound();
            }

            string filePath;
            string allowedRoot;

            if (letter.DocumentPath.StartsWith(
                "/documents/letters/",
                StringComparison.OrdinalIgnoreCase))
            {
                allowedRoot = Path.GetFullPath(
                    Path.Combine(
                        _environment.WebRootPath,
                        "documents",
                        "letters"));

                filePath = Path.GetFullPath(
                    Path.Combine(
                        _environment.WebRootPath,
                        letter.DocumentPath.TrimStart('/').Replace(
                            '/',
                            Path.DirectorySeparatorChar)));
            }
            else
            {
                allowedRoot = Path.GetFullPath(
                    Path.Combine(
                        _environment.ContentRootPath,
                        "App_Data",
                        "LetterDocuments"));

                filePath = Path.GetFullPath(
                    Path.Combine(
                        _environment.ContentRootPath,
                        "App_Data",
                        letter.DocumentPath));
            }

            if (!filePath.StartsWith(
                allowedRoot,
                StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            return PhysicalFile(
                filePath,
                "application/pdf",
                $"{letter.LetterNumber}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var letter =
                await _letterRepository.GetByIdAsync(id);

            if (letter == null)
            {
                return NotFound();
            }

            return View(letter);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var letter =
                await _letterRepository.GetByIdAsync(id);

            if (letter != null)
            {
                await _letterRepository.DeleteAsync(letter);
                await _letterRepository.SaveAsync();
            }

            TempData["Success"] =
                "Letter deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdowns()
        {
            ViewBag.Employees =
                await _employeeRepository.Employees
                .Where(x => x.IsActive)
                .OrderBy(x => x.FirstName)
                .ToListAsync();

            ViewBag.LetterTypes =
                new List<string>
                {
                    "Offer",
                    "Appointment",
                    "Increment",
                    "Warning",
                    "Experience",
                    "Policy",
                    "Other"
                };
        }

        private string GenerateLetterNumber()
        {
            int nextId =
                _letterRepository.Letters.Count() + 1;

            return $"LTR-{DateTime.Today.Year}-{nextId:000}";
        }
    }
}