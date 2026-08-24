using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace AryamanBMS.Business.Services
{
    public class WorkingDayService : IWorkingDayService
    {
        private readonly IWorkingDayRepository _workingDayRepository;
        private readonly WorkingDayOptions _workingDayOptions;

        public WorkingDayService(
            IWorkingDayRepository workingDayRepository,
            IOptions<WorkingDayOptions> workingDayOptions)
        {
            _workingDayRepository = workingDayRepository;
            _workingDayOptions = workingDayOptions.Value;
        }

        public async Task<bool> IsWorkingDayAsync(DateTime date)
        {
            var status = await GetDayStatusAsync(date);

            return status == "Working";
        }

        public async Task<string> GetDayStatusAsync(DateTime date)
        {
            date = date.Date;

            var overrideType = await _workingDayRepository.GetActiveOverrideTypeAsync(date);

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

            var holidayExists = await _workingDayRepository.HasActiveHolidayAsync(date);

            if (holidayExists)
            {
                return "Holiday";
            }

            var configuredHolidays =
                _workingDayOptions
                    .OfficeHolidays
                    .ToArray();

            if (configuredHolidays.Any(value =>
                DateTime.TryParse(value, out var holiday) &&
                holiday.Date == date))
            {
                return "Holiday";
            }

            var configuredWeeklyOffs =
                _workingDayOptions
                    .WeeklyOffDays
                    .Select(d => d.ToString())
                    .ToArray();

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

                var workingSaturdayNumbers = _workingDayOptions.WorkingSaturdayNumbers;

                return workingSaturdayNumbers.Contains(saturdayNumber)
                    ? "Working"
                    : "WeeklyOff";
            }

            return "Working";
        }
    }
}