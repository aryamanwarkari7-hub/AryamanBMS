using AryamanBMS.Data;
using AryamanBMS.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Services
{
    public class WorkingDayService : IWorkingDayService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public WorkingDayService(
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<bool> IsWorkingDayAsync(DateTime date)
        {
            var status = await GetDayStatusAsync(date);

            return status == "Working";
        }

        public async Task<string> GetDayStatusAsync(DateTime date)
        {
            date = date.Date;

            var overrideType =
                await _context.WorkingDayOverrides
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive &&
                        x.OverrideDate.Date == date)
                    .Select(x => x.OverrideType)
                    .FirstOrDefaultAsync();

            if (overrideType == "Working Day")
            {
                return "Working";
            }

            if (overrideType == "Holiday")
            {
                return "Holiday";
            }

            if (overrideType == "Weekly Off")
            {
                return "WeeklyOff";
            }

            var holidayExists =
                await _context.Holidays
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.IsActive &&
                        x.HolidayDate.Date == date);

            if (holidayExists)
            {
                return "Holiday";
            }

            var configuredHolidays =
                _configuration
                    .GetSection("Attendance:OfficeHolidays")
                    .Get<string[]>()
                ?? Array.Empty<string>();

            if (configuredHolidays.Any(value =>
                DateTime.TryParse(value, out var holiday) &&
                holiday.Date == date))
            {
                return "Holiday";
            }

            var configuredWeeklyOffs =
                _configuration
                    .GetSection("Attendance:WeeklyOffDays")
                    .Get<string[]>()
                ?? Array.Empty<string>();

            if (configuredWeeklyOffs.Any(value =>
                Enum.TryParse(
                    value,
                    true,
                    out DayOfWeek weeklyOffDay) &&
                date.DayOfWeek == weeklyOffDay))
            {
                return "WeeklyOff";
            }

            if (date.DayOfWeek == DayOfWeek.Saturday)
            {
                var saturdayNumber =
                    ((date.Day - 1) / 7) + 1;

                var workingSaturdayNumbers =
                    _configuration
                        .GetSection("Attendance:WorkingSaturdayNumbers")
                        .Get<int[]>()
                    ?? new[] { 1, 3, 5 };

                return workingSaturdayNumbers.Contains(saturdayNumber)
                    ? "Working"
                    : "WeeklyOff";
            }

            return "Working";
        }
    }
}