using AryamanBMS.Data;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize]
    [Authorize(Roles = "Admin,HR,Master")]
    public class DashboardController : Controller
    {
        #region Actions

        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            DateTime today = DateTime.Today;
            DateTime monthStart = new(today.Year, today.Month, 1);
            DateTime nextMonth = monthStart.AddMonths(1);
            DateTime exitWindowEnd = today.AddDays(30);

            var model = new MainDashboardViewModel
            {
                Today = today,
                FinancialYear = GetFinancialYear(today)
            };

            model.Hr.TotalEmployees = await _context.Employees.CountAsync();
            model.Hr.ActiveEmployees = await _context.Employees.CountAsync(x => x.IsActive);
            model.Hr.InactiveEmployees =
                Math.Max(model.Hr.TotalEmployees - model.Hr.ActiveEmployees, 0);

            model.Hr.OnLeaveToday = await _context.LeaveApplications.CountAsync(x =>
                x.Status == "Approved" &&
                x.FromDate <= today &&
                x.ToDate >= today);

            model.Hr.PendingLeaveApplications = await _context.LeaveApplications.CountAsync(x =>
                x.Status == "Pending");

            model.Hr.PendingCompOffRequests = await _context.CompOffCredits.CountAsync(x =>
                x.Status == "Pending");

            model.Hr.UpcomingExits = await _context.Employees.CountAsync(x =>
                x.LastWorkingDate.HasValue &&
                x.LastWorkingDate.Value >= today &&
                x.LastWorkingDate.Value <= exitWindowEnd);

            model.Projects.TotalProjects = await _context.Projects.CountAsync();
            model.Projects.ActiveProjects = await _context.Projects.CountAsync(x =>
                x.IsActive &&
                x.Status != "Completed" &&
                x.Status != "Cancelled");

            model.Projects.OpenTasks = await _context.ProjectTasks.CountAsync(x =>
                x.IsActive &&
                x.Status != "Completed");

            model.Projects.OverdueTasks = await _context.ProjectTasks.CountAsync(x =>
                x.IsActive &&
                x.Status != "Completed" &&
                x.DueDate.HasValue &&
                x.DueDate.Value < today);

            model.Projects.OpenRisks = await _context.ProjectRisks.CountAsync(x =>
                x.IsActive &&
                x.RiskStatus == "Open");

            model.Projects.PendingActionItems = await _context.ProjectMeetingActionItems.CountAsync(x =>
                x.IsActive &&
                x.ActionStatus != "Completed");

            model.Finance.TotalInvoiced = await _context.Invoices
                .Where(x => !x.IsDeleted && x.InvoiceStatus != "Cancelled")
                .SumAsync(x => (decimal?)x.GrandTotal) ?? 0m;

            model.Finance.OutstandingReceivables = await _context.Invoices
                .Where(x => !x.IsDeleted &&
                            x.InvoiceStatus != "Cancelled" &&
                            x.BalanceAmount > 0)
                .SumAsync(x => (decimal?)x.BalanceAmount) ?? 0m;

            model.Finance.OverdueReceivables = await _context.Invoices
                .Where(x => !x.IsDeleted &&
                            x.InvoiceStatus != "Cancelled" &&
                            x.BalanceAmount > 0 &&
                            x.DueDate.HasValue &&
                            x.DueDate.Value < today)
                .SumAsync(x => (decimal?)x.BalanceAmount) ?? 0m;

            model.Finance.CollectionThisMonth = await _context.PaymentReceipts
                .Where(x => x.IsActive &&
                            !x.IsCancelled &&
                            x.ReceiptDate >= monthStart &&
                            x.ReceiptDate < nextMonth)
                .SumAsync(x => (decimal?)x.AmountReceived) ?? 0m;

            model.Finance.DraftInvoices = await _context.Invoices.CountAsync(x =>
                !x.IsDeleted &&
                x.InvoiceStatus == "Draft");

            model.Finance.PendingExpenseVouchers = await _context.ExpenseVouchers.CountAsync(x =>
                x.IsActive &&
                !x.IsReversed &&
                x.ApprovalStatus != "Posted" &&
                x.ApprovalStatus != "Rejected");

            model.Finance.AdvanceReceiptBalance = await _context.AdvanceReceipts
                .Where(x => !x.IsCancelled && x.AvailableBalance > 0)
                .SumAsync(x => (decimal?)x.AvailableBalance) ?? 0m;

            model.Finance.AssetBookValue = await _context.OfficeAssets
                .Where(x => x.IsActive)
                .SumAsync(x => (decimal?)x.WrittenDownValue) ?? 0m;

            model.Finance.NetGstPayable = await _context.GstMonthlySnapshots
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .Select(x => x.NetGSTPayable)
                .FirstOrDefaultAsync();

            model.PendingApprovals =
                model.Hr.PendingLeaveApplications +
                model.Hr.PendingCompOffRequests +
                model.Finance.PendingExpenseVouchers;

            model.OverdueItems =
                model.Projects.OverdueTasks +
                await _context.ProjectRisks.CountAsync(x =>
                    x.IsActive &&
                    x.RiskStatus == "Open" &&
                    x.TargetResolutionDate.HasValue &&
                    x.TargetResolutionDate.Value < today) +
                await _context.ProjectMeetingActionItems.CountAsync(x =>
                    x.IsActive &&
                    x.ActionStatus != "Completed" &&
                    x.DueDate.HasValue &&
                    x.DueDate.Value < today);

            model.FinanceAttention =
                model.Finance.DraftInvoices +
                model.Finance.PendingExpenseVouchers +
                await _context.Invoices.CountAsync(x =>
                    !x.IsDeleted &&
                    x.InvoiceStatus != "Cancelled" &&
                    x.BalanceAmount > 0 &&
                    x.DueDate.HasValue &&
                    x.DueDate.Value < today);

            model.RecentEmployees = await _context.Employees
                 .OrderByDescending(x => x.Id)
                 .Take(5)
                 .Select(x => new DashboardListItem
                 {
                     Title = ((x.FirstName ?? "") + " " + (x.LastName ?? "")).Trim(),
                     Subtitle = x.EmployeeCode,
                     Meta = x.JoiningDate.ToString("dd-MMM-yyyy"),
                     Badge = x.IsActive ? "Active" : "Inactive",
                     Controller = "Employee",
                     Action = "Details",
                     RouteId = x.Id
                 })
                 .ToListAsync();

            model.OverdueTasks = await _context.ProjectTasks
                .Include(x => x.Project)
                .Where(x => x.IsActive &&
                            x.Status != "Completed" &&
                            x.DueDate.HasValue &&
                            x.DueDate.Value < today)
                .OrderBy(x => x.DueDate)
                .Take(5)
                .Select(x => new DashboardListItem
                 {
                     Title = x.TaskTitle,
                     Subtitle = x.Project != null ? x.Project.ProjectName : "",
                     Meta = x.DueDate.HasValue ? x.DueDate.Value.ToString("dd-MMM-yyyy") : "",
                     Badge = x.Priority,
                     Controller = "ProjectTask",
                     Action = "Details",
                     RouteId = x.Id
                 })
                .ToListAsync();

            model.HighRisks = await _context.ProjectRisks
                .Include(x => x.Project)
                .Where(x => x.IsActive &&
                            x.RiskStatus == "Open")
                .OrderByDescending(x => x.RiskScore)
                .Take(5)
                .Select(x => new DashboardListItem
                 {
                     Title = x.RiskTitle,
                     Subtitle = x.Project != null ? x.Project.ProjectName : "",
                     Meta = "Score " + x.RiskScore,
                     Badge = x.Severity,
                     Controller = "Risk",
                     Action = "Details",
                     RouteId = x.Id
                 })
                .ToListAsync();

            model.OverdueInvoices = await _context.Invoices
                .Include(x => x.Client)
                .Where(x => !x.IsDeleted &&
                            x.InvoiceStatus != "Cancelled" &&
                            x.BalanceAmount > 0 &&
                            x.DueDate.HasValue &&
                            x.DueDate.Value < today)
                .OrderBy(x => x.DueDate)
                .Take(5)
                .Select(x => new DashboardListItem
                  {
                      Title = x.InvoiceNo,
                      Subtitle = x.Client != null ? x.Client.ClientName : "",
                      Meta = x.BalanceAmount.ToString("N2"),
                      Badge = x.PaymentStatus,
                      Controller = "Invoice",
                      Action = "Details",
                      RouteId = x.InvoiceId
                  })
                .ToListAsync();

            model.PendingExpenses = await _context.ExpenseVouchers
                .Include(x => x.Category)
                .Where(x => x.IsActive &&
                            !x.IsReversed &&
                            x.ApprovalStatus != "Posted" &&
                            x.ApprovalStatus != "Rejected")
                .OrderByDescending(x => x.VoucherDate)
                .Take(5)
                .Select(x => new DashboardListItem
{
    Title = x.VoucherNumber,
    Subtitle = x.Category != null ? x.Category.CategoryName : x.Description,
    Meta = x.TotalAmount.ToString("N2"),
    Badge = x.ApprovalStatus,
    Controller = "ExpenseVoucher",
    Action = "Details",
    RouteId = x.ExpenseVoucherId
})
                .ToListAsync();

            model.ProjectStatusBuckets = await BuildProjectStatusBucketsAsync();
            model.ReceivableBuckets = await BuildReceivableBucketsAsync(today);
            model.DepartmentEmployeeBuckets = await BuildDepartmentEmployeeBucketsAsync();
            model.TaskHealthBuckets = await BuildTaskHealthBucketsAsync(today);
            model.MonthlyInvoiceCollectionBuckets =
                await BuildMonthlyInvoiceCollectionBucketsAsync(monthStart);
            model.FinancePressureBuckets = BuildFinancePressureBuckets(model);

            return View(model);
        }

        private async Task<List<DashboardBucket>> BuildDepartmentEmployeeBucketsAsync()
        {
            var employees = await _context.Employees
                .Include(x => x.Department)
                .Where(x => x.IsActive)
                .Select(x => new
                {
                    DepartmentName = x.Department != null
                        ? x.Department.DepartmentName
                        : "Unassigned"
                })
                .ToListAsync();

            var raw = employees
                .GroupBy(x => x.DepartmentName)
                .Select(x => new DashboardBucket
                {
                    Label = x.Key,
                    Count = x.Count(),
                    CssClass = "bucket-info"
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            int total = raw.Sum(x => x.Count);

            foreach (var item in raw)
            {
                item.Percent = total == 0
                    ? 0
                    : Math.Round((decimal)item.Count * 100 / total, 2);
            }

            return raw;
        }

        private async Task<List<DashboardBucket>> BuildProjectStatusBucketsAsync()
        {
            var raw = await _context.Projects
                .GroupBy(x => x.Status)
                .Select(x => new DashboardBucket
                {
                    Label = x.Key,
                    Count = x.Count()
                })
                .ToListAsync();

            int total = raw.Sum(x => x.Count);

            foreach (var item in raw)
            {
                item.Percent = total == 0 ? 0 : Math.Round((decimal)item.Count * 100 / total, 2);
                item.CssClass = GetStatusClass(item.Label);
            }

            return raw;
        }

        private async Task<List<DashboardBucket>> BuildTaskHealthBucketsAsync(DateTime today)
        {
            int completed = await _context.ProjectTasks.CountAsync(x =>
                x.IsActive &&
                x.Status == "Completed");

            int overdue = await _context.ProjectTasks.CountAsync(x =>
                x.IsActive &&
                x.Status != "Completed" &&
                x.DueDate.HasValue &&
                x.DueDate.Value < today);

            int inProgress = await _context.ProjectTasks.CountAsync(x =>
                x.IsActive &&
                x.Status == "In Progress" &&
                !(x.DueDate.HasValue && x.DueDate.Value < today));

            int notStarted = await _context.ProjectTasks.CountAsync(x =>
                x.IsActive &&
                x.Status == "Not Started" &&
                !(x.DueDate.HasValue && x.DueDate.Value < today));

            var buckets = new List<DashboardBucket>
            {
                new() { Label = "Completed", Count = completed, CssClass = "bucket-success" },
                new() { Label = "In Progress", Count = inProgress, CssClass = "bucket-info" },
                new() { Label = "Not Started", Count = notStarted, CssClass = "bucket-neutral" },
                new() { Label = "Overdue", Count = overdue, CssClass = "bucket-danger" }
            };

            int total = buckets.Sum(x => x.Count);

            foreach (var item in buckets)
            {
                item.Percent = total == 0
                    ? 0
                    : Math.Round((decimal)item.Count * 100 / total, 2);
            }

            return buckets;
        }

        private async Task<List<DashboardBucket>> BuildReceivableBucketsAsync(DateTime today)
        {
            var invoices = await _context.Invoices
                .Where(x => !x.IsDeleted &&
                            x.InvoiceStatus != "Cancelled" &&
                            x.BalanceAmount > 0 &&
                            x.DueDate.HasValue)
                .Select(x => new
                {
                    x.BalanceAmount,
                    x.DueDate
                })
                .ToListAsync();

            decimal total = invoices.Sum(x => x.BalanceAmount);

            var buckets = new List<DashboardBucket>
            {
                CreateReceivableBucket("0-30", invoices.Where(x => (today - x.DueDate!.Value).Days <= 30).Sum(x => x.BalanceAmount), total, "bucket-info"),
                CreateReceivableBucket("31-60", invoices.Where(x => (today - x.DueDate!.Value).Days is > 30 and <= 60).Sum(x => x.BalanceAmount), total, "bucket-warning"),
                CreateReceivableBucket("61-90", invoices.Where(x => (today - x.DueDate!.Value).Days is > 60 and <= 90).Sum(x => x.BalanceAmount), total, "bucket-danger"),
                CreateReceivableBucket("90+", invoices.Where(x => (today - x.DueDate!.Value).Days > 90).Sum(x => x.BalanceAmount), total, "bucket-critical")
            };

            return buckets;
        }

        private async Task<List<DashboardBucket>> BuildMonthlyInvoiceCollectionBucketsAsync(
            DateTime currentMonthStart)
        {
            DateTime firstMonth = currentMonthStart.AddMonths(-5);
            DateTime nextMonth = currentMonthStart.AddMonths(1);

            var invoiceRows = await _context.Invoices
                .Where(x => !x.IsDeleted &&
                            x.InvoiceStatus != "Cancelled" &&
                            x.InvoiceDate >= firstMonth &&
                            x.InvoiceDate < nextMonth)
                .Select(x => new
                {
                    x.InvoiceDate,
                    x.GrandTotal
                })
                .ToListAsync();

            var receiptRows = await _context.PaymentReceipts
                .Where(x => x.IsActive &&
                            !x.IsCancelled &&
                            x.ReceiptDate >= firstMonth &&
                            x.ReceiptDate < nextMonth)
                .Select(x => new
                {
                    x.ReceiptDate,
                    x.AmountReceived
                })
                .ToListAsync();

            decimal maxAmount = Math.Max(
                invoiceRows.GroupBy(x => new DateTime(x.InvoiceDate.Year, x.InvoiceDate.Month, 1))
                    .Select(x => x.Sum(y => y.GrandTotal))
                    .DefaultIfEmpty(0m)
                    .Max(),
                receiptRows.GroupBy(x => new DateTime(x.ReceiptDate.Year, x.ReceiptDate.Month, 1))
                    .Select(x => x.Sum(y => y.AmountReceived))
                    .DefaultIfEmpty(0m)
                    .Max());

            var buckets = new List<DashboardBucket>();

            for (int i = 0; i < 6; i++)
            {
                DateTime month = firstMonth.AddMonths(i);
                decimal invoiced = invoiceRows
                    .Where(x => x.InvoiceDate.Year == month.Year &&
                                x.InvoiceDate.Month == month.Month)
                    .Sum(x => x.GrandTotal);
                decimal collected = receiptRows
                    .Where(x => x.ReceiptDate.Year == month.Year &&
                                x.ReceiptDate.Month == month.Month)
                    .Sum(x => x.AmountReceived);

                buckets.Add(new DashboardBucket
                {
                    Label = month.ToString("MMM"),
                    Amount = invoiced,
                    Percent = maxAmount == 0
                        ? 0
                        : Math.Round(invoiced * 100 / maxAmount, 2),
                    Count = maxAmount == 0
                        ? 0
                        : (int)Math.Round(collected * 100 / maxAmount, 0)
                });
            }

            return buckets;
        }

        private static List<DashboardBucket> BuildFinancePressureBuckets(
            MainDashboardViewModel model)
        {
            var buckets = new List<DashboardBucket>
            {
                new()
                {
                    Label = "Outstanding",
                    Amount = model.Finance.OutstandingReceivables,
                    CssClass = "bucket-info"
                },
                new()
                {
                    Label = "Advance",
                    Amount = model.Finance.AdvanceReceiptBalance,
                    CssClass = "bucket-success"
                },
                new()
                {
                    Label = "GST Payable",
                    Amount = model.Finance.NetGstPayable,
                    CssClass = "bucket-warning"
                },
                new()
                {
                    Label = "Overdue",
                    Amount = model.Finance.OverdueReceivables,
                    CssClass = "bucket-danger"
                }
            };

            decimal total = buckets.Sum(x => Math.Abs(x.Amount));

            foreach (var item in buckets)
            {
                item.Percent = total == 0
                    ? 0
                    : Math.Round(Math.Abs(item.Amount) * 100 / total, 2);
            }

            return buckets;
        }

        private static DashboardBucket CreateReceivableBucket(
            string label,
            decimal amount,
            decimal total,
            string cssClass)
        {
            return new DashboardBucket
            {
                Label = label,
                Amount = amount,
                Percent = total == 0 ? 0 : Math.Round(amount * 100 / total, 2),
                CssClass = cssClass
            };
        }

        private static string GetFinancialYear(DateTime date)
        {
            int startYear = date.Month >= 4 ? date.Year : date.Year - 1;
            int endYear = startYear + 1;

            return $"{startYear}-{endYear.ToString()[2..]}";
        }

        private static string GetStatusClass(string status)
        {
            return status switch
            {
                "Completed" => "bucket-success",
                "In Progress" => "bucket-info",
                "Planning" => "bucket-warning",
                "On Hold" => "bucket-danger",
                _ => "bucket-neutral"
            };
        }
        #endregion
    }
}
