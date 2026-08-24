using AryamanBMS.Database.Context;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class WorkingDayRepository : IWorkingDayRepository
    {
        private readonly AttendanceCalendarDbContext _context;

        public WorkingDayRepository(
            AttendanceCalendarDbContext context)
        {
            _context = context;
        }

        public async Task<string?> GetActiveOverrideTypeAsync(
            DateTime date)
        {
            date = date.Date;

            return await _context.WorkingDayOverrides
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.OverrideDate.Date == date)
                .Select(x => x.OverrideType)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> HasActiveHolidayAsync(DateTime date)
        {
            date = date.Date;

            return await _context.Holidays
                .AsNoTracking()
                .AnyAsync(x =>
                    x.IsActive &&
                    x.HolidayDate.Date == date);
        }
    }
}