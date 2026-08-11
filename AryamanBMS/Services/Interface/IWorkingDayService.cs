namespace AryamanBMS.Services.Interface
{
    public interface IWorkingDayService
    {
        Task<bool> IsWorkingDayAsync(DateTime date);

        Task<string> GetDayStatusAsync(DateTime date);
    }
}