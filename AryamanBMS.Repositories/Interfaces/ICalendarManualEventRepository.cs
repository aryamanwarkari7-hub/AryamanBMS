using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface ICalendarManualEventRepository
    {
        Task<CalendarManualEventModel?> GetActiveByIdAsync(int id);

        Task AddAsync(CalendarManualEventModel item);

        Task SaveAsync();

        Task<List<CalendarManualEventModel>> GetActiveEventsAsync(
            DateTime start,
            DateTime end,
            string? personalUserId = null);
    }
}