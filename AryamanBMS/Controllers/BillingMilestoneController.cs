using AryamanBMS.Data;
using AryamanBMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class BillingMilestoneController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BillingMilestoneController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? purchaseWorkOrderId)
        {
            var milestones =
                await _context.BillingMilestones
                    .AsNoTracking()
                    .Include(x => x.PurchaseWorkOrder)
                        .ThenInclude(x => x!.Client)
                    .Include(x => x.Project)
                    .Where(x =>
                        !purchaseWorkOrderId.HasValue ||
                        x.PurchaseWorkOrderId == purchaseWorkOrderId.Value)
                    .OrderByDescending(x => x.IsActive)
                    .ThenBy(x => x.PurchaseWorkOrder!.OrderNumber)
                    .ThenBy(x => x.SortOrder)
                    .ThenBy(x => x.MilestoneName)
                    .ToListAsync();

            ViewBag.PurchaseWorkOrderId = purchaseWorkOrderId;

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
            Normalize(model);
            await ValidateMilestoneAsync(model);

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return View(model);
            }

            model.CreatedOn = DateTime.Now;
            model.IsActive = true;
            UpdateComputedValues(model);

            await _context.BillingMilestones.AddAsync(model);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Billing milestone created successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var milestone =
                await _context.BillingMilestones
                    .FirstOrDefaultAsync(x =>
                        x.BillingMilestoneId == id);

            if (milestone == null)
            {
                return NotFound();
            }

            await LoadDropdownsAsync();

            return View(milestone);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            BillingMilestoneModel model)
        {
            if (id != model.BillingMilestoneId)
            {
                return NotFound();
            }

            var existing =
                await _context.BillingMilestones
                    .FirstOrDefaultAsync(x =>
                        x.BillingMilestoneId == id);

            if (existing == null)
            {
                return NotFound();
            }

            Normalize(model);
            await ValidateMilestoneAsync(model);

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return View(model);
            }

            existing.PurchaseWorkOrderId = model.PurchaseWorkOrderId;
            existing.ProjectId = model.ProjectId;
            existing.MilestoneName = model.MilestoneName;
            existing.MilestoneDescription = model.MilestoneDescription;
            existing.MilestoneValue = model.MilestoneValue;
            existing.CompletionStatus = model.CompletionStatus;
            existing.ApprovalDate = model.ApprovalDate;
            existing.SortOrder = model.SortOrder;
            existing.IsActive = model.IsActive;
            existing.UpdatedOn = DateTime.Now;

            RefreshComputedValues(existing);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Billing milestone updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var milestone =
                await _context.BillingMilestones
                    .FirstOrDefaultAsync(x =>
                        x.BillingMilestoneId == id);

            if (milestone == null)
            {
                return NotFound();
            }

            milestone.IsActive = !milestone.IsActive;
            milestone.UpdatedOn = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = milestone.IsActive
                ? "Billing milestone activated successfully."
                : "Billing milestone deactivated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectsByPurchaseOrder(
    int purchaseOrderId)
        {
            var order = await _context.PurchaseOrders
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.PurchaseOrderId == purchaseOrderId &&
                    x.IsActive &&
                    x.Status != "Cancelled");

            if (order == null)
            {
                return Json(Array.Empty<object>());
            }

            var projects = await _context.Projects
                .AsNoTracking()
                .Where(x =>
                    x.Id == order.ClientId)
                .OrderBy(x => x.ProjectName)
                .Select(x => new
                {
                    id = x.Id,
                    projectCode = x.ProjectCode,
                    projectName = x.ProjectName
                })
                .ToListAsync();

            return Json(projects);
        }

        private async Task LoadDropdownsAsync()
        {
            ViewBag.PurchaseOrders =
                await _context.PurchaseOrders
                    .AsNoTracking()
                    .Include(x => x.Client)
                    .Where(x =>
                        x.IsActive &&
                        x.Status != "Cancelled")
                    .OrderByDescending(x => x.OrderDate)
                    .ThenBy(x => x.OrderNumber)
                    .ToListAsync();

            
        }

        private static void Normalize(BillingMilestoneModel model)
        {
            model.MilestoneName =
                model.MilestoneName?.Trim() ?? string.Empty;

            model.MilestoneDescription =
                string.IsNullOrWhiteSpace(model.MilestoneDescription)
                    ? null
                    : model.MilestoneDescription.Trim();

            model.CompletionStatus =
                string.IsNullOrWhiteSpace(model.CompletionStatus)
                    ? "Pending"
                    : model.CompletionStatus.Trim();

            model.MilestoneValue =
                Math.Round(model.MilestoneValue, 2);

            model.BilledValue =
                Math.Round(model.BilledValue, 2);
        }

        private async Task ValidateMilestoneAsync(
            BillingMilestoneModel model)
        {
            if (model.PurchaseWorkOrderId <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.PurchaseWorkOrderId),
                    "Purchase / Work Order is required.");

                return;
            }

            var order =
                await _context.PurchaseOrders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.PurchaseOrderId == model.PurchaseWorkOrderId &&
                        x.IsActive &&
                        x.Status != "Cancelled");

            if (order == null)
            {
                ModelState.AddModelError(
                    nameof(model.PurchaseWorkOrderId),
                    "Selected Purchase / Work Order is invalid.");

                return;
            }

            if (!order.OrderAmount.HasValue ||
                order.OrderAmount.Value <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.PurchaseWorkOrderId),
                    "Purchase / Work Order amount is required before milestones.");
            }

            if (model.ProjectId.HasValue)
            {
                bool projectMatchesOrderClient =
                    await _context.Projects
                        .AsNoTracking()
                        .AnyAsync(x =>
                            x.Id == model.ProjectId.Value &&
                            x.Id == order.ClientId);

                if (!projectMatchesOrderClient)
                {
                    ModelState.AddModelError(
                        nameof(model.ProjectId),
                        "Selected project does not belong to the PO / WO client.");
                }
            }

            if (string.IsNullOrWhiteSpace(model.MilestoneName))
            {
                ModelState.AddModelError(
                    nameof(model.MilestoneName),
                    "Milestone name is required.");
            }

            if (model.MilestoneValue <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.MilestoneValue),
                    "Milestone value must be greater than zero.");
            }

            if (model.CompletionStatus is not
                ("Pending" or "Approved" or "Completed" or "On Hold"))
            {
                ModelState.AddModelError(
                    nameof(model.CompletionStatus),
                    "Invalid milestone status.");
            }

            decimal otherMilestoneTotal =
                await _context.BillingMilestones
                    .AsNoTracking()
                    .Where(x =>
                        x.PurchaseWorkOrderId == model.PurchaseWorkOrderId &&
                        x.BillingMilestoneId != model.BillingMilestoneId &&
                        x.IsActive)
                    .SumAsync(x => x.MilestoneValue);

            if (order.OrderAmount.HasValue &&
                otherMilestoneTotal + model.MilestoneValue >
                order.OrderAmount.Value)
            {
                decimal available =
                    order.OrderAmount.Value - otherMilestoneTotal;

                ModelState.AddModelError(
                    nameof(model.MilestoneValue),
                    $"Milestone exceeds remaining PO / WO value. Available amount is {available:N2}.");
            }
        }

        private void RefreshComputedValues(BillingMilestoneModel model)
        {
            decimal billedValue =
                _context.Invoices
                    .AsNoTracking()
                    .Where(x =>
                        x.BillingMilestoneId == model.BillingMilestoneId &&
                        !x.IsDeleted &&
                        x.InvoiceStatus != "Cancelled")
                    .Sum(x => x.GrandTotal);

            model.BilledValue = Math.Round(billedValue, 2);
            UpdateComputedValues(model);
        }

        private static void UpdateComputedValues(
            BillingMilestoneModel model)
        {
            model.RemainingBillableValue =
                Math.Max(
                    0,
                    Math.Round(
                        model.MilestoneValue - model.BilledValue,
                        2));
        }


    }
}
