using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Services.Background
{
    public class InvoiceReminderBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<InvoiceReminderBackgroundService> _logger;

        private static readonly TimeSpan CheckInterval =
            TimeSpan.FromMinutes(30);

        public InvoiceReminderBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<InvoiceReminderBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            await Task.Delay(
                TimeSpan.FromSeconds(15),
                stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckInvoiceRemindersAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Invoice reminder background check failed.");
                }

                await Task.Delay(
                    CheckInterval,
                    stoppingToken);
            }
        }

        private async Task CheckInvoiceRemindersAsync(
            CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var context =
                scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

            var notificationService =
                scope.ServiceProvider
                    .GetRequiredService<INotificationService>();

            var userManager =
                scope.ServiceProvider
                    .GetRequiredService<
                        UserManager<ApplicationUserModel>>();

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var invoices = await context.Invoices
                .AsNoTracking()
                .Include(x => x.Client)
                .Where(x =>
                    !x.IsDeleted &&
                    x.DueDate.HasValue &&
                    x.InvoiceStatus == "Issued" &&
                    x.InvoiceStatus != "Cancelled" &&
                    x.PaymentStatus != "Paid" &&
                    x.BalanceAmount > 0)
                .ToListAsync(cancellationToken);

            var recipients =
                await GetFinanceRecipientsAsync(userManager);

            foreach (var invoice in invoices)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                DateTime dueDate =
                    invoice.DueDate!.Value.Date;

                if (dueDate == tomorrow)
                {
                    await NotifyRecipientsIfMissingAsync(
                        notificationService,
                        recipients,
                        invoice,
                        notificationType: "InvoiceDueTomorrow",
                        title: "Invoice Due Tomorrow",
                        message:
                            $"{invoice.InvoiceNo} for " +
                            $"{invoice.Client?.ClientName ?? "client"} " +
                            $"is due tomorrow. " +
                            $"Balance: ₹{invoice.BalanceAmount:N2}.");
                }
                else if (dueDate == today)
                {
                    await NotifyRecipientsIfMissingAsync(
                        notificationService,
                        recipients,
                        invoice,
                        notificationType: "InvoiceDueToday",
                        title: "Invoice Due Today",
                        message:
                            $"{invoice.InvoiceNo} for " +
                            $"{invoice.Client?.ClientName ?? "client"} " +
                            $"is due today. " +
                            $"Balance: ₹{invoice.BalanceAmount:N2}.");
                }
                else if (dueDate < today)
                {
                    int overdueDays =
                        (today - dueDate).Days;

                    await NotifyRecipientsIfMissingAsync(
                        notificationService,
                        recipients,
                        invoice,
                        notificationType: "InvoiceOverdue",
                        title: "Invoice Overdue",
                        message:
                            $"{invoice.InvoiceNo} for " +
                            $"{invoice.Client?.ClientName ?? "client"} " +
                            $"is overdue by {overdueDays} " +
                            $"{(overdueDays == 1 ? "day" : "days")}. " +
                            $"Balance: ₹{invoice.BalanceAmount:N2}.");
                }
            }
        }

        private static async Task<List<ApplicationUserModel>>
            GetFinanceRecipientsAsync(
                UserManager<ApplicationUserModel> userManager)
        {
            var admins =
                await userManager.GetUsersInRoleAsync("Admin");

            var financeUsers =
                await userManager.GetUsersInRoleAsync("Finance");

            return admins
                .Concat(financeUsers)
                .Where(x => x.IsActive)
                .GroupBy(x => x.Id)
                .Select(x => x.First())
                .ToList();
        }

        private static async Task NotifyRecipientsIfMissingAsync(
            INotificationService notificationService,
            IEnumerable<ApplicationUserModel> recipients,
            InvoiceModel invoice,
            string notificationType,
            string title,
            string message)
        {
            foreach (var recipient in recipients)
            {
                bool alreadyExists =
                    await notificationService.ExistsAsync(
                        recipient.Id,
                        notificationType,
                        "Invoice",
                        invoice.InvoiceId);

                if (alreadyExists)
                {
                    continue;
                }

                await notificationService.CreateAsync(
                    userId: recipient.Id,
                    title: title,
                    message: message,
                    notificationType: notificationType,
                    referenceType: "Invoice",
                    referenceId: invoice.InvoiceId,
                    actionUrl:
                        $"/Invoice/Details/{invoice.InvoiceId}");
            }
        }
    }
}