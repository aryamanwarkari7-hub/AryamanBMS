using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IHolidayRepository
    {
        Task<(List<HolidayModel> Records, int TotalRecords)> GetPagedAsync(
            int year,
            int? month,
            string status,
            string sortBy,
            string sortOrder,
            int page,
            int pageSize
        );

        Task<List<HolidayModel>> GetForExportAsync(int year, int? month, string status);

        Task<List<HolidayModel>> GetActiveInRangeAsync(DateTime start, DateTime end);

        Task<List<HolidayModel>> GetActiveConfirmedInRangeAsync(DateTime start, DateTime end);

        Task<HolidayModel?> GetByDateAsync(DateTime holidayDate);

        Task<HolidayModel?> GetByIdAsync(int holidayId);

        Task<List<HolidayModel>> GetUpcomingUnconfirmedHolidaysAsync(int withinDays);

        Task AddAsync(HolidayModel holiday);

        Task SaveAsync();
    }
}
