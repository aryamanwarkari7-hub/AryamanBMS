using AryamanBMS.Database.Context;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories.Implementations
{
    public class DailyWorkReportRepository : IDailyWorkReportRepository
    {
        private readonly DailyWorkReportDbContext _context;

        public DailyWorkReportRepository(DailyWorkReportDbContext context)
        {
            _context = context;
        }

        public async Task<DailyWorkReportModel?> GetByIdAsync(int id, bool asNoTracking = false)
        {
            var query = _context
                .DailyWorkReports.Include(x => x.WorkDetails.OrderBy(d => d.SortOrder))
                .Include(x => x.NextDayPlans.OrderBy(p => p.SortOrder))
                .AsQueryable();

            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            return await query.FirstOrDefaultAsync(x => x.DailyWorkReportId == id);
        }

        public async Task<DailyWorkReportModel?> GetByEmployeeAndDateAsync(
            int employeeId,
            DateTime date
        )
        {
            var dateOnly = date.Date;
            return await _context
                .DailyWorkReports.Include(x => x.WorkDetails.OrderBy(d => d.SortOrder))
                .Include(x => x.NextDayPlans.OrderBy(p => p.SortOrder))
                .FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.ReportDate == dateOnly);
        }

        public async Task<List<DailyWorkReportModel>> GetByEmployeeAsync(
            int employeeId,
            DateTime? fromDate = null,
            DateTime? toDate = null
        )
        {
            var query = _context
                .DailyWorkReports.Include(x => x.WorkDetails.OrderBy(d => d.SortOrder))
                .Include(x => x.NextDayPlans.OrderBy(p => p.SortOrder))
                .Where(x => x.EmployeeId == employeeId && x.IsActive);

            if (fromDate.HasValue)
            {
                var fDate = fromDate.Value.Date;
                query = query.Where(x => x.ReportDate >= fDate);
            }

            if (toDate.HasValue)
            {
                var tDate = toDate.Value.Date;
                query = query.Where(x => x.ReportDate <= tDate);
            }

            return await query.OrderByDescending(x => x.ReportDate).ToListAsync();
        }

        public async Task<List<DailyWorkReportModel>> GetReportsAsync(
            int? employeeId = null,
            int? projectId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null
        )
        {
            var query = _context
                .DailyWorkReports.Include(x => x.WorkDetails.OrderBy(d => d.SortOrder))
                .Include(x => x.NextDayPlans.OrderBy(p => p.SortOrder))
                .Where(x => x.IsActive)
                .AsQueryable();

            if (employeeId.HasValue)
            {
                query = query.Where(x => x.EmployeeId == employeeId.Value);
            }

            if (fromDate.HasValue)
            {
                var fDate = fromDate.Value.Date;
                query = query.Where(x => x.ReportDate >= fDate);
            }

            if (toDate.HasValue)
            {
                var tDate = toDate.Value.Date;
                query = query.Where(x => x.ReportDate <= tDate);
            }

            if (projectId.HasValue)
            {
                query = query.Where(x =>
                    x.WorkDetails.Any(d => d.ProjectId == projectId.Value)
                    || x.NextDayPlans.Any(p => p.ProjectId == projectId.Value)
                );
            }

            return await query.OrderByDescending(x => x.ReportDate).ToListAsync();
        }

        public async Task<List<DailyWorkReportPlanModel>> GetMostRecentPreviousPlansAsync(
            int employeeId,
            DateTime beforeDate
        )
        {
            var targetDate = beforeDate.Date;
            var previousReport = await _context
                .DailyWorkReports.Include(x => x.NextDayPlans.OrderBy(p => p.SortOrder))
                .Where(x => x.EmployeeId == employeeId && x.ReportDate < targetDate && x.IsActive)
                .OrderByDescending(x => x.ReportDate)
                .FirstOrDefaultAsync();

            return previousReport?.NextDayPlans.ToList() ?? new List<DailyWorkReportPlanModel>();
        }

        public async Task<bool> ExistsForDateAsync(
            int employeeId,
            DateTime date,
            int? excludeReportId = null
        )
        {
            var dateOnly = date.Date;
            return await _context.DailyWorkReports.AnyAsync(x =>
                x.EmployeeId == employeeId
                && x.ReportDate == dateOnly
                && (!excludeReportId.HasValue || x.DailyWorkReportId != excludeReportId.Value)
                && x.IsActive
            );
        }

        public async Task<DailyWorkReportModel> CreateAsync(DailyWorkReportModel report)
        {
            report.TotalPlannedHours = report.WorkDetails.Sum(x => x.PlannedEffortHours);
            report.TotalActualHours = report.WorkDetails.Sum(x => x.ActualEffortHours);
            report.CreatedOn = DateTime.Now;

            _context.DailyWorkReports.Add(report);
            await _context.SaveChangesAsync();
            return report;
        }

        public async Task<DailyWorkReportModel> UpdateAsync(DailyWorkReportModel report)
        {
            var existing = await _context
                .DailyWorkReports.Include(x => x.WorkDetails)
                .Include(x => x.NextDayPlans)
                .FirstOrDefaultAsync(x => x.DailyWorkReportId == report.DailyWorkReportId);

            if (existing == null)
            {
                throw new InvalidOperationException(
                    $"Daily Work Report ID {report.DailyWorkReportId} not found."
                );
            }

            existing.ReportDate = report.ReportDate.Date;
            existing.GeneralRemarks = report.GeneralRemarks;
            existing.TotalPlannedHours = report.WorkDetails.Sum(x => x.PlannedEffortHours);
            existing.TotalActualHours = report.WorkDetails.Sum(x => x.ActualEffortHours);
            existing.UpdatedOn = DateTime.Now;

            // Sync Work Details
            _context.DailyWorkReportDetails.RemoveRange(existing.WorkDetails);
            foreach (var detail in report.WorkDetails)
            {
                detail.DailyWorkReportDetailId = 0;
                detail.DailyWorkReportId = existing.DailyWorkReportId;
                existing.WorkDetails.Add(detail);
            }

            // Sync Next Day Plans
            _context.DailyWorkReportPlans.RemoveRange(existing.NextDayPlans);
            foreach (var plan in report.NextDayPlans)
            {
                plan.DailyWorkReportPlanId = 0;
                plan.DailyWorkReportId = existing.DailyWorkReportId;
                existing.NextDayPlans.Add(plan);
            }

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task DeleteAsync(int id)
        {
            var report = await _context.DailyWorkReports.FindAsync(id);
            if (report != null)
            {
                report.IsActive = false;
                report.UpdatedOn = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }
    }
}
