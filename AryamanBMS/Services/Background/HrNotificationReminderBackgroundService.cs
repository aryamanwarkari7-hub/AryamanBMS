using AryamanBMS.Data;
using AryamanBMS.Services.Interface;
using AryamanBMS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

using AryamanBMS.Business.Interfaces;

namespace AryamanBMS.Services.Background
{
    public class HrNotificationReminderBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HrNotificationReminderBackgroundService> _logger;

        public HrNotificationReminderBackgroundService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<HrNotificationReminderBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "HR notification reminder failed.");
                }

                await Task.Delay(
                    TimeSpan.FromHours(1),
                    stoppingToken);
            }
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            var today = DateTime.Today;

            await using var scope =
                _scopeFactory.CreateAsyncScope();

            var context =
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var notificationService =
                scope.ServiceProvider.GetRequiredService<INotificationService>();

            var workingDayService =
                scope.ServiceProvider.GetRequiredService<IWorkingDayService>();

            await NotifyCompOffExpiringAsync(
                context,
                notificationService,
                today,
                cancellationToken);

            await NotifyBirthdayEmployeesAsync(
                context,
                notificationService,
                today,
                cancellationToken);

            if (!await workingDayService.IsWorkingDayAsync(today))
            {
                return;
            }

            var now = DateTime.Now.TimeOfDay;

            if (now >= GetConfiguredTime(
                "Notifications:AttendanceMissingAfter",
                new TimeSpan(10, 30, 0)))
            {
                await NotifyMissingCheckInAsync(
                    context,
                    notificationService,
                    today,
                    cancellationToken);
            }

            if (now >= GetConfiguredTime(
                "Notifications:CheckOutMissingAfter",
                new TimeSpan(18, 30, 0)))
            {
                await NotifyMissingCheckOutAsync(
                    context,
                    notificationService,
                    today,
                    cancellationToken);
            }
        }

        private async Task NotifyCompOffExpiringAsync(
            ApplicationDbContext context,
            INotificationService notificationService,
            DateTime today,
            CancellationToken cancellationToken)
        {
            var expiryEnd = today.AddDays(7);

            var credits = await context.CompOffCredits
                .AsNoTracking()
                .Include(x => x.Employee)
                .Where(x =>
                    x.Status == "Approved" &&
                    x.ExpiryDate.Date >= today &&
                    x.ExpiryDate.Date <= expiryEnd &&
                    x.CreditDays > x.UsedDays &&
                    x.Employee != null &&
                    x.Employee.ApplicationUserId != null)
                .ToListAsync(cancellationToken);

            foreach (var credit in credits)
            {
                string userId = credit.Employee!.ApplicationUserId!;

                if (await notificationService.ExistsAsync(
                    userId,
                    "CompOffExpiringSoon",
                    "CompOffCredit",
                    credit.Id))
                {
                    continue;
                }

                await notificationService.CreateAsync(
                    userId,
                    "Comp Off Expiring Soon",
                    $"Your {credit.CreditDays - credit.UsedDays:0.##} day(s) Comp Off expires on {credit.ExpiryDate:dd-MMM-yyyy}.",
                    "CompOffExpiringSoon",
                    "CompOffCredit",
                    credit.Id,
                    "/CompOffCredit/Index");
            }
        }

        private static async Task NotifyBirthdayEmployeesAsync(
    ApplicationDbContext context,
    INotificationService notificationService,
    DateTime today,
    CancellationToken cancellationToken)
        {
            var birthdayEmployees =
                await context.Employees
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive &&
                        x.DateOfBirth.HasValue)
                    .ToListAsync(cancellationToken);

            birthdayEmployees = birthdayEmployees
                .Where(x => IsBirthdayToday(x.DateOfBirth!.Value, today))
                .ToList();

            if (birthdayEmployees.Count == 0)
            {
                return;
            }

            var recipients =
                await context.Employees
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive &&
                        x.ApplicationUserId != null)
                    .ToListAsync(cancellationToken);

            foreach (var birthdayEmployee in birthdayEmployees)
            {
                int referenceId =
                    (today.Year * 100000) + birthdayEmployee.Id;

                if (!string.IsNullOrWhiteSpace(birthdayEmployee.ApplicationUserId) &&
                    !await notificationService.ExistsAsync(
                        birthdayEmployee.ApplicationUserId,
                        "EmployeeBirthdaySelf",
                        "EmployeeBirthday",
                        referenceId))
                {
                    await notificationService.CreateAsync(
                        birthdayEmployee.ApplicationUserId,
                        "Happy Birthday",
                        $"Happy birthday, {birthdayEmployee.FullName}. Wishing you a wonderful day and a fantastic year ahead.",
                        "EmployeeBirthdaySelf",
                        "EmployeeBirthday",
                        referenceId,
                        "/Calendar/Index?mine=true");
                }

                foreach (var recipient in recipients)
                {
                    string userId = recipient.ApplicationUserId!;

                    if (recipient.Id == birthdayEmployee.Id)
                    {
                        continue;
                    }

                    if (await notificationService.ExistsAsync(
                        userId,
                        "EmployeeBirthday",
                        "EmployeeBirthday",
                        referenceId))
                    {
                        continue;
                    }

                    await notificationService.CreateAsync(
                        userId,
                        "Employee Birthday",
                        $"{birthdayEmployee.FullName} has a birthday today.",
                        "EmployeeBirthday",
                        "EmployeeBirthday",
                        referenceId,
                        "/Calendar/Index?mine=true");
                }
            }
        }

        private static bool IsBirthdayToday(
            DateTime dateOfBirth,
            DateTime today)
        {
            if (dateOfBirth.Month == 2 &&
                dateOfBirth.Day == 29 &&
                !DateTime.IsLeapYear(today.Year))
            {
                return today.Month == 2 &&
                    today.Day == 28;
            }

            return dateOfBirth.Month == today.Month &&
                dateOfBirth.Day == today.Day;
        }

        private static async Task NotifyMissingCheckInAsync(
            ApplicationDbContext context,
            INotificationService notificationService,
            DateTime today,
            CancellationToken cancellationToken)
        {
            int referenceId = int.Parse(today.ToString("yyyyMMdd"));

            var employees = await context.Employees
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.ApplicationUserId != null &&
                    !context.Attendances.Any(a =>
                        a.EmployeeId == x.Id &&
                        a.AttendanceDate.Date == today))
                .ToListAsync(cancellationToken);

            foreach (var employee in employees)
            {
                if (await notificationService.ExistsAsync(
                    employee.ApplicationUserId!,
                    "AttendanceMissing",
                    "AttendanceReminder",
                    referenceId))
                {
                    continue;
                }

                await notificationService.CreateAsync(
                    employee.ApplicationUserId!,
                    "Check-in Missing",
                    "Your check-in is missing for today.",
                    "AttendanceMissing",
                    "AttendanceReminder",
                    referenceId,
                    "/Attendance/Index");
            }
        }

        private static async Task NotifyMissingCheckOutAsync(
            ApplicationDbContext context,
            INotificationService notificationService,
            DateTime today,
            CancellationToken cancellationToken)
        {
            int referenceId = int.Parse(today.ToString("yyyyMMdd"));

            var attendances = await context.Attendances
                .AsNoTracking()
                .Include(x => x.Employee)
                .Where(x =>
                    x.AttendanceDate.Date == today &&
                    x.CheckInTime.HasValue &&
                    !x.CheckOutTime.HasValue &&
                    x.Employee != null &&
                    x.Employee.ApplicationUserId != null)
                .ToListAsync(cancellationToken);

            foreach (var attendance in attendances)
            {
                string userId = attendance.Employee!.ApplicationUserId!;

                if (await notificationService.ExistsAsync(
                    userId,
                    "CheckOutMissing",
                    "AttendanceReminder",
                    referenceId))
                {
                    continue;
                }

                await notificationService.CreateAsync(
                    userId,
                    "Check-out Missing",
                    "Your check-out is missing for today.",
                    "CheckOutMissing",
                    "AttendanceReminder",
                    referenceId,
                    "/Attendance/Index");
            }
        }

        private TimeSpan GetConfiguredTime(
            string key,
            TimeSpan fallback)
        {
            return TimeSpan.TryParse(
                _configuration[key],
                out var value)
                    ? value
                    : fallback;
        }
    }
}