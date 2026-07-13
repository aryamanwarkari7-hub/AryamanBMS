using AryamanBMS.Data;
using AryamanBMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Services.Background
{
    public class TaskReminderBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TaskReminderBackgroundService> _logger;

        private static readonly TimeSpan CheckInterval =
             TimeSpan.FromMinutes(30);

        public TaskReminderBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<TaskReminderBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            // Small delay so application startup completes first.
            await Task.Delay(
                TimeSpan.FromSeconds(15),
                stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckTaskRemindersAsync(stoppingToken);
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
                        "Task reminder background check failed.");
                }

                await Task.Delay(
                    CheckInterval,
                    stoppingToken);
            }
        }

        private async Task CheckTaskRemindersAsync(
            CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var context =
                scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

            var notificationService =
                scope.ServiceProvider
                    .GetRequiredService<INotificationService>();

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var tasks = await context.ProjectTasks
                .AsNoTracking()
                .Include(x => x.AssignedEmployee)
                .Where(x =>
                    x.IsActive &&
                    x.DueDate.HasValue &&
                    x.AssignedEmployeeId.HasValue &&
                    x.AssignedEmployee != null &&
                    x.AssignedEmployee.ApplicationUserId != null &&
                    x.Status != "Completed")
                .ToListAsync(cancellationToken);

            foreach (var task in tasks)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                string userId =
                    task.AssignedEmployee!.ApplicationUserId!;

                DateTime dueDate =
                    task.DueDate!.Value.Date;

                if (dueDate == tomorrow)
                {
                    await CreateReminderIfMissingAsync(
                        notificationService,
                        userId,
                        task.Id,
                        "TaskDueTomorrow",
                        "Task Due Tomorrow",
                        $"Task {task.TaskCode} - {task.TaskTitle} " +
                        $"is due tomorrow, {dueDate:dd MMM yyyy}.");
                }
                else if (dueDate == today)
                {
                    await CreateReminderIfMissingAsync(
                        notificationService,
                        userId,
                        task.Id,
                        "TaskDueToday",
                        "Task Due Today",
                        $"Task {task.TaskCode} - {task.TaskTitle} " +
                        $"is due today, {dueDate:dd MMM yyyy}.");
                }
                else if (dueDate < today)
                {
                    int overdueDays =
                        (today - dueDate).Days;

                    await CreateReminderIfMissingAsync(
                        notificationService,
                        userId,
                        task.Id,
                        "TaskOverdue",
                        "Task Overdue",
                        $"Task {task.TaskCode} - {task.TaskTitle} " +
                        $"is overdue by {overdueDays} " +
                        $"{(overdueDays == 1 ? "day" : "days")}.");
                }
            }
        }

        private static async Task CreateReminderIfMissingAsync(
            INotificationService notificationService,
            string userId,
            int taskId,
            string notificationType,
            string title,
            string message)
        {
            bool alreadyExists =
                await notificationService.ExistsAsync(
                    userId,
                    notificationType,
                    "ProjectTask",
                    taskId);

            if (alreadyExists)
            {
                return;
            }

            await notificationService.CreateAsync(
                userId: userId,
                title: title,
                message: message,
                notificationType: notificationType,
                referenceType: "ProjectTask",
                referenceId: taskId,
                actionUrl: $"/ProjectTask/Details/{taskId}");
        }
    }
}