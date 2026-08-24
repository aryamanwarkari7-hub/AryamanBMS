namespace AryamanBMS.Repositories.Interfaces
{
    public interface IWorkingDayRepository
    {
        Task<string?> GetActiveOverrideTypeAsync(DateTime date);

        Task<bool> HasActiveHolidayAsync(DateTime date);
    }
}