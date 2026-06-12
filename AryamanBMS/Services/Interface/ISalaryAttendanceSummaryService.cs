using AryamanBMS.ViewModels;

namespace AryamanBMS.Services.Interfaces
{
    public interface ISalaryAttendanceSummaryService
    {
        Task<List<AttendanceSummaryViewModel>> GetMonthlySummaryAsync(
            int month,
            int year);
    }
}