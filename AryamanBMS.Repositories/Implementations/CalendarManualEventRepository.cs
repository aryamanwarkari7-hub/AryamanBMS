using AryamanBMS.Database.Context;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories.Implementations
{
    public class CalendarManualEventRepository
        : ICalendarManualEventRepository
    {
        private readonly CalendarManualEventDbContext _context;

        public CalendarManualEventRepository(
            CalendarManualEventDbContext context)
        {
            _context = context;
        }

        public async Task<CalendarManualEventModel?>
            GetActiveByIdAsync(int id)
        {
            return await _context.CalendarManualEvents
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);
        }

        public async Task AddAsync(
            CalendarManualEventModel item)
        {
            await _context.CalendarManualEvents.AddAsync(item);
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<List<CalendarManualEventModel>>
            GetActiveEventsAsync(
                DateTime start,
                DateTime end,
                string? personalUserId = null)
        {
            return await _context.CalendarManualEvents
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.StartDateTime <= end &&
                    (x.EndDateTime ?? x.StartDateTime) >= start &&
                    (personalUserId == null ||
                     x.VisibilityScope == "All" ||
                     x.CreatedByUserId == personalUserId))
                .ToListAsync();
        }
    }
}