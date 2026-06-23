using AryamanBMS.Models;
using Microsoft.AspNetCore.Http;

namespace AryamanBMS.Services.Interface
{
    public interface IEmployeeDocumentService
    {
        Task<EmployeeDocumentModel> SaveAsync(
            IFormFile file,
            string employeeCode,
            string documentType,
            string? uploadedBy);

        Task DeleteAsync(string storagePath);

        string GetAbsolutePath(string storagePath);
    }
}