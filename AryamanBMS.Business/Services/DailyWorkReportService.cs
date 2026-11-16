using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Business.Services
{
    public class DailyWorkReportService : IDailyWorkReportService
    {
        private readonly IDailyWorkReportRepository _reportRepository;

        public DailyWorkReportService(IDailyWorkReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public async Task<DailyWorkReportModel?> GetReportByIdAsync(int id)
        {
            return await _reportRepository.GetByIdAsync(id, asNoTracking: true);
        }

        public async Task<DailyWorkReportModel?> GetReportByEmployeeAndDateAsync(
            int employeeId,
            DateTime date
        )
        {
            return await _reportRepository.GetByEmployeeAndDateAsync(employeeId, date);
        }

        public async Task<List<DailyWorkReportModel>> GetEmployeeReportsAsync(
            int employeeId,
            DateTime? fromDate = null,
            DateTime? toDate = null
        )
        {
            return await _reportRepository.GetByEmployeeAsync(employeeId, fromDate, toDate);
        }

        public async Task<List<DailyWorkReportModel>> GetTeamReportsAsync(
            int? employeeId = null,
            int? projectId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null
        )
        {
            return await _reportRepository.GetReportsAsync(employeeId, projectId, fromDate, toDate);
        }

        public async Task<List<DailyWorkReportPlanModel>> GetPreviousPlansToImportAsync(
            int employeeId,
            DateTime date
        )
        {
            return await _reportRepository.GetMostRecentPreviousPlansAsync(employeeId, date);
        }

        public async Task<(
            bool Success,
            string? Error,
            DailyWorkReportModel? Report
        )> SaveReportAsync(DailyWorkReportModel model, string currentUserId)
        {
            if (model.EmployeeId <= 0)
            {
                return (false, "A valid employee record is required.", null);
            }

            model.ReportDate = model.ReportDate.Date;

            // Ensure unique report per day
            int? excludeId = model.DailyWorkReportId > 0 ? model.DailyWorkReportId : null;
            if (
                await _reportRepository.ExistsForDateAsync(
                    model.EmployeeId,
                    model.ReportDate,
                    excludeId
                )
            )
            {
                return (
                    false,
                    $"A Daily Work Report already exists for {model.ReportDate:dd-MM-yyyy}.",
                    null
                );
            }

            // Remove completely empty rows from details
            model.WorkDetails ??= new List<DailyWorkReportDetailModel>();
            model.NextDayPlans ??= new List<DailyWorkReportPlanModel>();

            // Remove completely empty rows from details
            var validDetails = model
                .WorkDetails.Where(x =>
                    !string.IsNullOrWhiteSpace(x.Activity)
                    || !string.IsNullOrWhiteSpace(x.ProjectName)
                )
                .ToList();

            for (int i = 0; i < validDetails.Count; i++)
            {
                validDetails[i].SortOrder = i + 1;
                if (string.IsNullOrWhiteSpace(validDetails[i].ProjectName))
                {
                    validDetails[i].ProjectName = "General";
                }
            }
            model.WorkDetails = validDetails;

            // Remove completely empty rows from plans
            var validPlans = model
                .NextDayPlans.Where(x =>
                    !string.IsNullOrWhiteSpace(x.Activity)
                    || !string.IsNullOrWhiteSpace(x.ProjectName)
                )
                .ToList();

            for (int i = 0; i < validPlans.Count; i++)
            {
                validPlans[i].SortOrder = i + 1;
                if (string.IsNullOrWhiteSpace(validPlans[i].ProjectName))
                {
                    validPlans[i].ProjectName = "General";
                }
                if (string.IsNullOrWhiteSpace(validPlans[i].Priority))
                {
                    validPlans[i].Priority = "Medium";
                }
            }
            model.NextDayPlans = validPlans;

            if (model.DailyWorkReportId > 0)
            {
                model.UpdatedOn = DateTime.Now;
                var updated = await _reportRepository.UpdateAsync(model);
                return (true, null, updated);
            }
            else
            {
                model.CreatedByUserId = currentUserId;
                model.CreatedOn = DateTime.Now;
                var created = await _reportRepository.CreateAsync(model);
                return (true, null, created);
            }
        }

        public async Task<bool> DeleteReportAsync(int id, string currentUserId, bool isAdmin)
        {
            var report = await _reportRepository.GetByIdAsync(id);
            if (report == null)
                return false;

            if (!isAdmin && report.CreatedByUserId != currentUserId)
            {
                return false; // Unauthorized
            }

            await _reportRepository.DeleteAsync(id);
            return true;
        }
    }
}
