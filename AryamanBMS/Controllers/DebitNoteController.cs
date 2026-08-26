using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Master")]
    public class DebitNoteController : Controller
    {
        #region Actions

        private readonly IDebitNoteQueryService _queryService;
        private readonly IDebitNoteService _debitNoteService;

        public DebitNoteController(IDebitNoteQueryService queryService, IDebitNoteService debitNoteService)
        {
            _queryService = queryService;
            _debitNoteService = debitNoteService;
        }

        public async Task<IActionResult> Index(
            string? search,
            string sortBy = "CreatedOn",
            string sortOrder = "desc")
        {
            var notes = await _queryService.GetAllAsync(search, sortBy, sortOrder);
            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            return View(notes);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? invoiceId)
        {
            await LoadInvoicesAsync();

            return View(new DebitNoteModel
            {
                OriginalInvoiceId = invoiceId ?? 0
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DebitNoteModel model)
        {
            var validation = await _debitNoteService.ValidateAsync(model);
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.Key, error.Value);
            }

            if (!ModelState.IsValid)
            {
                await LoadInvoicesAsync();
                return View(model);
            }

            await _debitNoteService.CreateAsync(model, User.FindFirstValue(ClaimTypes.NameIdentifier));
            TempData["Success"] = "Debit note created successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadInvoicesAsync()
        {
            ViewBag.Invoices = await _queryService.GetIssuedInvoicesAsync();
        }
        #endregion
    }
}
