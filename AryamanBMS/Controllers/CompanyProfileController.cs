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

            model.CompanyName = model.CompanyName?.Trim() ?? string.Empty;
            model.GSTIN = model.GSTIN?.Trim().ToUpper();
            model.PAN = model.PAN?.Trim().ToUpper();
            model.CIN = model.CIN?.Trim().ToUpper();
            model.Address = model.Address?.Trim();
            model.Email = model.Email?.Trim();
            model.Phone = model.Phone?.Trim();
            model.VendorRegistrationNumber =
            model.VendorRegistrationNumber?.Trim();

            model.BankName =
                model.BankName?.Trim();

            model.AccountName =
                model.AccountName?.Trim();

            model.AccountNumber =
                model.AccountNumber?.Trim();

            model.IFSCCode =
                model.IFSCCode?.Trim().ToUpper();

            model.BankBranch =
                model.BankBranch?.Trim();

            model.AuthorizedSignatory =
                model.AuthorizedSignatory?.Trim();

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
                model.IsActive = true;
                model.CreatedOn = DateTime.Now;

                await _companyProfileRepository
                    .AddAsync(model);

                await _companyProfileRepository
                    .SaveAsync();

                TempData["Success"] =
                    "Company Profile created successfully.";
            }
            else
            {
                model.IsActive = true;
                model.UpdatedOn = DateTime.Now;
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