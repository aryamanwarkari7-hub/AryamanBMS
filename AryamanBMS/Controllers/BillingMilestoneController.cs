using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers;

[Authorize(Roles = "Admin,Master")]
public class BillingMilestoneController : Controller
{
    private readonly IBillingMilestoneService _billingMilestoneService;

    public BillingMilestoneController(IBillingMilestoneService billingMilestoneService)
    {
        _billingMilestoneService = billingMilestoneService;
    }

    public async Task<IActionResult> Index(
        int? purchaseWorkOrderId,
        string? search,
        string sortBy = "Order",
        string sortOrder = "asc")
    {
        var milestones = await _billingMilestoneService.GetAllAsync(
            purchaseWorkOrderId, search, sortBy, sortOrder);

        ViewBag.PurchaseWorkOrderId = purchaseWorkOrderId;
        ViewBag.Search = search;
        ViewBag.SortBy = sortBy;
        ViewBag.SortOrder = sortOrder;

        return View(milestones);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? purchaseWorkOrderId)
    {
        await LoadDropdownsAsync();

        return View(new BillingMilestoneModel
        {
            PurchaseWorkOrderId = purchaseWorkOrderId ?? 0,
            CompletionStatus = "Pending",
            IsActive = true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BillingMilestoneModel model)
    {
        AddValidationErrors(await _billingMilestoneService.ValidateAsync(model));

        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return View(model);
        }

        await _billingMilestoneService.CreateAsync(model);

        TempData["Success"] =
            "Billing milestone created successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var milestone = await _billingMilestoneService.GetByIdAsync(id);

        if (milestone == null)
        {
            return NotFound();
        }

        await LoadDropdownsAsync();

        return View(milestone);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BillingMilestoneModel model)
    {
        if (id != model.BillingMilestoneId)
        {
            return NotFound();
        }

        var existing = await _billingMilestoneService.GetByIdAsync(id);

        if (existing == null)
        {
            return NotFound();
        }

        AddValidationErrors(await _billingMilestoneService.ValidateAsync(model));

        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return View(model);
        }

        await _billingMilestoneService.UpdateAsync(existing, model);

        TempData["Success"] =
            "Billing milestone updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var milestone = await _billingMilestoneService.ToggleActiveAsync(id);

        if (milestone == null)
        {
            return NotFound();
        }

        TempData["Success"] = milestone.IsActive
            ? "Billing milestone activated successfully."
            : "Billing milestone deactivated successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> GetProjectsByPurchaseOrder(int purchaseOrderId)
    {
        return Json(await _billingMilestoneService
            .GetProjectsByPurchaseOrderAsync(purchaseOrderId));
    }

    private async Task LoadDropdownsAsync()
    {
        ViewBag.PurchaseOrders =
            await _billingMilestoneService.GetActivePurchaseOrdersAsync();
    }

    private void AddValidationErrors(IReadOnlyDictionary<string, string> errors)
    {
        foreach (var (field, message) in errors)
        {
            ModelState.AddModelError(field, message);
        }
    }
}