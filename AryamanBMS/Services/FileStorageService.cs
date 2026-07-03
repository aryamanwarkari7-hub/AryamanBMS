using AryamanBMS.Models;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.StaticFiles;

namespace AryamanBMS.Services
{
    public class FileStorageService : IFileStorageService
    {
        private const long MaximumFileSize = 10 * 1024 * 1024;

        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".pdf",
                ".doc",
                ".docx",
                ".xls",
                ".xlsx",
                ".jpg",
                ".jpeg",
                ".png"
            };

        private readonly string _privateStorageRoot;
        private readonly FileExtensionContentTypeProvider _contentTypeProvider;

        public FileStorageService(IWebHostEnvironment environment)
        {
            _privateStorageRoot = Path.Combine(
                environment.ContentRootPath,
                "App_Data");

            Directory.CreateDirectory(_privateStorageRoot);

            _contentTypeProvider = new FileExtensionContentTypeProvider();
        }

        public async Task<FileUploadResult> UploadAsync(
            IFormFile file,
            string folderName)
        {
            var result = new FileUploadResult();

            try
            {
                if (file == null || file.Length == 0)
                {
                    return Failure("Please select a valid file.");
                }

                if (file.Length > MaximumFileSize)
                {
                    return Failure("File size cannot exceed 10 MB.");
                }

                string originalFileName =
                    Path.GetFileName(file.FileName);

                string extension =
                    Path.GetExtension(originalFileName)
                        .ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(extension) ||
                    !AllowedExtensions.Contains(extension))
                {
                    return Failure(
                        "Only PDF, Word, Excel, JPG and PNG files are allowed.");
                }

                if (!IsValidFolderName(folderName))
                {
                    return Failure("Invalid upload folder.");
                }

                string safeFolder = folderName;

                string uploadFolder =
                    GetSafePhysicalPath(safeFolder);

                Directory.CreateDirectory(uploadFolder);

                string storedFileName =
                    $"{Guid.NewGuid():N}{extension}";

                string relativePath =
                    Path.Combine(
                            safeFolder,
                            storedFileName)
                        .Replace("\\", "/");

                string physicalPath =
                    GetSafePhysicalPath(relativePath);

                await using var stream = new FileStream(
                    physicalPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    useAsync: true);

                await file.CopyToAsync(stream);

                string contentType =
                    GetContentType(storedFileName);

                return new FileUploadResult
                {
                    Success = true,
                    OriginalFileName = originalFileName,
                    StoredFileName = storedFileName,
                    RelativePath = relativePath,
                    PhysicalPath = physicalPath,
                    FileExtension = extension,
                    ContentType = contentType,
                    FileSize = file.Length
                };
            }
            catch (IOException)
            {
                result.Success = false;
                result.ErrorMessage =
                    "The file could not be stored. Please try again.";

                return result;
            }
            catch (UnauthorizedAccessException)
            {
                result.Success = false;
                result.ErrorMessage =
                    "The application does not have permission to store files.";

                return result;
            }
            catch
            {
                result.Success = false;
                result.ErrorMessage =
                    "An unexpected error occurred while uploading the file.";

                return result;
            }
        }

        public async Task<byte[]?> DownloadAsync(
            string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return null;

            try
            {
                string physicalPath =
                    GetSafePhysicalPath(relativePath);

                if (!File.Exists(physicalPath))
                    return null;

                return await File.ReadAllBytesAsync(physicalPath);
            }
            catch
            {
                return null;
            }
        }

        public Task<bool> DeleteAsync(
            string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return Task.FromResult(false);

            try
            {
                string physicalPath =
                    GetSafePhysicalPath(relativePath);

                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                }

                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public bool FileExists(
            string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return false;

            try
            {
                string physicalPath =
                    GetSafePhysicalPath(relativePath);

                return File.Exists(physicalPath);
            }
            catch
            {
                return false;
            }
        }

        private string GetSafePhysicalPath(
            string relativePath)
        {
            string normalizedRelativePath =
                relativePath
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .TrimStart(Path.DirectorySeparatorChar);

            string fullPath =
                Path.GetFullPath(
                    Path.Combine(
                        _privateStorageRoot,
                        normalizedRelativePath));

            string normalizedRoot =
                Path.GetFullPath(_privateStorageRoot)
                    .TrimEnd(Path.DirectorySeparatorChar) +
                Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(
                    normalizedRoot,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Invalid file path.");
            }

            return fullPath;
        }

        private static bool IsValidFolderName(
            string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                return false;

            if (folderName.Contains(".."))
                return false;

            char[] invalidCharacters =
                Path.GetInvalidPathChars();

            return folderName.IndexOfAny(invalidCharacters) < 0;
        }

        private string GetContentType(
            string fileName)
        {
            return _contentTypeProvider.TryGetContentType(
                fileName,
                out string? contentType)
                    ? contentType
                    : "application/octet-stream";
        }

        private static FileUploadResult Failure(
            string message)
        {
            return new FileUploadResult
            {
                Success = false,
                ErrorMessage = message
            };
        }
    }
}