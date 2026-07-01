using AryamanBMS.Models;
using AryamanBMS.Services.Interfaces;

namespace AryamanBMS.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;

        public FileStorageService(
            IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<FileUploadResult> UploadAsync(
            IFormFile file,
            string folderName)
        {
            var result = new FileUploadResult();

            try
            {
                string uploadFolder =
                    Path.Combine(
                        _environment.WebRootPath,
                        "Uploads",
                        folderName);

                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                string extension =
                    Path.GetExtension(file.FileName);

                string storedName =
                    $"{Guid.NewGuid()}{extension}";

                string physicalPath =
                    Path.Combine(uploadFolder, storedName);

                using FileStream stream =
                    new FileStream(
                        physicalPath,
                        FileMode.Create);

                await file.CopyToAsync(stream);

                result.Success = true;

                result.OriginalFileName = file.FileName;

                result.StoredFileName = storedName;

                result.FileExtension = extension;

                result.ContentType = file.ContentType;

                result.FileSize = file.Length;

                result.PhysicalPath = physicalPath;

                result.RelativePath =
                    Path.Combine(
                        "Uploads",
                        folderName,
                        storedName)
                    .Replace("\\", "/");
            }
            catch (Exception ex)
            {
                result.Success = false;

                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<byte[]?> DownloadAsync(
            string relativePath)
        {
            string physicalPath =
                Path.Combine(
                    _environment.WebRootPath,
                    relativePath);

            if (!File.Exists(physicalPath))
                return null;

            return await File.ReadAllBytesAsync(
                physicalPath);
        }

        public async Task<bool> DeleteAsync(
            string relativePath)
        {
            try
            {
                string physicalPath =
                    Path.Combine(
                        _environment.WebRootPath,
                        relativePath);

                if (File.Exists(physicalPath))
                    File.Delete(physicalPath);

                await Task.CompletedTask;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool FileExists(string relativePath)
        {
            string physicalPath =
                Path.Combine(
                    _environment.WebRootPath,
                    relativePath);

            return File.Exists(physicalPath);
        }
    }
}