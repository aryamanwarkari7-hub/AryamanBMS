using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR,Finance")]
    public class FullAndFinalSettlementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly INotificationService _notificationService;
        private readonly ILogger<FullAndFinalSettlementController> _logger;

        public FullAndFinalSettlementController(
            ApplicationDbContext context,
            UserManager<ApplicationUserModel> userManager,
            INotificationService notificationService,
            ILogger<FullAndFinalSettlementController> logger)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var settlements =
                await _context.FullAndFinalSettlements
                    .AsNoTracking()
                    .Include(x => x.Employee)
                    .OrderByDescending(x => x.CreatedOn)
                    .ToListAsync();

            return View(settlements);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadEmployeesAsync();

            return View(new FullAndFinalSettlementModel
            {
                LastWorkingDate = DateTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FullAndFinalSettlementModel model)
        {
            Normalize(model);

            if (model.EmployeeId <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.EmployeeId),
                    "Employee is required.");
            }

            if (!ModelState.IsValid)
            {
                await LoadEmployeesAsync();
                return View(model);
            }

            model.FinalNetPayable =
                model.SalaryUpToLastWorkingDate +
                model.LeaveEncashment +
                model.BonusOrIncentive +
                model.NoticePay +
                model.OtherPayableAmount -
                model.SalaryAdvanceRecovery -
                model.LoanRecovery -
                model.AssetRecovery -
                model.OtherRecoverableAmount;

            model.ApprovalStatus = "Approved";
            model.PaymentStatus = "Pending";
            model.ApprovedByUserId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);
            model.ApprovedOn = DateTime.Now;
            model.CreatedOn = DateTime.Now;

            await _context.FullAndFinalSettlements.AddAsync(model);
            await _context.SaveChangesAsync();

            var settlement = await _context.FullAndFinalSettlements
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x =>
                    x.FullAndFinalSettlementId == model.FullAndFinalSettlementId);

            if (settlement != null)
            {
                await NotifyEmployeeAsync(
                    settlement.Employee,
                    notificationType: "FullAndFinalCreated",
                    title: "Full and Final Settlement Created",
                    message:
                        $"Your full and final settlement has been created. " +
                        $"Net payable: ₹{settlement.FinalNetPayable:N2}.",
                    referenceType: "FullAndFinalSettlement",
                    referenceId: settlement.FullAndFinalSettlementId,
                    actionUrl: "/Employee/MyDashboard");
            }

            TempData["Success"] =
                "Full and final settlement created successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadEmployeesAsync()
        {
            ViewBag.Employees =
                await _context.Employees
                    .AsNoTracking()
                    .OrderBy(x => x.EmployeeCode)
                    .ToListAsync();
        }

        private static void Normalize(FullAndFinalSettlementModel model)
        {
            model.SalaryUpToLastWorkingDate =
                Math.Round(model.SalaryUpToLastWorkingDate, 2);
            model.LeaveEncashment =
                Math.Round(model.LeaveEncashment, 2);
            model.BonusOrIncentive =
                Math.Round(model.BonusOrIncentive, 2);
            model.NoticePay =
                Math.Round(model.NoticePay, 2);
            model.SalaryAdvanceRecovery =
                Math.Round(model.SalaryAdvanceRecovery, 2);
            model.LoanRecovery =
                Math.Round(model.LoanRecovery, 2);
            model.AssetRecovery =
                Math.Round(model.AssetRecovery, 2);
            model.OtherPayableAmount =
                Math.Round(model.OtherPayableAmount, 2);
            model.OtherRecoverableAmount =
                Math.Round(model.OtherRecoverableAmount, 2);
        }

        private async Task NotifyEmployeeAsync(
            EmployeeModel? employee,
            string notificationType,
            string title,
            string message,
            string referenceType,
            int referenceId,
            string actionUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(employee?.ApplicationUserId))
                {
                    return;
                }

                var recipient =
                    await _userManager.FindByIdAsync(employee.ApplicationUserId);

                if (recipient == null || !recipient.IsActive)
                {
                    return;
                }

                bool exists = await _notificationService.ExistsAsync(
                    recipient.Id,
                    notificationType,
                    referenceType,
                    referenceId);

                if (exists)
                {
                    return;
                }

                await _notificationService.CreateAsync(
                    recipient.Id,
                    title,
                    message,
                    notificationType,
                    referenceType,
                    referenceId,
                    actionUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Full and final notification failed. Type: {NotificationType}, Reference: {ReferenceType}/{ReferenceId}",
                    notificationType,
                    referenceType,
                    referenceId);
            }
        }
    }
}
