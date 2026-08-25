using AryamanBMS.Database.Context;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories.Implementations
{
    public class WorkingDayOverrideRepository
        : IWorkingDayOverrideRepository
    {
        private readonly AttendanceCalendarDbContext _context;

        public WorkingDayOverrideRepository(
            AttendanceCalendarDbContext context)
        {
            _context = context;
        }

        public async Task<List<WorkingDayOverrideModel>> GetAllAsync(
            string status,
            string sortBy,
            string sortOrder)
        {
            var query = _context.WorkingDayOverrides
                .AsNoTracking()
                .AsQueryable();

            if (status == "Active")
            {
                query = query.Where(x => x.IsActive);
            }
            else if (status == "Inactive")
            {
                query = query.Where(x => !x.IsActive);
            }

            bool descending = string.Equals(
                sortOrder,
                "desc",
                StringComparison.OrdinalIgnoreCase);

            query = sortBy switch
            {
                "OverrideType" => descending
                    ? query.OrderByDescending(x => x.OverrideType)
                        .ThenBy(x => x.OverrideDate)
                    : query.OrderBy(x => x.OverrideType)
                        .ThenBy(x => x.OverrideDate),

                "Status" => descending
                    ? query.OrderByDescending(x => x.IsActive)
                        .ThenBy(x => x.OverrideDate)
                    : query.OrderBy(x => x.IsActive)
                        .ThenBy(x => x.OverrideDate),

                _ => descending
                    ? query.OrderByDescending(x => x.OverrideDate)
                    : query.OrderBy(x => x.OverrideDate)
            };

            return await query.ToListAsync();
        }

        public async Task<List<WorkingDayOverrideModel>>
            GetForExportAsync()
        {
            return await _context.WorkingDayOverrides
                .AsNoTracking()
                .OrderBy(x => x.OverrideDate)
                .ToListAsync();
        }

        public async Task<WorkingDayOverrideModel?>
            GetByIdAsync(int id)
        {
            return await _context.WorkingDayOverrides
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> ExistsForDateAsync(
            DateTime overrideDate,
            int? excludeId = null)
        {
            overrideDate = overrideDate.Date;

            return await _context.WorkingDayOverrides
                .AsNoTracking()
                .AnyAsync(x =>
                    x.OverrideDate.Date == overrideDate &&
                    (!excludeId.HasValue ||
                     x.Id != excludeId.Value));
        }

        public async Task AddAsync(
            WorkingDayOverrideModel item)
        {
            await _context.WorkingDayOverrides.AddAsync(item);
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}