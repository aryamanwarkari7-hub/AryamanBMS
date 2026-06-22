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

                if (documentFile != null &&
                    documentFile.Length > 0)
                {
                    string uploadFolder =
                        Path.Combine(
                            _environment.WebRootPath,
                            "documents",
                            "letters");

                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    string fileName =
                        $"{Guid.NewGuid()}_{documentFile.FileName}";

                    string filePath =
                        Path.Combine(uploadFolder, fileName);

                    using var stream =
                        new FileStream(filePath, FileMode.Create);

                    await documentFile.CopyToAsync(stream);

                    letter.DocumentPath =
                        $"/documents/letters/{fileName}";
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