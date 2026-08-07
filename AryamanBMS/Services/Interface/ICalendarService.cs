using System.Security.Claims;
using AryamanBMS.ViewModels;

namespace AryamanBMS.Services.Interfaces
{
    public interface ICalendarService
    {
        Task<List<CalendarEventViewModel>> GetEventsAsync(
            ClaimsPrincipal user,
            DateTime start,
            DateTime end);
    }
}