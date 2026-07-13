using AryamanBMS.Models;

namespace AryamanBMS.Services.Interfaces
{
    public interface IFileStorageService
    {
        Task<FileUploadResult> UploadAsync(
            IFormFile file,
            string folderName);

        Task<byte[]?> DownloadAsync(
            string relativePath);

        Task<bool> DeleteAsync(
            string relativePath);

        bool FileExists(
            string relativePath);
    }
}
