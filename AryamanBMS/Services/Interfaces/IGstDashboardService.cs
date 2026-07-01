using AryamanBMS.ViewModels;

namespace AryamanBMS.Services.Interfaces
{
    public interface IGstDashboardService
    {
        Task<GstDashboardViewModel> GetDashboardAsync(
            int month,
            int year);
    }
}
