using AryamanBMS.Database.Context;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories.Implementations
{
    public class HolidayRepository : IHolidayRepository
    {
        private readonly AttendanceCalendarDbContext _context;

        public HolidayRepository(
            AttendanceCalendarDbContext context)
        {
            _context = context;
        }

        public async Task<(List<HolidayModel> Records, int TotalRecords)>
            GetPagedAsync(
                int year,
                int? month,
                string status,
                string sortBy,
                string sortOrder,
                int page,
                int pageSize)
        {
            if (page < 1)
            {
                page = 1;
            }

            if (pageSize < 1)
            {
                pageSize = 12;
            }

            var query = BuildFilteredQuery(year, month, status);

            query = ApplySort(query, sortBy, sortOrder);

            int totalRecords = await query.CountAsync();

            int totalPages = totalRecords == 0
                ? 1
                : (int)Math.Ceiling(
                    totalRecords / (double)pageSize);

            if (page > totalPages)
            {
                page = totalPages;
            }

            var records = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (records, totalRecords);
        }

        public async Task<List<HolidayModel>> GetForExportAsync(
            int year,
            int? month,
            string status)
        {
            return await BuildFilteredQuery(year, month, status)
                .OrderBy(x => x.HolidayDate)
                .ToListAsync();
        }

        public async Task<List<HolidayModel>> GetActiveInRangeAsync(
            DateTime start,
            DateTime end)
        {
            return await _context.Holidays
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.HolidayDate.Date >= start.Date &&
                    x.HolidayDate.Date <= end.Date)
                .OrderBy(x => x.HolidayDate)
                .ToListAsync();
        }

        public async Task<HolidayModel?> GetByDateAsync(
            DateTime holidayDate)
        {
            holidayDate = holidayDate.Date;

            return await _context.Holidays
                .FirstOrDefaultAsync(x =>
                    x.HolidayDate == holidayDate);
        }

        public async Task AddAsync(HolidayModel holiday)
        {
            await _context.Holidays.AddAsync(holiday);
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        private IQueryable<HolidayModel> BuildFilteredQuery(
            int year,
            int? month,
            string status)
        {
            var query = _context.Holidays
                .AsNoTracking()
                .Where(x => x.HolidayDate.Year == year);

            if (month.HasValue)
            {
                query = query.Where(x =>
                    x.HolidayDate.Month == month.Value);
            }

            if (status == "Active")
            {
                query = query.Where(x => x.IsActive);
            }
            else if (status == "Inactive")
            {
                query = query.Where(x => !x.IsActive);
            }

            return query;
        }

        private static IQueryable<HolidayModel> ApplySort(
            IQueryable<HolidayModel> query,
            string sortBy,
            string sortOrder)
        {
            bool descending = string.Equals(
                sortOrder,
                "desc",
                StringComparison.OrdinalIgnoreCase);

            return sortBy switch
            {
                "HolidayName" => descending
                    ? query.OrderByDescending(x => x.HolidayName)
                        .ThenBy(x => x.HolidayDate)
                    : query.OrderBy(x => x.HolidayName)
                        .ThenBy(x => x.HolidayDate),

                "HolidayType" => descending
                    ? query.OrderByDescending(x => x.HolidayType)
                        .ThenBy(x => x.HolidayDate)
                    : query.OrderBy(x => x.HolidayType)
                        .ThenBy(x => x.HolidayDate),

                "Status" => descending
                    ? query.OrderByDescending(x => x.IsActive)
                        .ThenBy(x => x.HolidayDate)
                    : query.OrderBy(x => x.IsActive)
                        .ThenBy(x => x.HolidayDate),

                _ => descending
                    ? query.OrderByDescending(x => x.HolidayDate)
                        .ThenBy(x => x.HolidayName)
                    : query.OrderBy(x => x.HolidayDate)
                        .ThenBy(x => x.HolidayName)
            };
        }
    }
}