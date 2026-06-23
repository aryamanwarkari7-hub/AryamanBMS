using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Http;

namespace AryamanBMS.Services.Interface
{
    public interface ISalaryExcelImportService
    {
        Task<SalaryImportResult> ImportAsync(
            IFormFile file,
            int month,
            int year);
    }
}