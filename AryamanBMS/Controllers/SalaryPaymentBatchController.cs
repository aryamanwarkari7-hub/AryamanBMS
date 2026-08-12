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
    [Authorize(Roles = "Admin,HR")]
    public class SalaryPaymentBatchController : Controller
    {
        #region Actions

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly INotificationService _notificationService;
        private readonly ILogger<SalaryPaymentBatchController> _logger;

        public SalaryPaymentBatchController(
            ApplicationDbContext context,
            UserManager<ApplicationUserModel> userManager,
            INotificationService notificationService,
            ILogger<SalaryPaymentBatchController> logger)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var batches =
                await _context.SalaryPaymentBatches
                    .AsNoTracking()
                    .OrderByDescending(x => x.Year)
                    .ThenByDescending(x => x.Month)
                    .ThenByDescending(x => x.CreatedOn)
                    .ToListAsync();

            return View(batches);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBatch(
            int month,
            int year,
            string? bankAccount)
        {
            var salaries =
                await _context.SalaryRecords
                    .Where(x =>
                        x.Month == month &&
                        x.Year == year &&
                        x.PayrollStatus == "Finalized" &&
                        x.PaymentStatus != "Paid")
                    .ToListAsync();

            if (!salaries.Any())
            {
                TempData["Error"] =
                    "No finalized unpaid salary records found for this period.";

                return RedirectToAction(nameof(Index));
            }

            var batch = new SalaryPaymentBatchModel
            {
                Month = month,
                Year = year,
                BankAccount = bankAccount?.Trim(),
                TotalEmployees = salaries.Count,
                TotalNetSalary = salaries.Sum(x => x.NetSalary),
                PaymentStatus = "Processing",
                ProcessedByUserId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                CreatedOn = DateTime.Now
            };

            foreach (var salary in salaries)
            {
                salary.PaymentStatus = "Processing";
            }

            await _context.SalaryPaymentBatches.AddAsync(batch);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Salary payment batch created successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(
            int id,
            string transactionReference)
        {
            var batch =
                await _context.SalaryPaymentBatches
                    .FirstOrDefaultAsync(x =>
                        x.SalaryPaymentBatchId == id);

            if (batch == null)
            {
                return NotFound();
            }

            var salaries =
                await _context.SalaryRecords
                    .Include(x => x.Employee)
                    .Where(x =>
                        x.Month == batch.Month &&
                        x.Year == batch.Year &&
                        x.PaymentStatus == "Processing")
                    .ToListAsync();

            foreach (var salary in salaries)
            {
                salary.PaymentStatus = "Paid";
                salary.PaidOn = DateTime.Now;
                salary.PaidByUserId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);
            }

            batch.PaymentStatus = "Paid";
            batch.PaymentDate = DateTime.Today;
            batch.TransactionReference =
                transactionReference?.Trim();
            batch.UpdatedOn = DateTime.Now;

            await _context.SaveChangesAsync();

            foreach (var salary in salaries)
            {
                await NotifyEmployeeSalaryPaidAsync(salary);
            }

            TempData["Success"] =
                "Salary payment batch marked paid.";

            return RedirectToAction(nameof(Index));
        }

        private async Task NotifyEmployeeSalaryPaidAsync(
            SalaryRecordModel salary)
        {
            try
            {
                string? recipientUserId =
                    salary.Employee?.ApplicationUserId;

                if (string.IsNullOrWhiteSpace(recipientUserId))
                {
                    return;
                }

                var recipient =
                    await _userManager.FindByIdAsync(recipientUserId);

                if (recipient == null || !recipient.IsActive)
                {
                    return;
                }

                bool exists =
                    await _notificationService.ExistsAsync(
                        recipient.Id,
                        "SalaryPaid",
                        "SalaryRecord",
                        salary.Id);

                if (exists)
                {
                    return;
                }

                await _notificationService.CreateAsync(
                    userId: recipient.Id,
                    title: "Salary Paid",
                    message:
                        $"Salary for {GetMonthName(salary.Month)} {salary.Year} " +
                        $"has been paid. Net salary: ₹{salary.NetSalary:N2}.",
                    notificationType: "SalaryPaid",
                    referenceType: "SalaryRecord",
                    referenceId: salary.Id,
                    actionUrl: "/Salary/MySalary");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Salary payment notification failed for SalaryRecordId: {SalaryRecordId}",
                    salary.Id);
            }
        }

        private static string GetMonthName(int month)
        {
            return new System.Globalization.CultureInfo("en-US")
                .DateTimeFormat.GetMonthName(month);
        }
        #endregion
    }
}
