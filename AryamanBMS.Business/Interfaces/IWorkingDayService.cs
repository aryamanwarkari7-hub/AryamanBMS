namespace AryamanBMS.Business.Interfaces
{
    public interface IWorkingDayService
    {
        Task<bool> IsWorkingDayAsync(DateTime date);

        Task<string> GetDayStatusAsync(DateTime date);
    }
}