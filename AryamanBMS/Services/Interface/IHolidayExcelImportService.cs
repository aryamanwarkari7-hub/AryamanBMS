using AryamanBMS.ViewModels;

namespace AryamanBMS.Services.Interface
{
    public interface IHolidayExcelImportService
    {
        Task<HolidayImportResult> ImportAsync(IFormFile file);
    }
}