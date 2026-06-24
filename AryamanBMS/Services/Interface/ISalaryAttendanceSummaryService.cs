using AryamanBMS.ViewModels;

namespace AryamanBMS.Services.Interface
{
    public interface ISalaryAttendanceSummaryService
    {
        Task<List<AttendanceSummaryViewModel>> GetMonthlySummaryAsync(
            int month,
            int year);
    }
}