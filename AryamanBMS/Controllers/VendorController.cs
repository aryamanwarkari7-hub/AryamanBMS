using AryamanBMS.Data;
using AryamanBMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class VendorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUserModel> _userManager;

        private static readonly Regex GstinRegex =
            new(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public VendorController(
            ApplicationDbContext context,
            UserManager<ApplicationUserModel> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(
    string? search,
    string sortBy = "VendorName",
    string sortOrder = "asc")
        {
            var vendors = await _context.Vendors
                .AsNoTracking()
                .Where(x => x.IsActive)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = search.Trim().ToLower();

                vendors = vendors
                    .Where(x =>
                        x.VendorCode.ToLower().Contains(keyword) ||
                        x.VendorName.ToLower().Contains(keyword) ||
                        (!string.IsNullOrWhiteSpace(x.GSTIN) &&
                            x.GSTIN.ToLower().Contains(keyword)) ||
                        (!string.IsNullOrWhiteSpace(x.PAN) &&
                            x.PAN.ToLower().Contains(keyword)))
                    .ToList();
            }

            bool desc =
                string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            vendors = sortBy switch
            {
                "VendorCode" => desc
                    ? vendors.OrderByDescending(x => x.VendorCode).ToList()
                    : vendors.OrderBy(x => x.VendorCode).ToList(),

                "GSTIN" => desc
                    ? vendors.OrderByDescending(x => x.GSTIN).ToList()
                    : vendors.OrderBy(x => x.GSTIN).ToList(),

                "PAN" => desc
                    ? vendors.OrderByDescending(x => x.PAN).ToList()
                    : vendors.OrderBy(x => x.PAN).ToList(),

                "Status" => desc
                    ? vendors.OrderByDescending(x => x.IsActive).ToList()
                    : vendors.OrderBy(x => x.IsActive).ToList(),

                _ => desc
                    ? vendors.OrderByDescending(x => x.VendorName).ToList()
                    : vendors.OrderBy(x => x.VendorName).ToList()
            };

            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

            return View(vendors);
        }

        public IActionResult Create()
        {
            return View(new VendorModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VendorModel model)
        {
            Normalize(model);
            ValidateVendor(model);

            if (await _context.Vendors.AnyAsync(x => x.VendorCode == model.VendorCode))
            {
                ModelState.AddModelError(nameof(model.VendorCode), "This vendor code already exists.");
            }

            if (!ModelState.IsValid)
                return View(model);

            model.CreatedByUserId = _userManager.GetUserId(User);
            model.CreatedOn = DateTime.Now;
            model.IsActive = true;

            await _context.Vendors.AddAsync(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Vendor '{model.VendorName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var vendor = await _context.Vendors
                .FirstOrDefaultAsync(x => x.VendorId == id && x.IsActive);

            if (vendor == null)
                return NotFound();

            return View(vendor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VendorModel model)
        {
            Normalize(model);
            ValidateVendor(model);

            if (await _context.Vendors.AnyAsync(x =>
                    x.VendorCode == model.VendorCode &&
                    x.VendorId != model.VendorId))
            {
                ModelState.AddModelError(nameof(model.VendorCode), "This vendor code already exists.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var existing = await _context.Vendors
                .FirstOrDefaultAsync(x => x.VendorId == model.VendorId && x.IsActive);

            if (existing == null)
                return NotFound();

            existing.VendorCode = model.VendorCode;
            existing.VendorName = model.VendorName;
            existing.GSTIN = model.GSTIN;
            existing.PAN = model.PAN;
            existing.State = model.State;
            existing.StateCode = model.StateCode;
            existing.Address = model.Address;
            existing.RegistrationType = model.RegistrationType;
            existing.PaymentTerms = model.PaymentTerms;
            existing.BankDetails = model.BankDetails;
            existing.IsActive = model.IsActive;
            existing.UpdatedOn = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Vendor '{existing.VendorName}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null)
                return NotFound();

            vendor.IsActive = false;
            vendor.UpdatedOn = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Vendor deactivated.";
            return RedirectToAction(nameof(Index));
        }

        private static void Normalize(VendorModel model)
        {
            model.VendorCode = model.VendorCode?.Trim().ToUpperInvariant() ?? string.Empty;
            model.VendorName = model.VendorName?.Trim() ?? string.Empty;
            model.GSTIN = string.IsNullOrWhiteSpace(model.GSTIN) ? null : model.GSTIN.Trim().ToUpperInvariant();
            model.PAN = string.IsNullOrWhiteSpace(model.PAN) ? null : model.PAN.Trim().ToUpperInvariant();
            model.State = string.IsNullOrWhiteSpace(model.State) ? null : model.State.Trim();
            model.StateCode = string.IsNullOrWhiteSpace(model.StateCode) ? null : model.StateCode.Trim();
            model.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
            model.RegistrationType = string.IsNullOrWhiteSpace(model.RegistrationType) ? null : model.RegistrationType.Trim();
            model.PaymentTerms = string.IsNullOrWhiteSpace(model.PaymentTerms) ? null : model.PaymentTerms.Trim();
            model.BankDetails = string.IsNullOrWhiteSpace(model.BankDetails) ? null : model.BankDetails.Trim();
        }

        private void ValidateVendor(VendorModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.GSTIN) &&
                !GstinRegex.IsMatch(model.GSTIN))
            {
                ModelState.AddModelError(nameof(model.GSTIN), "Enter a valid 15-character GSTIN.");
            }
        }
    }
}
