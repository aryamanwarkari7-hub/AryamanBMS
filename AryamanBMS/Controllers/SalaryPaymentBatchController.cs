using AryamanBMS.Data;
using AryamanBMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,HR,Finance")]
    public class SalaryPaymentBatchController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalaryPaymentBatchController(ApplicationDbContext context)
        {
            _context = context;
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

            TempData["Success"] =
                "Salary payment batch marked paid.";

            return RedirectToAction(nameof(Index));
        }
    }
}
