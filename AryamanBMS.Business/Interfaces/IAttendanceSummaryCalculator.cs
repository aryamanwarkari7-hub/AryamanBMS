using AryamanBMS.Models;

namespace AryamanBMS.Business.Interfaces
{
    public interface IAttendanceSummaryCalculator
    {
        Task<List<AttendanceSummaryResult>> CalculateAsync(
            AttendanceSummaryCalculationInput input);
    }
}