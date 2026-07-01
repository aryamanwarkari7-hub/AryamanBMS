using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class CompanyProfileController : Controller
    {
        private readonly ICompanyProfileRepository _companyProfileRepository;

        public CompanyProfileController(
            ICompanyProfileRepository companyProfileRepository)
        {
            _companyProfileRepository = companyProfileRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model =
                await _companyProfileRepository.GetActiveAsync()
                ?? new CompanyProfileModel();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            CompanyProfileModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.CompanyProfileId == 0)
            {
                bool exists =
                    await _companyProfileRepository.ExistsAsync();

                if (exists)
                {
                    TempData["Error"] =
                        "Company Profile already exists.";

                    return View(model);
                }

                await _companyProfileRepository
                    .AddAsync(model);

                await _companyProfileRepository
                    .SaveAsync();

                TempData["Success"] =
                    "Company Profile created successfully.";
            }
            else
            {
                await _companyProfileRepository
                    .UpdateAsync(model);

                await _companyProfileRepository
                    .SaveAsync();

                TempData["Success"] =
                    "Company Profile updated successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}